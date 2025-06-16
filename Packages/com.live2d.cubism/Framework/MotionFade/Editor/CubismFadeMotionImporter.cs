/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework.Json;
using Packages.Live2D.Editor.Importers;
using Packages.Live2D.Editor.Importers.New;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            CubismModel3JsonImporter.OnDidImportModel += OnModelImport;
            CubismMotion3JsonImporter.OnDidImportMotion += OnFadeMotionImport;
        }

        #endregion

        #region Cubism Import Event Handling

        /// <summary>
        /// Create animator controller for MotionFade.
        /// </summary>
        /// <param name="importer">Event source.</param>
        /// <param name="model">Imported model.</param>
        private static void OnModelImport(IModelImportContext ctx)
        {
            bool hasMotions = ctx.Model3Json.FileReferences.Motions.Motions?.Any() == true;
            if (!hasMotions)
                return;

            var fadeController = ctx.Model.gameObject.GetOrAddComponent<CubismFadeController>();

            var fadeMotions = ScriptableObject.CreateInstance<CubismFadeMotionList>();
            fadeMotions.name = $"{ctx.ModelName}.fadeMotionList";
            ctx.AddSubObject(fadeMotions);

            fadeController.CubismFadeMotionList = fadeMotions;

            var fileReferences = ctx.Model3Json.FileReferences;

            // Create pose animation clip
            var motions = new List<CubismModel3Json.SerializableMotion>();
            if (fileReferences.Motions.GroupNames != null)
            {
                for (var i = 0; i < fileReferences.Motions.GroupNames.Length; i++)
                {
                    motions.AddRange(fileReferences.Motions.Motions[i]);
                }
            }

            var directoryPath2 = Path.GetDirectoryName(ctx.AssetPath);

            var motionFadeDataList = new List<CubismFadeMotionData>();
            var instanceIdList = new List<int>();

            for (var i = 0; i < motions.Count; ++i)
            {
                var motion = motions[i];
                var motionPath = Path.Combine(directoryPath2, motion.File);

                if (!File.Exists(motionPath))
                {
                    Debug.LogWarning($"CubismFadeMotionImporter : Can not find motion file: {motionPath}");
                    continue;
                }

                var motionFadeData = AssetDatabase.LoadAssetAtPath<CubismFadeMotionData>(motionPath);
                if (motionFadeData == null)
                {
                    Debug.LogWarning($"CubismFadeMotionImporter : Can not find motion fade data for {motionPath}");
                    continue;
                }

                var animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(motionPath);
                if (animationClip == null)
                {
                    Debug.LogWarning($"CubismFadeMotionImporter : Can not find animation clip for {motionPath}");
                    continue;
                }

                ctx.ImporterContext.DependsOnSourceAsset(motionPath);
                motionFadeDataList.Add(motionFadeData);
                instanceIdList.Add(animationClip.GetInstanceID());
            }

            fadeMotions.MotionInstanceIds = instanceIdList.ToArray();
            fadeMotions.CubismFadeMotionObjects = motionFadeDataList.ToArray();
        }

        /// <summary>
        /// Create oldFadeMotion.
        /// </summary>
        /// <param name="importer">Event source.</param>
        /// <param name="animationClip">Imported motion.</param>
        private static void OnFadeMotionImport(IMotionImportContext ctx)
        {
            // Create fade motion instance.
            var fadeMotion = CubismFadeMotionData.CreateInstance(
                ctx.Motion3Json,
                ctx.MotionName,
                ctx.AnimationClip.length,
                ctx.ShouldImportAsOriginalWorkflow,
                ctx.ShouldClearAnimationCurves,
                ctx.Model3Json);
            fadeMotion.name = $"{ctx.MotionName}.fadedata";
            ctx.AddSubObject(fadeMotion);

            // Add animation event
            var sourceAnimationEvents = AnimationUtility.GetAnimationEvents(ctx.AnimationClip);
            Array.Resize(ref sourceAnimationEvents, sourceAnimationEvents.Length + 1);
            sourceAnimationEvents[sourceAnimationEvents.Length - 1] = new AnimationEvent
            {
                time = 0,
                functionName = "InstanceId",
                //intParameter = instanceId,
                messageOptions = SendMessageOptions.DontRequireReceiver
            };
            AnimationUtility.SetAnimationEvents(ctx.AnimationClip, sourceAnimationEvents);
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

        #endregion
    }
}
