using System;
using UnityEditor;
using UnityEngine;

public sealed partial class Live2DDeformationTrackerEditor
{
    #region UI Constants

    private const float HANDLE_SIZE = 0.015f;
    private const float HANDLE_LINE_WIDTH = 4f;
    private const float GUIDE_LINE_WIDTH = 4f;

    private const float SMALL_POINT_SIZE_MULTIPLIER = 0.25f;
    private const float CROSS_SIZE_MULTIPLIER = 0.35f;
    private const float LABEL_POSITION_OFFSET_MULTIPLIER = 0.8f;

    private const float RADIUS_MINIMUM_SIZE = 0.0001f;

    private static readonly Color k_HandleColorNormal = new Color(1f, 0.92f, 0.016f);
    private static readonly Color k_HandleColorEdit = new Color(0.2f, 0.9f, 0.2f);
    private static readonly Color k_HandleColorSelected = new Color(0.2f, 0.2f, 0.9f);
    private static readonly Color k_HandleColorDelete = new Color(1f, 0f, 0f);
    private static readonly Color k_RadiusColor = new Color(0.4f, 0.7f, 1.0f, 0.3f);
    private static readonly Color k_VertexReferenceColor = new Color(0.2f, 0.6f, 1.0f, 0.8f);

    private static readonly Color k_GuideLineColor = new Color(1f, 1f, 1f, 0.75f);
    private static readonly Color k_GuideLineConstraintColor = new Color(1f, 0.5f, 0.5f, 1f);
    private static readonly Color k_GuideLineInactiveColor = new Color(1f, 1f, 1f, 0.5f);

    #endregion

    #region Point Handling

    private void DrawAndHandlePoints(SceneView sceneView)
    {
        var tracker = Tracker;
        using (new Handles.DrawingScope(tracker.transform.localToWorldMatrix))
        {
            for (int i = 0; i < tracker.trackedPoints.Length; i++)
            {
                var position = CalculatePointPosition(tracker, i);

                if (_isEditing)
                {
                    position = HandlePointInteraction(i, position, sceneView);
                }

                DrawPointVisuals(position, i, sceneView, _isEditing, tracker.enabled);
            }
        }
    }

    private Vector3 HandlePointInteraction(int index, Vector3 position, SceneView sceneView)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.KeyDown:
                switch (Event.current.keyCode)
                {
                    case KeyCode.E:
                        _isDeleteMode = true;
                        Event.current.Use();
                        sceneView.Repaint();
                        break;

                    case KeyCode.A:
                        if (_isDragging && !_isAxisConstraintKeyPressed)
                        {
                            _isAxisConstrained = true;
                            _constraintOrigin = _draggingPoint;
                        }
                        _isAxisConstraintKeyPressed = true;
                        Event.current.Use();
                        sceneView.Repaint();
                        break;

                    case KeyCode.G:
                        _isGuidelineKeyPressed = true;
                        if (_isDragging)
                        {
                            _constraintOrigin = _draggingPoint;
                        }
                        Event.current.Use();
                        sceneView.Repaint();
                        break;
                }
                break;

            case EventType.KeyUp:
                switch (Event.current.keyCode)
                {
                    case KeyCode.E:
                        _isDeleteMode = false;
                        Event.current.Use();
                        sceneView.Repaint();
                        break;

                    case KeyCode.A:
                        _isAxisConstraintKeyPressed = false;
                        if (_isDragging)
                        {
                            _isAxisConstrained = false;
                        }
                        Event.current.Use();
                        sceneView.Repaint();
                        break;

                    case KeyCode.G:
                        _isGuidelineKeyPressed = false;
                        Event.current.Use();
                        sceneView.Repaint();
                        break;
                }
                break;

            case EventType.MouseDown:
                if (HandleUtility.nearestControl == controlID && Event.current.button == 0)
                {
                    if (_isDeleteMode)
                    {
                        DeletePoint(index);
                        Event.current.Use();
                        sceneView.Repaint();
                        GUIUtility.ExitGUI();
                    }
                    else
                    {
                        StartPointDrag(index, position, controlID);
                        Event.current.Use();
                        sceneView.Repaint();
                    }
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID && _isDragging)
                {
                    position = UpdateDragPosition(position, sceneView);
                    Event.current.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID && _isDragging)
                {
                    FinalizeDrag(index);
                    Event.current.Use();
                }
                break;

