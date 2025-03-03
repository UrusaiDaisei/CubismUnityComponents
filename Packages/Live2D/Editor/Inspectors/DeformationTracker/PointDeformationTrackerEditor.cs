using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Live2D.Cubism.Framework;

namespace Live2D.Cubism.Editor.Inspectors
{
    [CustomEditor(typeof(PointDeformationTracker))]
    public sealed partial class PointDeformationTrackerEditor : UnityEditor.Editor
    {
        #region UI Constants

        private const string BasePath = "Packages/com.live2d.cubism/Editor/Inspectors/DeformationTracker/";

        #endregion

        #region Runtime State

        private VisualElement _root;
        private Button _editButton;
        private Button _includeDrawablesButton;
        private Toggle _showVertexConnectionsToggle;
        private Label _statsTextLabel;
        private VisualElement _editInstructionsContainer;
        private VisualElement _instructionsList;
        private bool _isEditing;
        private bool _showVertexConnections = false;

        private bool IsExpanded => InternalEditorUtility.GetIsInspectorExpanded(target);
        private PointDeformationTracker Tracker => target as PointDeformationTracker;

        #endregion

        #region Unity Methods

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();

            // Load and instantiate the UXML file
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BasePath + "PointDeformationTrackerEditor.uxml");
            if (visualTree == null)
                throw new System.Exception("Could not load PointDeformationTrackerEditor.uxml.");

            // Instantiate the UXML
            visualTree.CloneTree(_root);

            // Load the style sheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(BasePath + "PointDeformationTrackerEditor.uss");
            if (styleSheet != null)
            {
                _root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("Could not load PointDeformationTrackerEditor.uss style sheet.");
            }

            // Get references to UI elements
            _editButton = _root.Q<Button>("edit-points-button");
            _includeDrawablesButton = _root.Q<Button>("include-drawables-button");
            _showVertexConnectionsToggle = _root.Q<Toggle>("show-vertex-connections");
            _statsTextLabel = _root.Q<Label>("stats-text");
            _editInstructionsContainer = _root.Q("edit-instructions-container");
            _instructionsList = _root.Q("instructions-list");

            // Add script field to script container
            var scriptContainer = _root.Q("script-container");
            var scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty != null && scriptContainer != null)
            {
                var scriptField = new PropertyField(scriptProperty);
                scriptField.SetEnabled(false);
                scriptContainer.Add(scriptField);
            }

            // Bind toggle for vertex connections
            if (_showVertexConnectionsToggle != null)
            {
                _showVertexConnectionsToggle.value = _showVertexConnections;
                _showVertexConnectionsToggle.RegisterValueChangedCallback(evt =>
                {
                    _showVertexConnections = evt.newValue;
                    SceneView.RepaintAll();
                });
            }

            // Setup UI bindings
            SetupUIBindings();

            // Initialize UI data
            UpdateUI();

            return _root;
        }

        private void SetupUIBindings()
        {
            // Setup button click events
            if (_editButton != null)
            {
                _editButton.clicked += ToggleEditMode;
                UpdateEditButtonState();
            }

            if (_includeDrawablesButton != null)
            {
                _includeDrawablesButton.clicked += ShowIncludeDrawablesDialog;
            }
        }

        private void UpdateUI()
        {
            UpdateStatsText();
            UpdateEditInstructionsVisibility();
        }

        private void UpdateStatsText()
        {
            if (_statsTextLabel == null || Tracker == null)
                return;

            int drawablesCount = Tracker.includedDrawables?.Length ?? 0;
            int pointsCount = Tracker.trackedPoints?.Length ?? 0;

            _statsTextLabel.text = $"Statistics: {drawablesCount} drawables, {pointsCount} tracked points";
        }

        private void UpdateEditInstructionsVisibility()
        {
            if (_editInstructionsContainer == null)
                return;

            _editInstructionsContainer.style.display = _isEditing ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateEditButtonState()
        {
            if (_editButton == null)
                return;

            // Update button appearance based on edit mode
            if (_isEditing)
            {
                _editButton.text = "Exit Edit Mode";
                _editButton.AddToClassList("editing");
            }
            else
            {
                _editButton.text = "Edit Points";
                _editButton.RemoveFromClassList("editing");
            }
        }

        private void OnEnable()
        {
            ResetEditorState();
        }

        private void OnDisable()
        {
            ResetEditorState();
            SceneView.RepaintAll();
        }

        #endregion

        #region UI Setup

        private void ToggleEditMode()
        {
            _isEditing = !_isEditing;
            UpdateEditButtonState();

            // Update edit instructions visibility
            UpdateEditInstructionsVisibility();

            // If exiting edit mode, reset the editor state
            if (!_isEditing)
            {
                ResetEditorState();
            }

            SceneView.RepaintAll();
        }

        private void ResetEditorState()
        {
            if (_isEditing)
                return;

            // These references will need to call methods in the Points.cs file
            ResetPointsEditorState();
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