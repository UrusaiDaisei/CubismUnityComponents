/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Json;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Live2D.Cubism.Editor.Importers
{
    /// <summary>
    /// ScriptedImporter for model3.json. Uses 5.3 ToModel(pickers) and exposes <see cref="IModelImportContext"/> and <see cref="OnDidImportModel"/>.
    /// </summary>
    [ScriptedImporter(1, "model3.json", CubismImporterPriorities.Model3JsonImporter)]
    public sealed class CubismModel3JsonImporter : ScriptedImporter
    {
        /// <summary>
        /// Callback when a model is imported (ScriptedImporter path). Use this for custom post-import logic.
        /// </summary>
        public static event Action<IModelImportContext> OnDidImportModel;

        /// <summary>
        /// Path of the asset being imported. Valid only during the current OnImportAsset / model-import callbacks.
        /// </summary>
        public string AssetPath => _currentAssetPath;

        /// <summary>
        /// Model3Json for the asset being imported. Valid only during the current OnImportAsset / model-import callbacks.
        /// </summary>
        public CubismModel3Json Model3Json => _currentModel3Json;

        [NonSerialized]
        private string _currentAssetPath;

        [NonSerialized]
        private CubismModel3Json _currentModel3Json;

        private sealed class ModelImportContext : IModelImportContext
        {
            private readonly AssetImportContext _ctx;
            private readonly string _modelName;

            public string AssetPath => _ctx.assetPath;
            public string ModelName => _modelName;
            public CubismModel3Json Model3Json { get; }
            public CubismModel Model { get; }

            public ModelImportContext(AssetImportContext ctx, CubismModel3Json model3Json, CubismModel model)
            {
                _ctx = ctx;
                Model3Json = model3Json;
                Model = model;
                _modelName = Path.GetFileNameWithoutExtension(AssetPath);
                var dot = _modelName.IndexOf('.');
                if (dot >= 0)
                    _modelName = _modelName.Substring(0, dot);
            }

            public void AddSubObject(Object subObject)
            {
                if (subObject != null)
                    _ctx.AddObjectToAsset(subObject.name ?? "sub", subObject);
            }

            public void DependsOnSourceAsset(string path)
            {
                if (!string.IsNullOrEmpty(path))
                    _ctx.DependsOnSourceAsset(path);
            }

            public void DependsOnArtifact(string path)
            {
                if (!string.IsNullOrEmpty(path))
                    _ctx.DependsOnArtifact(path);
            }
        }

        private enum OverrideOption
        {
            SameAsSettings,
            Yes,
            No
        }

        [SerializeField]
        private OverrideOption _overrideImportAsOriginalWorkflowOption = OverrideOption.SameAsSettings;

        private bool ShouldImportAsOriginalWorkflow
        {
            get
            {
                switch (_overrideImportAsOriginalWorkflowOption)
                {
                    case OverrideOption.Yes: return true;
                    case OverrideOption.No: return false;
                    default: return CubismUnityEditorMenu.ShouldImportAsOriginalWorkflow;
                }
            }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            _currentAssetPath = ctx.assetPath;
            _currentModel3Json = CubismModel3Json.LoadAtPath(ctx.assetPath);
            if (_currentModel3Json == null)
            {
                ctx.LogImportError("Unable to load model3.json file.");
                return;
            }

            var model3Json = _currentModel3Json;
            AssignDependencies(ctx, model3Json.FileReferences);

            var model = model3Json.ToModel(
                CubismImporter.OnPickDrawableMaterial,
                CubismImporter.OnPickTexture,
                CubismImporter.OnPickOffscreenMaterial,
                ShouldImportAsOriginalWorkflow);

            if (model == null)
            {
                ctx.LogImportError("Unable to import model data.");
                return;
            }

            var moc = model.Moc;
            if (moc != null)
            {
                moc.name = Path.GetFileNameWithoutExtension(model3Json.FileReferences.Moc);
                ctx.AddObjectToAsset("moc", moc);
            }

            ctx.AddObjectToAsset("model", model.gameObject);
            ctx.SetMainObject(model.gameObject);

            var importContext = new ModelImportContext(ctx, model3Json, model);
            OnDidImportModel?.Invoke(importContext);
            CubismImporter.SendModelImportEvent(this, model);

            foreach (var texture in model3Json.Textures ?? Array.Empty<Texture2D>())
            {
                if (texture != null)
                    CubismImporter.SendModelTextureImportEvent(importContext, model, texture);
            }
        }

        private static void AssignDependencies(AssetImportContext ctx, CubismModel3Json.SerializableFileReferences refs)
        {
            var baseDir = Path.GetDirectoryName(ctx.assetPath);
            string FullPath(string path)
            {
                if (string.IsNullOrWhiteSpace(path)) return null;
                return Path.Combine(baseDir, path);
            }

            if (!string.IsNullOrWhiteSpace(refs.Moc))
                ctx.DependsOnSourceAsset(FullPath(refs.Moc));

            if (refs.Textures != null)
            {
                foreach (var t in refs.Textures)
                {
                    var p = FullPath(t);
                    if (p != null) ctx.DependsOnSourceAsset(p);
                }
            }

            if (!string.IsNullOrWhiteSpace(refs.DisplayInfo))
                ctx.DependsOnSourceAsset(FullPath(refs.DisplayInfo));
            if (!string.IsNullOrWhiteSpace(refs.Physics))
                ctx.DependsOnSourceAsset(FullPath(refs.Physics));
            if (!string.IsNullOrWhiteSpace(refs.UserData))
                ctx.DependsOnSourceAsset(FullPath(refs.UserData));
            if (!string.IsNullOrWhiteSpace(refs.Pose))
                ctx.DependsOnSourceAsset(FullPath(refs.Pose));

            if (refs.Expressions != null)
            {
                foreach (var e in refs.Expressions)
                {
                    if (string.IsNullOrWhiteSpace(e.File)) continue;
                    var p = FullPath(e.File);
                    if (p != null) ctx.DependsOnSourceAsset(p);
                }
            }

            if (refs.Motions.Motions != null)
            {
                foreach (var group in refs.Motions.Motions)
                {
                    if (group == null) continue;
                    foreach (var m in group)
                    {
                        if (string.IsNullOrWhiteSpace(m.File)) continue;
                        var p = FullPath(m.File);
                        if (p != null) ctx.DependsOnSourceAsset(p);
                    }
                }
            }
        }
    }
}
