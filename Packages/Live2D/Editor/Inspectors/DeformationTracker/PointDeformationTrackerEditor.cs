using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using System.Linq;

namespace Live2D.Cubism.Editor.Inspectors
{
    // Reference to the partial file containing IncludeDrawablesWindow
    // Assets/Scripts/Editor/PointDeformationTrackerIncludeDrawablesWindow.cs

    [CustomEditor(typeof(PointDeformationTracker))]
    public sealed partial class PointDeformationTrackerEditor : UnityEditor.Editor
    {
        #region UI Constants

        private const string BasePath = "Packages/com.live2d.cubism/Editor/Inspectors/DeformationTracker/";

        private static readonly Color k_LabelBackgroundColor = new Color(0, 0, 0, 0.7f);
        private static GUIStyle k_LabelStyle = null;
        private static GUIStyle LabelStyle
        {
            get
            {
                if (k_LabelStyle == null)
                    k_LabelStyle = CreateLabelStyle();

                return k_LabelStyle;
            }
        }

        // Default radius value
        private const float DEFAULT_RADIUS = 0.1f;

        #endregion

        #region Runtime State

        private VisualElement _root;
        private Button _editButton;
        private bool _isEditing;
        private bool _isDeleteMode;
        private int _selectedPointIndex = -1;
        private bool _isDragging;
        private Vector3 _dragStartPosition;
        private Vector3 _draggingPoint;
        private bool _isAxisConstrained;
        private Vector3 _constraintOrigin;
        private bool _isAxisConstraintKeyPressed;
        private bool _isGuidelineKeyPressed;
        private bool _showVertexConnections = false;

        private bool IsExpanded => InternalEditorUtility.GetIsInspectorExpanded(target);
        private PointDeformationTracker Tracker => target as PointDeformationTracker;

        private CubismDrawable[] _previousDrawables = new CubismDrawable[0];

        private bool _styleSheetLoaded = false;

        #endregion

        #region Unity Methods

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();

            TryLoadStyleSheet();
            CreateInspectorElements();

            return _root;

            void TryLoadStyleSheet()
            {
                if (_styleSheetLoaded) return;

                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(BasePath + "PointDeformationTrackerEditor.uss");
                if (styleSheet == null)
                {
                    Debug.LogWarning("Could not load PointDeformationTrackerEditor.uss style sheet.");
                    return;
                }

                _root.styleSheets.Add(styleSheet);
                _styleSheetLoaded = true;
            }

