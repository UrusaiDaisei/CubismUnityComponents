using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using UnityEditor;
using UnityEngine;

namespace Live2D.Cubism.Editor
{
    [InitializeOnLoad]
    internal static class CubismAnimationPreviewRefresher
    {
        private static int _lastSelectedInstanceId = 0;
        private static CubismModel _cachedModel;
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

            UpdateModel(model);

            var activeView = SceneView.lastActiveSceneView;
            if (activeView != null)
                activeView.Repaint();
            else
                SceneView.RepaintAll();
        }

        private static void UpdateModel(CubismModel model)
        {
            model.ForceUpdateNow();

            var updateController = model.GetComponent<CubismUpdateController>();
            if (updateController != null)
            {
                updateController.Refresh();
                updateController.ForceUpdateNow();
            }
        }

        private static void OnSelectionChanged()
        {
            _cachedModel = null;
            _lastSelectedInstanceId = 0;
        }
    }
}