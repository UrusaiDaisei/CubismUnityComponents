using System.IO;
using System.Linq;
using Live2D.Cubism.Editor;
using Live2D.Cubism.Framework.Json;
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
        AnimationClip AnimationClip { get; }
        void AddSubObject<T>(T subObject) where T : Object;

        T GetSubObject<T>() where T : Object;
    }

    [ScriptedImporter(1, "motion3.json", CubismImporterPriorities.Motion3JsonImporter)]
    public sealed class CubismMotion3JsonImporter : ScriptedImporter
    {
        private sealed class MotionImportContext : IMotionImportContext
        {
            private readonly AssetImportContext _ctx;
            private readonly CubismMotion3Json _motion3Json;
            private readonly AnimationClip _animationClip;

            public string AssetPath => _ctx.assetPath;
            public string MotionName => Path.GetFileName(AssetPath);
            public CubismMotion3Json Motion3Json => _motion3Json;
            public AnimationClip AnimationClip => _animationClip;
            public MotionImportContext(AssetImportContext ctx, CubismMotion3Json motion3Json, AnimationClip animationClip)
            {
                _ctx = ctx;
                _motion3Json = motion3Json;
                _animationClip = animationClip;
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
            AnimationClip clip = Motion3Json.ToAnimationClip(ShouldImportAsOriginalWorkflow, ShouldClearAnimationCurves);
            clip.name = motionName;
            ctx.AddObjectToAsset("animation", clip);
            ctx.SetMainObject(clip);

            if (OnDidImportMotion != null)
            {
                var motionCtx = new MotionImportContext(ctx, Motion3Json, clip);
                OnDidImportMotion(motionCtx);
            }
        }

        /// <summary>
        /// Should import as original workflow.
        /// </summary>
        private bool ShouldImportAsOriginalWorkflow
        {
            get
            {
                return CubismUnityEditorMenu.ShouldImportAsOriginalWorkflow;
            }
        }

        /// <summary>
        /// Should clear animation clip curves.
        /// </summary>
        private bool ShouldClearAnimationCurves
        {
            get
            {
                return CubismUnityEditorMenu.ShouldClearAnimationCurves;
            }
        }
    }
}