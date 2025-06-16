using System.IO;
using System.Linq;
using Live2D.Cubism.Editor;
using Live2D.Cubism.Framework.Json;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Pool;

namespace Packages.Live2D.Editor.Importers
{
    public interface IMotionImportContext
    {
        string AssetPath { get; }
        string MotionName { get; }
        CubismMotion3Json Motion3Json { get; }
        CubismModel3Json Model3Json { get; }
        AnimationClip AnimationClip { get; }

        bool ShouldImportAsOriginalWorkflow { get; }
        bool ShouldClearAnimationCurves { get; }

        void AddSubObject<T>(T subObject) where T : Object;

        T GetSubObject<T>() where T : Object;
    }

    [ScriptedImporter(1, "motion3.json", CubismImporterPriorities.Motion3JsonImporter)]
    public sealed class CubismMotion3JsonImporter : ScriptedImporter
    {

        private enum OverrideOption
        {
            SameAsSettings,
            Yes,
            No
        }

        [SerializeField]
        private OverrideOption _overrideImportAsOriginalWorkflowOption;

        [SerializeField]
        private OverrideOption _overrideClearAnimationCurvesOption;

        [SerializeField]
        private OverrideOption _overrideLoopProperty;

        private sealed class MotionImportContext : IMotionImportContext
        {
            private readonly AssetImportContext _ctx;
            private readonly CubismMotion3Json _motion3Json;
            private readonly AnimationClip _animationClip;
            private readonly CubismModel3Json _model3Json;
            private readonly string _motionName;
            private readonly bool _shouldImportAsOriginalWorkflow;
            private readonly bool _shouldClearAnimationCurves;

            public string AssetPath => _ctx.assetPath;
            public string MotionName => _motionName;
            public CubismMotion3Json Motion3Json => _motion3Json;
            public CubismModel3Json Model3Json => _model3Json;
            public AnimationClip AnimationClip => _animationClip;

            public bool ShouldImportAsOriginalWorkflow => _shouldImportAsOriginalWorkflow;
            public bool ShouldClearAnimationCurves => _shouldClearAnimationCurves;

            public MotionImportContext(AssetImportContext ctx, CubismMotion3Json motion3Json, AnimationClip animationClip, CubismModel3Json model3Json, bool shouldImportAsOriginalWorkflow, bool shouldClearAnimationCurves)
            {
                _ctx = ctx;
                _motion3Json = motion3Json;
                _animationClip = animationClip;
                _model3Json = model3Json;
                _shouldImportAsOriginalWorkflow = shouldImportAsOriginalWorkflow;
                _shouldClearAnimationCurves = shouldClearAnimationCurves;

                _motionName = Path.GetFileName(AssetPath)
                                .Replace(".motion3.json", "");
            }

            public void AddSubObject<T>(T subObject) where T : Object
            {
                var name = typeof(T).Name.ToLowerInvariant();
                _ctx.AddObjectToAsset(name, subObject);
            }

            public T GetSubObject<T>() where T : Object
            {
                var buffer = ListPool<Object>.Get();
                try
                {
                    _ctx.GetObjects(buffer);
                    return buffer.OfType<T>().FirstOrDefault();
                }
                finally
                {
                    ListPool<Object>.Release(buffer);
                }
            }
        }

        /// <summary>
        /// Callback on Cubism motions import as<see cref="AnimationClip"/>.
        /// </summary>
        /// <param name="importer">Importer.</param>
        /// <param name="animationClip">Generated animation.</param>
        public delegate void MotionImportHandler(IMotionImportContext ctx);

        /// <summary>
        /// Allows getting called back whenever a Cubism motions is imported (and before it is saved).
        /// </summary>
        public static event MotionImportHandler OnDidImportMotion;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var motionName = Path.GetFileName(ctx.assetPath);
            var Motion3Json = CubismMotion3Json.LoadPath(ctx.assetPath);
            if (Motion3Json == null)
            {
                ctx.LogImportError("unable to load motion3json file.");
                return;
            }

            var parentDirectory = Path.GetDirectoryName(ctx.assetPath);
            var model3JsonPath = FindModel3JsonFile(parentDirectory);
            if (model3JsonPath == null)
            {
                ctx.LogImportError("unable to find model3json file.");
                return;
            }

            var model3Json = CubismModel3Json.LoadAtPath(model3JsonPath);
            if (model3Json == null)
            {
                ctx.LogImportError("unable to load model3json file.");
                return;
            }

            var importedGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(model3JsonPath);
            if (importedGameObject == null)
            {
                ctx.LogImportError("unable to load model3json gameobject.");
                return;
            }

            AnimationClip clip = Motion3Json.ToAnimationClip(
                importedGameObject,
                new CubismMotion3Json.AnimationClipImportSettings
                {
                    shouldImportAsOriginalWorkflow = ShouldImportAsOriginalWorkflow,
                    shouldClearAnimationCurves = ShouldClearAnimationCurves,
                    OverrideLoop = _overrideLoopProperty switch
                    {
                        OverrideOption.Yes => true,
                        OverrideOption.No => false,
                        _ => null
                    }
                }
            );
            clip.name = motionName;
            ctx.AddObjectToAsset("animation", clip);
            ctx.SetMainObject(clip);
            ctx.DependsOnSourceAsset(model3JsonPath);

            if (OnDidImportMotion != null)
            {
                var motionCtx = new MotionImportContext(
                    ctx, Motion3Json, clip, model3Json,
                    ShouldImportAsOriginalWorkflow, ShouldClearAnimationCurves
                );
                OnDidImportMotion(motionCtx);
            }
        }

        /// <summary>
        /// Searches for a model3.json file in the current directory and its parent directories.
        /// </summary>
        /// <param name="startDirectory">The directory to start searching from.</param>
        /// <returns>The path to the model3.json file, or null if not found.</returns>
        private string FindModel3JsonFile(string startDirectory)
        {
            var currentDirectory = startDirectory;

            while (currentDirectory != null)
            {
                var model3JsonPath = Directory.EnumerateFiles(currentDirectory, "*.model3.json").FirstOrDefault();
                if (model3JsonPath != null)
                {
                    return model3JsonPath;
                }

                // Move up to parent directory
                currentDirectory = Path.GetDirectoryName(currentDirectory);
            }

            return null;
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

        /// <summary>
        /// Should clear animation clip curves.
        /// </summary>
        private bool ShouldClearAnimationCurves
        {
            get
            {
                return _overrideClearAnimationCurvesOption switch
                {
                    OverrideOption.Yes => true,
                    OverrideOption.No => false,
                    _ => CubismUnityEditorMenu.ShouldClearAnimationCurves
                };
            }
        }
    }
}