            void CreateInspectorElements()
            {
                var iterator = serializedObject.GetIterator();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (iterator.propertyPath == "trackedPoints")
                    {
                        CreateEditModeButton();
                        CreateIncludeDrawablesButton();
                        CreateVisualizationToggles();
                    }

                    // Skip adding the includedDrawables field directly to the inspector
                    if (iterator.propertyPath == "includedDrawables")
                    {
                        // Display a read-only version of the field
                        var container = new VisualElement();
                        var header = new Label("Included Drawables (Use the 'Include Drawables' button to modify)");
                        header.style.unityFontStyleAndWeight = FontStyle.Bold;
                        header.style.marginTop = 5;
                        header.style.marginBottom = 5;

                        container.Add(header);

                        // Create a disabled property field
                        var propertyField = new PropertyField(iterator);
                        propertyField.SetEnabled(false);

                        container.Add(propertyField);
                        _root.Add(container);

                        continue; // Skip the default property field addition below
                    }

                    var field = new PropertyField(iterator);
                    _root.Add(field);
                }
            }
        }

        private void CreateIncludeDrawablesButton()
        {
            var container = new VisualElement();
            container.style.marginBottom = 10;

            var includeButton = new Button(ShowIncludeDrawablesDialog)
            {
                text = "Include Drawables"
            };
            includeButton.AddToClassList("edit-button");

            container.Add(includeButton);
            _root.Add(container);
        }

        private void OnEnable()
        {
            // Cache the current list of drawables for change detection
            PointDeformationTracker tracker = target as PointDeformationTracker;
            if (tracker != null && tracker.includedDrawables != null)
            {
                _previousDrawables = new CubismDrawable[tracker.includedDrawables.Length];
                tracker.includedDrawables.CopyTo(_previousDrawables, 0);
            }

            ResetEditorState();
        }

        private void OnDisable()
        {
            ResetEditorState();
            SceneView.RepaintAll();
        }

        #endregion

        #region UI Setup

        private void CreateEditModeButton()
        {
            var container = new VisualElement();
            container.style.marginBottom = 10;

            _editButton = new Button(ToggleEditMode)
            {
                text = "Edit Points"
            };
            _editButton.AddToClassList("edit-button");

            container.Add(_editButton);
            UpdateEditButtonState();
            _root.Add(container);
        }

        private void CreateVisualizationToggles()
        {
            var container = new VisualElement();
            container.style.marginBottom = 10;
            container.style.marginTop = 10;

            var titleLabel = new Label("Visualization Options");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 5;
            container.Add(titleLabel);

            var connectionsToggle = new Toggle("Show Vertex Connections") { value = _showVertexConnections };
            connectionsToggle.RegisterValueChangedCallback(evt =>
            {
                _showVertexConnections = evt.newValue;
                SceneView.RepaintAll();
            });

            container.Add(connectionsToggle);
            _root.Add(container);
        }

        /// <summary>
        /// Recalculates all tracked points for a PointDeformationTracker.
        /// Call this after changing the included drawables list.
        /// </summary>
        public static void RecalculateTrackedPoints(PointDeformationTracker tracker)
        {
            if (tracker == null || tracker.trackedPoints == null || tracker.trackedPoints.Length == 0)
                return;

            Undo.RecordObject(tracker, "Update Tracked Points");

            // Recalculate vertex references for each point
            for (int i = 0; i < tracker.trackedPoints.Length; i++)
            {
                var point = tracker.trackedPoints[i];

                // Calculate current position
                var currentPosition = tracker.CalculatePointPosition(i);
                Vector2 point2D = new Vector2(currentPosition.x, currentPosition.y);

                // Recalculate vertex references using the current position and radius
                point.vertexReferences = FindVerticesInRadius(
                    point2D,
                    point.radius,
                    tracker.includedDrawables
                );

                tracker.trackedPoints[i] = point;
            }

            EditorUtility.SetDirty(tracker);
        }

        private void ToggleEditMode()
        {
            _isEditing = !_isEditing;
            ResetEditorState();
            UpdateEditButtonState();
            SceneView.RepaintAll();
        }

        private void UpdateEditButtonState()
        {
            if (_editButton == null)
                return;

            _editButton.text = _isEditing ? "Done Editing" : "Edit Points";

            if (_isEditing)
            {
                _editButton.AddToClassList("editing");
            }
            else
            {
                _editButton.RemoveFromClassList("editing");
            }
        }

        private void ResetEditorState()
        {
            if (_isEditing)
                return;

            _selectedPointIndex = -1;
            _isDragging = false;
        }

        private static GUIStyle CreateLabelStyle()
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(5, 5, 2, 2)
            };

            style.normal.background = CreateLabelBackground();

            return style;
        }

        private static Texture2D CreateLabelBackground()
        {
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, k_LabelBackgroundColor);
            backgroundTexture.Apply();
            return backgroundTexture;
        }

        private void ShowIncludeDrawablesDialog()
        {
            var allDrawables = Tracker.Model.Drawables;
            if (allDrawables == null || allDrawables.Length == 0)
            {
                EditorUtility.DisplayDialog("No Drawables", "No drawables found in the model.", "OK");
                return;
            }

            // Create a dialog to select which drawables to include
            DrawableSelectionWindow.ShowWindow(Tracker, allDrawables);
        }

        #endregion
    }
}