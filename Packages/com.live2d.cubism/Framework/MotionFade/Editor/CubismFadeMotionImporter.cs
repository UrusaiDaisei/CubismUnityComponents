/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Editor;
using Live2D.Cubism.Framework.Json;
using Packages.Live2D.Editor.Importers;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;


namespace Live2D.Cubism.Framework.MotionFade
{
    internal static class CubismFadeMotionImporter
    {
        #region Unity Event Handling

        /// <summary>
        /// Register fadeMotion importer.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RegisterMotionImporter()
        {
            CubismMotion3JsonImporter.OnDidImportMotion += OnFadeMotionImport;
        }

        #endregion

        #region Cubism Import Event Handling

        /// <summary>
        /// Create oldFadeMotion.
        /// </summary>
        /// <param name="ctx">Motion import context.</param>
        private static void OnFadeMotionImport(IMotionImportContext ctx)
        {
            var animationClip = ctx.AnimationClip;
            var instanceId = 0;
            var isExistInstanceId = false;
            var events = animationClip.events;
            for (var k = 0; k < events.Length; ++k)
            {
                if (events[k].functionName != "InstanceId")
                {
                    continue;
                }

                instanceId = events[k].intParameter;
                isExistInstanceId = true;
                break;
            }

            if (!isExistInstanceId)
            {
                instanceId = animationClip.GetInstanceID();
            }

            // Motion importer owns fade motion data as a motion3.json sub-asset.
            CreateOrUpdateFadeMotionSubAsset(ctx.AssetPath, ctx.Motion3Json, animationClip, ctx.Model3Json, ctx);

            // Add animation event
            {
                var sourceAnimationEvents = AnimationUtility.GetAnimationEvents(animationClip);
                var index = -1;

                for(var i = 0; i < sourceAnimationEvents.Length; ++i)
                {
                    if(sourceAnimationEvents[i].functionName != "InstanceId")
                    {
                        continue;
                    }

                    index = i;
                    break;
                }

                if(index == -1)
                {
                    index = sourceAnimationEvents.Length;
                    Array.Resize(ref sourceAnimationEvents, sourceAnimationEvents.Length + 1);
                    sourceAnimationEvents[sourceAnimationEvents.Length - 1] = new AnimationEvent();
                }

                sourceAnimationEvents[index].time = 0;
                sourceAnimationEvents[index].functionName = "InstanceId";
                sourceAnimationEvents[index].intParameter = instanceId;
                sourceAnimationEvents[index].messageOptions = SendMessageOptions.DontRequireReceiver;

                AnimationUtility.SetAnimationEvents(animationClip, sourceAnimationEvents);
            }
        }

        #endregion


        #region Functions

        /// <summary>
        /// Create animator controller for MotionFade.
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns>Animator controller attached CubismFadeStateObserver.</returns>
        public static AnimatorController CreateAnimatorController(string assetPath)
        {
            var animatorController = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            animatorController.layers[0].stateMachine.AddStateMachineBehaviour<CubismFadeStateObserver>();

            return animatorController;
        }

        /// <summary>
        /// Creates or updates <see cref="CubismFadeMotionData"/> as a sub-asset of the corresponding .motion3.json.
        /// </summary>
        /// <param name="motion3JsonAssetsPath">Path of target .motion3.json.</param>
        /// <param name="motion3Json">Target <see cref="CubismMotion3Json"/> instance.</param>
        /// <param name="animationClip">Imported motion clip.</param>
        /// <param name="model3Json"><see cref="CubismModel3Json"/> instance for fade times.</param>
        /// <param name="motionImportContext">Motion import context.</param>
        private static void CreateOrUpdateFadeMotionSubAsset(
            string motion3JsonAssetsPath,
            CubismMotion3Json motion3Json,
            AnimationClip animationClip,
            CubismModel3Json model3Json,
            IMotionImportContext motionImportContext)
        {
            if (motionImportContext == null)
            {
                return;
            }

            var existingFadeMotion = LoadFadeMotionSubAssetAtPath(motion3JsonAssetsPath);
            if (existingFadeMotion != null)
            {
                var newFadeMotion = CubismFadeMotionData.CreateInstance(
                    existingFadeMotion,
                    motion3Json,
                    motion3JsonAssetsPath,
                    animationClip.length,
                    CubismUnityEditorMenu.ShouldImportAsOriginalWorkflow,
                    CubismUnityEditorMenu.ShouldClearAnimationCurves,
                    model3Json);

                EditorUtility.CopySerialized(newFadeMotion, existingFadeMotion);
                EditorUtility.SetDirty(existingFadeMotion);
                return;
            }

            var fadeMotion = CubismFadeMotionData.CreateInstance(
                motion3Json,
                motion3JsonAssetsPath,
                animationClip.length,
                CubismUnityEditorMenu.ShouldImportAsOriginalWorkflow,
                CubismUnityEditorMenu.ShouldClearAnimationCurves,
                model3Json);

            motionImportContext.AddSubObject(fadeMotion);
            EditorUtility.SetDirty(fadeMotion);
        }

        /// <summary>
        /// Loads the fade motion sub-asset attached to a .motion3.json asset path.
        /// </summary>
        /// <param name="motion3JsonAssetsPath">Path of target .motion3.json asset.</param>
        /// <returns>Existing fade motion sub-asset, or null if not found.</returns>
        private static CubismFadeMotionData LoadFadeMotionSubAssetAtPath(string motion3JsonAssetsPath)
        {
            var allSubAssets = AssetDatabase.LoadAllAssetsAtPath(motion3JsonAssetsPath);

            if (allSubAssets == null || allSubAssets.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < allSubAssets.Length; i++)
            {
                var fadeMotion = allSubAssets[i] as CubismFadeMotionData;

                if (fadeMotion != null)
                {
                    return fadeMotion;
                }
            }

            return null;
        }

        #endregion
    }
}
