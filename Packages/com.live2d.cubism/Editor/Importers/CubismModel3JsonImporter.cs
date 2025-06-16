using Live2D.Cubism.Core;
using Live2D.Cubism.Editor;
using Live2D.Cubism.Editor.Importers;
using Live2D.Cubism.Framework.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Packages.Live2D.Editor.Importers.New
{
    public interface IModelImportContext
    {
        string AssetPath { get; }
        string ModelName { get; }
        CubismModel3Json Model3Json { get; }

        CubismModel Model { get; }

        AssetImportContext ImporterContext { get; }

        void AddSubObject<T>(T subObject) where T : Object;
    }

    [ScriptedImporter(1, "model3.json", CubismImporterPriorities.Model3JsonImporter)]
    public sealed class CubismModel3JsonImporter : ScriptedImporter
    {
        private sealed class ModelImportContext : IModelImportContext
        {
            private readonly AssetImportContext _ctx;
            private readonly string _modelName;

            public string AssetPath => _ctx.assetPath;
            public string ModelName => _modelName;
            public CubismModel3Json Model3Json { get; }
            public CubismModel Model { get; }
            public AssetImportContext ImporterContext => _ctx;
            public ModelImportContext(AssetImportContext ctx, CubismModel3Json model3Json, CubismModel model)
            {
                _ctx = ctx;
                Model3Json = model3Json;
                Model = model;

                _modelName = Path.GetFileNameWithoutExtension(AssetPath);
                var index = _modelName.IndexOf(".");
                if (index != -1)
                    _modelName = _modelName[..index];
            }

            public void AddSubObject<T>(T subObject) where T : Object
            {
                var name = typeof(T).Name.ToLowerInvariant();
                _ctx.AddObjectToAsset(name, subObject);
            }
        }

        /// <summary>
        /// Callback on <see cref="CubismModel"/> import.
        /// </summary>
        /// <param name="importer">Importer.</param>
        /// <param name="model">Imported model.</param>
        public delegate void ModelImportListener(IModelImportContext ctx);

        /// <summary>
        /// Allows getting called back whenever a model is imported (and before it is saved).
        /// </summary>
        public static event ModelImportListener OnDidImportModel;

        private enum OverrideOption
        {
            SameAsSettings,
            Yes,
            No
        }

        [SerializeField]
        private OverrideOption _overrideImportAsOriginalWorkflowOption;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var model3Json = CubismModel3Json.LoadAtPath(ctx.assetPath);
            if (model3Json == null)
            {
                ctx.LogImportError("unable to load model3json file.");
                return;
            }

            var parentDirectory = Path.GetDirectoryName(ctx.assetPath);
            var mocPath = Path.Combine(parentDirectory, model3Json.FileReferences.Moc);
            var moc = AssetDatabase.LoadAssetAtPath<CubismMoc>(mocPath);
            ctx.DependsOnArtifact(mocPath);

            AssignDependencies(ctx, model3Json.FileReferences);

            // Instantiate model source and model.
            var model = model3Json.ToModel(moc, CubismImporter.OnPickMaterial, CubismImporter.OnPickTexture, ShouldImportAsOriginalWorkflow);

            if (model == null)
            {
                ctx.LogImportError("unable to import model data.");
                return;
            }

            ctx.AddObjectToAsset("model", model.gameObject);
            ctx.SetMainObject(model.gameObject);

            var modelImportContext = new ModelImportContext(ctx, model3Json, model);

            // Trigger events.
            OnDidImportModel?.Invoke(modelImportContext);

            foreach (var texture in model3Json.Textures)
            {
                CubismImporter.SendModelTextureImportEvent(modelImportContext, model, texture);
            }
        }

        /// <summary>
        /// Should import as original workflow.
        /// </summary>
        private bool ShouldImportAsOriginalWorkflow
        {
            get
            {
                return _overrideImportAsOriginalWorkflowOption switch
                {
                    OverrideOption.Yes => true,
                    OverrideOption.No => false,
                    _ => CubismUnityEditorMenu.ShouldImportAsOriginalWorkflow
                };
            }
        }


        private void AssignDependencies(AssetImportContext ctx, CubismModel3Json.SerializableFileReferences references)
        {
            var baseDir = Path.GetDirectoryName(ctx.assetPath);
            string getFulldir(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                return Path.Combine(baseDir, path);
            }

            IEnumerable<IEnumerable<string>> UnravelFileReferences()
            {
                if (references.Textures?.Length > 0)
                {
                    yield return references.Textures.Select(getFulldir);
                }

                if (!string.IsNullOrWhiteSpace(references.DisplayInfo))
                    yield return new string[] { getFulldir(references.DisplayInfo) };

                if (!string.IsNullOrWhiteSpace(references.Physics))
                    yield return new string[] { getFulldir(references.Physics) };

                if (!string.IsNullOrWhiteSpace(references.UserData))
                    yield return new string[] { getFulldir(references.UserData) };

                if (!string.IsNullOrWhiteSpace(references.Pose))
                    yield return new string[] { getFulldir(references.Pose) };

                if (references.Expressions?.Length > 0)
                {
                    yield return references.Expressions.Select(e => e.File)
                        .Distinct().Select(getFulldir);
                }

                if (references.Motions.Motions?.Length > 0)
                {
                    yield return references.Motions.Motions.SelectMany(m => m).Select(m => m.File)
                        .Distinct().Select(getFulldir);
                }

            }

            foreach (var path in UnravelFileReferences().SelectMany(p => p))
                ctx.DependsOnSourceAsset(path);
        }
    }
}
