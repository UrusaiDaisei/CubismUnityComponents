using Live2D.Cubism.Core;
using System.Collections.Generic;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Rendering.Masking;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Live2D.Cubism.Editor
{
    [InitializeOnLoad]
    internal static class CubismAnimationPreviewRefresher
    {
        private static int _lastSelectedInstanceId = 0;
        private static CubismModel _cachedModel;
        private static CubismMaskController[] _cachedMaskControllers;
        private static CommandBuffer _sharedCmd;
        private static double _lastRunTime = 0.0;
        private static bool _wasInAnimationMode = false;

        private const double MinUpdateIntervalSeconds = 1.0 / 60.0; // ~60 FPS throttle in preview

        static CubismAnimationPreviewRefresher()
        {
            EditorApplication.update += OnEditorUpdate;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!AnimationMode.InAnimationMode())
            {
                if (_wasInAnimationMode)
                {
                    UpdateAnimationPreview();
                }
                _wasInAnimationMode = false;
                return;
            }
            _wasInAnimationMode = AnimationMode.InAnimationMode();

            var now = EditorApplication.timeSinceStartup;
            if (now - _lastRunTime < MinUpdateIntervalSeconds)
                return;
            _lastRunTime = now;

            UpdateAnimationPreview();
        }

        private static GameObject SelectPreviewGameObject()
        {
            return Selection.activeGameObject;
        }

        private static void UpdateAnimationPreview()
        {
            var selected = SelectPreviewGameObject();
            // Refresh cache on selection change
            if (selected != null && (selected.GetInstanceID() != _lastSelectedInstanceId || _cachedModel == null))
            {
                _lastSelectedInstanceId = selected.GetInstanceID();
                _cachedModel = selected.FindCubismModel(true);
                _cachedMaskControllers = _cachedModel != null
                    ? _cachedModel.GetComponentsInChildren<CubismMaskController>(true)
                    : selected.GetComponentsInChildren<CubismMaskController>(true);
            }

            var model = _cachedModel;
            if (model == null)
                return;

            var animationWindows = Resources.FindObjectsOfTypeAll<AnimationWindow>();
            if (animationWindows != null && animationWindows.Length > 0)
            {
                var window = animationWindows[0];

                if (window.playing && window.animationClip != null)
                {
                    window.animationClip.SampleAnimation(model.gameObject, window.time);
                }
            }

            UpdateModel(model, _cachedMaskControllers);

            var activeView = SceneView.lastActiveSceneView;
            if (activeView != null)
                activeView.Repaint();
            else
                SceneView.RepaintAll();
        }

        private static void UpdateModel(CubismModel model, CubismMaskController[] maskControllers)
        {
            model.ForceUpdateNow();

            var updateController = model.GetComponent<CubismUpdateController>();
            if (updateController != null)
            {
                updateController.Refresh();
                updateController.ForceUpdateNow();
            }

            // Ensure clipping masks refresh in edit mode by drawing mask textures directly
            if (maskControllers != null && maskControllers.Length > 0)
            {
                var uniqueTextures = new HashSet<CubismMaskTexture>();

                for (int i = 0; i < maskControllers.Length; i++)
                {
                    var maskController = maskControllers[i];
                    if (maskController == null || !maskController.isActiveAndEnabled)
                        continue;

                    var maskTexture = maskController.MaskTexture;
                    if (maskTexture == null)
                        continue;

                    // Ensure registered once
                    maskTexture.AddSource(maskController);

                    uniqueTextures.Add(maskTexture);
                }

                foreach (var maskTexture in uniqueTextures)
                {
                    var commandSource = (ICubismMaskCommandSource)maskTexture;
                    int rtCount = maskTexture.RenderTextureCount;
                    if (rtCount <= 0)
                    {
                        _sharedCmd ??= new CommandBuffer { name = "CubismMaskPreview" };
                        _sharedCmd.Clear();
                        commandSource.AddToCommandBuffer(_sharedCmd, false, -1);
                        Graphics.ExecuteCommandBuffer(_sharedCmd);
                    }
                    else
                    {
                        for (int bufferIndex = 0; bufferIndex < rtCount; bufferIndex++)
                        {
                            _sharedCmd ??= new CommandBuffer { name = "CubismMaskPreview" };
                            _sharedCmd.Clear();
                            commandSource.AddToCommandBuffer(_sharedCmd, true, bufferIndex);
                            Graphics.ExecuteCommandBuffer(_sharedCmd);
                        }
                    }
                }
            }
        }

        private static void OnSelectionChanged()
        {
            _cachedModel = null;
            _cachedMaskControllers = null;
            _lastSelectedInstanceId = 0;
        }
    }
}