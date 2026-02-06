/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Core;
using Live2D.Cubism.Editor;
using Live2D.Cubism.Framework.Json;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Live2D.Cubism.Editor.Importers
{
    /// <summary>
    /// Context passed when a motion is imported via the ScriptedImporter (motion3.json).
    /// </summary>
    public interface IMotionImportContext
    {
        string AssetPath { get; }
        string MotionName { get; }
        CubismMotion3Json Motion3Json { get; }
        CubismModel3Json Model3Json { get; }
        AnimationClip AnimationClip { get; }
        bool ShouldImportAsOriginalWorkflow { get; }
        bool ShouldClearAnimationCurves { get; }
        void AddSubObject(Object subObject);
    }

    /// <summary>
    /// ScriptedImporter for motion3.json. Exposes <see cref="IMotionImportContext"/> and <see cref="OnDidImportMotion"/>.
    /// </summary>
    [ScriptedImporter(1, "motion3.json", CubismImporterPriorities.Motion3JsonImporter)]
    public sealed class CubismMotion3JsonImporter : ScriptedImporter
    {
        /// <summary>
        /// Callback when a motion is imported. Use for custom post-import logic.
        /// </summary>
        public static event Action<IMotionImportContext> OnDidImportMotion;

        /// <summary>
        /// Path of the asset being imported. Valid only during the current OnImportAsset / motion-import callbacks.
        /// </summary>
        public string AssetPath => _currentAssetPath;

        /// <summary>
        /// Motion3Json for the asset being imported. Valid only during the current OnImportAsset / motion-import callbacks.
        /// </summary>
        public CubismMotion3Json Motion3Json => _currentMotion3Json;

        [NonSerialized]
        private string _currentAssetPath;

        [NonSerialized]
        private CubismMotion3Json _currentMotion3Json;

        private sealed class MotionImportContext : IMotionImportContext
        {
            private readonly AssetImportContext _ctx;
            private readonly string _motionName;

            public string AssetPath => _ctx.assetPath;
            public string MotionName => _motionName;
            public CubismMotion3Json Motion3Json { get; }
            public CubismModel3Json Model3Json { get; }
            public AnimationClip AnimationClip { get; }
            public bool ShouldImportAsOriginalWorkflow { get; }
            public bool ShouldClearAnimationCurves { get; }

            public MotionImportContext(
                AssetImportContext ctx,
                CubismMotion3Json motion3Json,
                AnimationClip animationClip,
                CubismModel3Json model3Json,
                bool shouldImportAsOriginalWorkflow,
                bool shouldClearAnimationCurves)
            {
                _ctx = ctx;
                Motion3Json = motion3Json;
                AnimationClip = animationClip;
                Model3Json = model3Json;
                ShouldImportAsOriginalWorkflow = shouldImportAsOriginalWorkflow;
                ShouldClearAnimationCurves = shouldClearAnimationCurves;
                _motionName = Path.GetFileName(ctx.assetPath).Replace(".motion3.json", "");
            }

            public void AddSubObject(Object subObject)
            {
                if (subObject != null)
                    _ctx.AddObjectToAsset(subObject.name ?? "sub", subObject);
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

        [SerializeField]
        private OverrideOption _overrideClearAnimationCurvesOption = OverrideOption.SameAsSettings;

        private bool ShouldImportAsOriginalWorkflow =>
            _overrideImportAsOriginalWorkflowOption == OverrideOption.Yes
                ? true
                : _overrideImportAsOriginalWorkflowOption == OverrideOption.No
                    ? false
                    : CubismUnityEditorMenu.ShouldImportAsOriginalWorkflow;

        private bool ShouldClearAnimationCurves =>
            _overrideClearAnimationCurvesOption == OverrideOption.Yes
                ? true
                : _overrideClearAnimationCurvesOption == OverrideOption.No
                    ? false
                    : CubismUnityEditorMenu.ShouldClearAnimationCurves;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            _currentAssetPath = ctx.assetPath;
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ctx.assetPath));
            var jsonText = File.ReadAllText(fullPath);
            _currentMotion3Json = CubismMotion3Json.LoadFrom(jsonText);
            if (_currentMotion3Json == null)
            {
                ctx.LogImportError("Unable to load motion3.json file.");
                return;
            }

            var motion3Json = _currentMotion3Json;
            var parentDirectory = Path.GetDirectoryName(ctx.assetPath);
            var model3JsonPath = FindModel3JsonFile(parentDirectory);
            if (model3JsonPath == null)
            {
                ctx.LogImportError("Unable to find model3.json file.");
                return;
            }

            ctx.DependsOnSourceAsset(model3JsonPath);

            var model3Json = CubismModel3Json.LoadAtPath(model3JsonPath);
            var motionName = Path.GetFileName(ctx.assetPath).Replace(".motion3.json", "");
            var animationClip = motion3Json.ToAnimationClip(ShouldImportAsOriginalWorkflow, ShouldClearAnimationCurves);
            if (animationClip == null)
            {
                ctx.LogImportError("Unable to create animation clip.");
                return;
            }

            animationClip.name = motionName;
            ctx.AddObjectToAsset("animation", animationClip);
            ctx.SetMainObject(animationClip);

            var importContext = new MotionImportContext(
                ctx, motion3Json, animationClip, model3Json,
                ShouldImportAsOriginalWorkflow, ShouldClearAnimationCurves);
            OnDidImportMotion?.Invoke(importContext);
            CubismImporter.SendMotionImportEvent(this, animationClip);
        }

        private static string FindModel3JsonFile(string startDirectory)
        {
            if (string.IsNullOrEmpty(startDirectory))
                return null;

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var currentDirectory = startDirectory;

            while (!string.IsNullOrEmpty(currentDirectory))
            {
                var fullDir = Path.GetFullPath(Path.Combine(projectRoot, currentDirectory));
                if (Directory.Exists(fullDir))
                {
                    var files = Directory.GetFiles(fullDir, "*.model3.json");
                    if (files.Length > 0)
                    {
                        var fileName = Path.GetFileName(files[0]);
                        return Path.Combine(currentDirectory, fileName).Replace("\\", "/");
                    }
                }

                currentDirectory = Path.GetDirectoryName(currentDirectory);
                if (currentDirectory != null && !currentDirectory.StartsWith("Assets"))
                    break;
            }

            return null;
        }
    }
}
