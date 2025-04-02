using System.IO;
using Live2D.Cubism.Editor;
using Live2D.Cubism.Editor.Importers;
using Live2D.Cubism.Framework.Json;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Packages.Live2D.Editor.Importers
{
    [ScriptedImporter(1, "motion3.json", CubismImporterPriorities.Motion3JsonImporter)]
    public sealed class CubismMotion3JsonImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var motionName = Path.GetFileName(ctx.assetPath);

            var Motion3Json = CubismMotion3Json.LoadPath(ctx.assetPath);
            AnimationClip clip = Motion3Json.ToAnimationClip(ShouldImportAsOriginalWorkflow, ShouldClearAnimationCurves);
            clip.name = motionName;
            ctx.AddObjectToAsset("animation", clip);
            ctx.SetMainObject(clip);
            CubismImporter.SendMotionImportEvent(
                new CubismImporter.MotionImportContext
                {
                    AssetPath = ctx.assetPath,
                    Motion3Json = Motion3Json
                },
                clip
            );
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