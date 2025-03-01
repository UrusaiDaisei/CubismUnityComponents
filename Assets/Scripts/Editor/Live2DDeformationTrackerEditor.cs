using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Live2DDeformationTracker))]
public sealed partial class Live2DDeformationTrackerEditor : Editor
{
    #region UI Constants

    private static readonly Color k_LabelBackgroundColor = new Color(0, 0, 0, 0.7f);
    private static GUIStyle k_LabelStyle = null;

    #endregion

    #region Runtime State

    private VisualElement _root;
    private Button _editButton;
    private bool _isEditing;
    private int _selectedPointIndex = -1;
    private bool _isDragging;
    private Vector3 _dragStartPosition;
    private Vector3 _draggingPoint;
    private bool _isAxisConstrained;
    private Vector3 _constraintOrigin;
    private bool _isAxisConstraintKeyPressed;
    private bool _isGuidelineKeyPressed;

    private bool IsExpanded => InternalEditorUtility.GetIsInspectorExpanded(target);
    private Live2DDeformationTracker Tracker => target as Live2DDeformationTracker;

    #endregion

    #region Unity Methods

    public override VisualElement CreateInspectorGUI()
    {
        _root = new VisualElement();

        AddStyleSheet();
        CreateInspectorElements();

        return _root;

        void AddStyleSheet()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Live2DDeformationTrackerEditor.uss");
            if (styleSheet != null)
                _root.styleSheets.Add(styleSheet);
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
                }

                var propertyField = new PropertyField(iterator);
                _root.Add(propertyField);
            }
        }
    }

    private void OnEnable()
    {
        if (k_LabelStyle == null)
            k_LabelStyle = CreateLabelStyle();

        ResetEditorState();
    }

    private void OnDisable()
    {
        ResetEditorState();
        SceneView.RepaintAll();
    }

    private void OnSceneGUI()
    {
        if (!IsExpanded)
            return;

        SetupSceneViewForEditing();
        DrawAndHandlePoints(SceneView.currentDrawingSceneView);

        if (!_isEditing)
            return;

        // New point creation logic
        if (Event.current.type == EventType.MouseDown && Event.current.button == (int)MouseButton.LeftMouse && Event.current.control)
        {
            CreatePointAtMousePosition();
            Event.current.Use(); // Consume the event
        }

        if (Event.current.type == EventType.MouseMove)
            SceneView.currentDrawingSceneView.Repaint();
    }

    private void CreatePointAtMousePosition()
    {
        Vector3 mousePosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        Plane plane = new Plane(Tracker.transform.forward, Tracker.transform.position);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 newPointPosition = ray.GetPoint(distance);
            AddNewTrackedPoint(newPointPosition);
        }
    }

    private void AddNewTrackedPoint(Vector3 position)
    {
        Undo.RecordObject(Tracker, "Create Track Point");

        // Create a new tracked point and calculate barycentric data using the existing method
        var newTrackedPoint = new Live2DDeformationTracker.TrackedPoint
        {
            trackingData = CalculateBarycentricData(position, Tracker.targetDrawable)
        };

        // Add the new point to the tracked points array
        Array.Resize(ref Tracker.trackedPoints, Tracker.trackedPoints.Length + 1);
        Tracker.trackedPoints[Tracker.trackedPoints.Length - 1] = newTrackedPoint;

        // Mark the tracker as dirty to save changes
        EditorUtility.SetDirty(Tracker);
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

    private void SetupSceneViewForEditing()
    {
        if (!_isEditing)
            return;

        Tools.current = Tool.None;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
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

    #endregion
}