            case EventType.Layout:
                HandleUtility.AddControl(
                    controlID,
                    HandleUtility.DistanceToCircle(position, HANDLE_SIZE * 0.5f)
                );
                break;

            case EventType.Repaint:
                UpdatePointSelection(index, controlID);
                break;

            case EventType.ScrollWheel:
                if (Event.current.control)
                {
                    // Get distance from mouse to control to check if we're over it
                    Vector2 screenPos = HandleUtility.WorldToGUIPoint(position);
                    float distance = Vector2.Distance(screenPos, Event.current.mousePosition);

                    // Only adjust radius if mouse is close enough to the point
                    if (distance < 20f)
                    {
                        // Store the delta before consuming the event
                        float scrollDelta = Event.current.delta.y;

                        // Consume the event to prevent camera zoom
                        Event.current.Use();

                        // Adjust the point radius
                        AdjustPointRadius(index, scrollDelta, sceneView);

                        return position;
                    }
                }
                break;
        }

        return position;
    }

    private void StartPointDrag(int index, Vector3 position, int controlID)
    {
        _selectedPointIndex = index;
        _isDragging = true;
        _dragStartPosition = position;
        _draggingPoint = position;
        _isAxisConstrained = false;
        _isAxisConstraintKeyPressed = false;
        _isGuidelineKeyPressed = false;

        GUIUtility.hotControl = controlID;
    }

    private Vector3 UpdateDragPosition(Vector3 position, SceneView sceneView)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Plane plane = new Plane(Tracker.transform.forward, _dragStartPosition);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 newPoint = ray.GetPoint(distance);

            if (_isAxisConstraintKeyPressed && !_isAxisConstrained)
            {
                _isAxisConstrained = true;
                _constraintOrigin = newPoint;
            }
            else if (!_isAxisConstraintKeyPressed && _isAxisConstrained)
            {
                _isAxisConstrained = false;
            }

            if (_isAxisConstrained)
            {
                var delta = newPoint - _constraintOrigin;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    newPoint.y = _constraintOrigin.y;
                }
                else
                {
                    newPoint.x = _constraintOrigin.x;
                }
            }

            _draggingPoint = newPoint;
            position = _draggingPoint;
        }

        sceneView.Repaint();
        return position;
    }

    private void FinalizeDrag(int index)
    {
        Undo.RecordObject(Tracker, "Move Tracked Point");

        var point = Tracker.trackedPoints[index];
        var newPosition = Tracker.transform.InverseTransformPoint(_draggingPoint);

        // Update vertex references based on new position
        Vector2 point2D = new Vector2(newPosition.x, newPosition.y);

        point.vertexReferences = FindVerticesInRadius(
            point2D,
            point.radius,
            Tracker.includedDrawables
        );

        Tracker.trackedPoints[index] = point;

        _isDragging = false;
        _isAxisConstrained = false;
        GUIUtility.hotControl = 0;

        EditorUtility.SetDirty(Tracker);
        SceneView.RepaintAll();
    }

    private void UpdatePointSelection(int index, int controlID)
    {
        if (HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0)
        {
            _selectedPointIndex = index;
        }
        else if (GUIUtility.hotControl == 0 && _selectedPointIndex == index)
        {
            _selectedPointIndex = -1;
        }
    }

    private void DrawPointVisuals(Vector3 position, int index, SceneView sceneView, bool isEditing, bool isTrackerEnabled)
    {
        var tracker = Tracker;
        var point = tracker.trackedPoints[index];

        if (_isDragging && _selectedPointIndex == index)
        {
            position = _draggingPoint;

            if (_isAxisConstrained || _isGuidelineKeyPressed)
            {
                var constraintPoint = _isGuidelineKeyPressed && !_isAxisConstrained
                    ? _draggingPoint
                    : _constraintOrigin;
                DrawAxisGuidelines(constraintPoint, sceneView);
            }
        }

        // Draw vertex connections first (drawn behind other elements)
        if (_showVertexConnections && (isEditing || isTrackerEnabled))
        {
            DrawVertexConnections(position, point);
        }

        // Always draw radius circles when editing, regardless of toggle state
        if (isEditing)
        {
            // Draw radius circle
            Handles.color = k_RadiusColor;
            Handles.DrawWireDisc(position, Vector3.forward, point.radius, 2f);
        }

        // Draw the main handle - this should always be drawn last to ensure visibility
        Handles.color = GetPointColor(index);

        if (!isTrackerEnabled && !isEditing)
        {
            DrawSimplePoint(position);
        }
        else
        {
            DrawFullHandle(position);
        }

        if (Event.current.alt)
        {
            DrawIndexLabel(position, index);
        }
    }

    private void DrawVertexConnections(Vector3 position, Live2DDeformationTracker.TrackedPoint point)
    {
        const float VERTEX_WEIGHT_MULTIPLIER = 1.6f;
        const float VERTEX_ALPHA_MULTIPLIER = 0.7f;
        const float VERTEX_LINE_WIDTH = 1.5f;
        const float VERTEX_POINT_SIZE_MULTIPLIER = 0.5f;

        var originalColor = Handles.color;

        // Draw lines to each referenced vertex
        for (int i = 0; i < point.vertexReferences.Length; i++)
        {
            var vertexRef = point.vertexReferences[i];
            var drawable = Tracker.includedDrawables[vertexRef.drawableIndex];
            var vertex = drawable.VertexPositions[vertexRef.vertexIndex];

            // Color based on weight - more influence = more opacity but reduced to improve handle visibility
            float alpha = Mathf.Clamp01(vertexRef.weight * VERTEX_WEIGHT_MULTIPLIER) * VERTEX_ALPHA_MULTIPLIER;
            Handles.color = new Color(k_VertexReferenceColor.r, k_VertexReferenceColor.g,
                                     k_VertexReferenceColor.b, alpha);

            // Draw connection line
            Handles.DrawLine(position, vertex, VERTEX_LINE_WIDTH);

            // Draw small point at vertex position
            Handles.DrawSolidDisc(vertex, Vector3.forward, HANDLE_SIZE * VERTEX_POINT_SIZE_MULTIPLIER * alpha);
        }

        Handles.color = originalColor;
    }

    private Color GetPointColor(int index)
    {
        if (_isDeleteMode)
            return k_HandleColorDelete;

        if (_selectedPointIndex == index)
            return k_HandleColorSelected;

        return _isEditing ? k_HandleColorEdit : k_HandleColorNormal;
    }

    private void DrawSimplePoint(Vector3 position)
    {
        float smallPointSize = HANDLE_SIZE * SMALL_POINT_SIZE_MULTIPLIER;
        Handles.DrawSolidDisc(position, Vector3.forward, smallPointSize);
    }

    private void DrawFullHandle(Vector3 position)
    {
        // Then draw the wire circle on top
        Handles.DrawWireArc(position, Vector3.forward, Vector3.right, 360f,
                           HANDLE_SIZE * 0.5f, HANDLE_LINE_WIDTH);

        float crossSize = HANDLE_SIZE * CROSS_SIZE_MULTIPLIER;
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
    }

    private void DrawIndexLabel(Vector3 position, int index)
    {
        Vector3 labelPosition = position + Vector3.up * HANDLE_SIZE * LABEL_POSITION_OFFSET_MULTIPLIER;
        Handles.Label(labelPosition, index.ToString(), LabelStyle);
    }

    private void DrawAxisGuidelines(Vector3 origin, SceneView view)
    {
        var originalColor = Handles.color;
        var transform = Tracker.transform;

        Vector3 originWorld = transform.TransformPoint(origin);
        Vector3 originScreen = HandleUtility.WorldToGUIPoint(originWorld);

        Plane plane = new Plane(transform.forward, originWorld);

        var viewWidth = view.position.width;
        var viewHeight = view.position.height;

        var hStart = new Vector3(0, originScreen.y, 0);
        var hEnd = new Vector3(viewWidth, originScreen.y, 0);
        var vStart = new Vector3(originScreen.x, 0, 0);
        var vEnd = new Vector3(originScreen.x, viewHeight, 0);

        Vector3 hStartWorld = GetWorldPointOnPlane(hStart, plane);
        Vector3 hEndWorld = GetWorldPointOnPlane(hEnd, plane);
        Vector3 vStartWorld = GetWorldPointOnPlane(vStart, plane);
        Vector3 vEndWorld = GetWorldPointOnPlane(vEnd, plane);

        if (_isGuidelineKeyPressed && !_isAxisConstrained)
        {
            Handles.color = k_GuideLineColor;
            Handles.DrawLine(hStartWorld, hEndWorld, GUIDE_LINE_WIDTH);
            Handles.DrawLine(vStartWorld, vEndWorld, GUIDE_LINE_WIDTH);
        }
        else
        {
            var delta = _draggingPoint - _constraintOrigin;
            bool isHorizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);

            Handles.color = k_GuideLineConstraintColor;
            if (isHorizontal)
            {
                Handles.DrawLine(hStartWorld, hEndWorld, GUIDE_LINE_WIDTH);
                Handles.color = k_GuideLineInactiveColor;
                Handles.DrawLine(vStartWorld, vEndWorld, GUIDE_LINE_WIDTH);
            }
            else
            {
                Handles.DrawLine(vStartWorld, vEndWorld, GUIDE_LINE_WIDTH);
                Handles.color = k_GuideLineInactiveColor;
                Handles.DrawLine(hStartWorld, hEndWorld, GUIDE_LINE_WIDTH);
            }
        }

        Handles.color = originalColor;
    }

    private Vector3 GetWorldPointOnPlane(Vector3 screenPoint, Plane plane)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(screenPoint);
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return ray.origin;
    }

    private Vector3 CalculatePointPosition(Live2DDeformationTracker tracker, int index)
    {
        // In play mode, use the runtime position
        if (Application.isPlaying)
            return tracker.GetCurrentPosition(index);

        // If dragging this point, return drag position
        if (_isDragging && _selectedPointIndex == index)
            return _draggingPoint;

        // Otherwise calculate from vertex references
        if (index >= 0 && index < tracker.trackedPoints.Length)
        {
            var point = tracker.trackedPoints[index];
            if (point.vertexReferences != null && point.vertexReferences.Length > 0)
                return tracker.CalculatePointPosition(index);
        }

        // Fallback for points with no references
        return Vector3.zero;
    }

    private void DeletePoint(int index)
    {
        // Record the state for undo before making any changes
        Undo.RecordObject(Tracker, "Delete Track Point");

        var points = Tracker.trackedPoints;

        // Check if the index is valid
        if (index < 0 || index >= points.Length)
            return;

        // Create a new array with one less element
        var newPoints = new Live2DDeformationTracker.TrackedPoint[points.Length - 1];

        // Copy elements before the index
        if (index > 0)
            Array.Copy(points, 0, newPoints, 0, index);

        // Copy elements after the index
        if (index < points.Length - 1)
            Array.Copy(points, index + 1, newPoints, index, points.Length - index - 1);

        Tracker.trackedPoints = newPoints;

        // Reset selection
        _selectedPointIndex = -1;

        EditorUtility.SetDirty(Tracker);
        SceneView.RepaintAll();
    }

    private void AdjustPointRadius(int index, float delta, SceneView sceneView)
    {
        var tracker = Tracker;
        var point = tracker.trackedPoints[index];

        // Calculate current position
        Vector3 position = CalculatePointPosition(tracker, index);
        Vector2 point2D = new Vector2(position.x, position.y);

        // Adjust radius with mouse wheel (negative to make scrolling down reduce radius)
        // Reduced scale factor for more subtle adjustments
        float newRadius = Mathf.Max(point.radius - delta * 0.002f, RADIUS_MINIMUM_SIZE); // Smaller scale factor for finer control

        Undo.RecordObject(tracker, "Change Point Radius");

        point.radius = newRadius;

        // Update vertex references based on new radius
        point.vertexReferences = FindVerticesInRadius(
            point2D,
            point.radius,
            tracker.includedDrawables
        );

        tracker.trackedPoints[index] = point;
        EditorUtility.SetDirty(tracker);

        sceneView.Repaint();
    }

    #endregion
}