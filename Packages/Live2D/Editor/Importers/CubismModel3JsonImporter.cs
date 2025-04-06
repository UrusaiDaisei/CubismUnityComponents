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
    [ScriptedImporter(1, "model3.json", CubismImporterPriorities.Model3JsonImporter)]
    public sealed class CubismModel3JsonImporter : ScriptedImporter
    {
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

            var modelImportContext = new CubismImporter.ModelImportContext
            {
                AssetPath = ctx.assetPath,
                Model3Json = model3Json,
            };

            // Trigger events.
            CubismImporter.SendModelImportEvent(modelImportContext, model);
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
