using Live2D.Cubism.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

[CustomEditor(typeof(Live2DDeformationTracker))]
public class Live2DDeformationTrackerEditor : Editor
{
    private const float HANDLE_SIZE = 0.015f;
    private const float HANDLE_LINE_WIDTH = 4f;

    private static readonly Color k_HandleColorNormal = new Color(1f, 0.92f, 0.016f);
    private static readonly Color k_HandleColorEdit = new Color(0.2f, 0.9f, 0.2f);

    private VisualElement _root;
    private Button _editButton;
    private bool _isEditing;

    public override VisualElement CreateInspectorGUI()
    {
        _root = new VisualElement();

        // Load and clone the default USS
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Live2DDeformationTrackerEditor.uss");
        if (styleSheet != null)
            _root.styleSheets.Add(styleSheet);

        // Add the default inspector
        var tracker = target as Live2DDeformationTracker;
        var iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.propertyPath == "trackedPoints")
            {
                // Add edit button before points array
                CreateEditButton();
            }

            var propertyField = new PropertyField(iterator);
            _root.Add(propertyField);
        }

        return _root;
    }

    private void CreateEditButton()
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

    private void OnEnable()
    {
        // Subscribe to scene view events
        SceneView.duringSceneGui += OnSceneGUIGlobal;

        // Make sure we get priority for events
        EditorApplication.update += CheckEditMode;
    }

    private void OnDisable()
    {
        // Turn off edit mode when disabled
        _isEditing = false;

        // Unsubscribe from scene view events
        SceneView.duringSceneGui -= OnSceneGUIGlobal;
        EditorApplication.update -= CheckEditMode;

        SceneView.RepaintAll();
    }

    private void CheckEditMode()
    {
        if (_isEditing)
        {
            // Force the scene view to repaint to ensure our event handling is active
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUIGlobal(SceneView sceneView)
    {
        // Only intercept events when in edit mode
        if (!_isEditing) return;

        // Get the current event
        var currentEvent = Event.current;

        // Use our helper method to determine if we should capture this event
        if (ShouldCaptureEvent(currentEvent))
        {
            // Prevent the event from being processed further
            currentEvent.Use();

            // Force a repaint to ensure our visuals stay updated
            if (currentEvent.type == EventType.MouseMove)
            {
                SceneView.RepaintAll();
            }
        }
    }

    // Helper method to determine which events to capture
    private bool ShouldCaptureEvent(Event evt)
    {
        // Capture left mouse button events (button 0)
        if ((evt.type == EventType.MouseDown ||
             evt.type == EventType.MouseUp ||
             evt.type == EventType.MouseDrag) &&
            evt.button == 0)
        {
            return true;
        }

        // Capture all keyboard events
        if (evt.type == EventType.KeyDown ||
            evt.type == EventType.KeyUp)
        {
            return true;
        }

        return false;
    }

    private void ToggleEditMode()
    {
        _isEditing = !_isEditing;
        UpdateEditButtonState();
        SceneView.RepaintAll();
    }

    private void UpdateEditButtonState()
    {
        if (_editButton == null) return;

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

    private void OnSceneGUI()
    {
        var tracker = target as Live2DDeformationTracker;
        if (!tracker || !tracker.enabled || tracker.targetDrawable == null) return;

        // Hide the default transform tools when in edit mode
        if (_isEditing)
        {
            Tools.current = Tool.None;

            // Disable selection in the scene view
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        using (new Handles.DrawingScope(tracker.transform.localToWorldMatrix))
        {
            DrawTrackedPoints(tracker);
        }
    }

    private void DrawTrackedPoints(Live2DDeformationTracker tracker)
    {
        var vertices = tracker.targetDrawable.VertexPositions;

        // Draw all points
        for (int i = 0; i < tracker.trackedPoints.Length; i++)
        {
            var point = tracker.trackedPoints[i];
            if (point.trackingData.vertexIndices == null) continue;

            Vector3 position = CalculatePointPosition(tracker, point, i, vertices);

            // Determine handle color based on state
            Color handleColor = _isEditing ? k_HandleColorEdit : k_HandleColorNormal;

            DrawPointHandle(position, i, handleColor);
        }
    }

    private Vector3 CalculatePointPosition(Live2DDeformationTracker tracker, Live2DDeformationTracker.TrackedPoint point, int index, Vector3[] vertices)
    {
        if (Application.isPlaying)
        {
            return tracker.GetCurrentPosition(index);
        }

        Vector3 position = Vector3.zero;
        var indices = point.trackingData.vertexIndices;
        var weights = point.trackingData.weights;

        for (int j = 0; j < 3; j++)
        {
            position += vertices[indices[j]] * weights[j];
        }

        return position;
    }

    private void DrawPointHandle(Vector3 position, int index, Color color)
    {
        Handles.color = color;

        // Draw circle
        Handles.DrawWireArc(position, Vector3.forward, Vector3.right, 360f, HANDLE_SIZE * 0.5f, HANDLE_LINE_WIDTH);

        // Draw X
        float crossSize = HANDLE_SIZE * 0.35f;
        Handles.DrawLine(
            position + new Vector3(-crossSize, -crossSize, 0),
            position + new Vector3(crossSize, crossSize, 0),
            HANDLE_LINE_WIDTH
        );
        Handles.DrawLine(
            position + new Vector3(-crossSize, crossSize, 0),
            position + new Vector3(crossSize, -crossSize, 0),
            HANDLE_LINE_WIDTH
        );

        // Only draw index label if Alt key is pressed
        if (Event.current.alt)
        {
            // Create a style with improved visibility
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(5, 5, 2, 2)
            };

            // Add a background texture to the style
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
            backgroundTexture.Apply();
            style.normal.background = backgroundTexture;

            // Position slightly above the handle
            Vector3 labelPosition = position + Vector3.up * HANDLE_SIZE * 1.2f;

            // Draw the label with the enhanced style
            Handles.Label(labelPosition, index.ToString(), style);
        }
    }
}