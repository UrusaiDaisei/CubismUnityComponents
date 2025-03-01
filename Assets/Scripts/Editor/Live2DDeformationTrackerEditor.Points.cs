using Live2D.Cubism.Core;
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
    private const float LABEL_POSITION_OFFSET_MULTIPLIER = 1.2f;

    private static readonly Color k_HandleColorNormal = new Color(1f, 0.92f, 0.016f);
    private static readonly Color k_HandleColorEdit = new Color(0.2f, 0.9f, 0.2f);
    private static readonly Color k_HandleColorSelected = new Color(0.2f, 0.2f, 0.9f);

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
                    StartPointDrag(index, position, controlID);
                    Event.current.Use();
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
        Vector3 localPoint = Tracker.transform.InverseTransformPoint(_draggingPoint);
        UpdatePointPosition(index, localPoint);

        _isDragging = false;
        _isAxisConstrained = false;
        GUIUtility.hotControl = 0;
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
        if (_isDragging && _selectedPointIndex == index)
        {
            position = _draggingPoint;

            if (_isAxisConstrained || _isGuidelineKeyPressed)
            {
                var point = _isGuidelineKeyPressed && !_isAxisConstrained
                    ? _draggingPoint
                    : _constraintOrigin;
                DrawAxisGuidelines(point, sceneView);
            }
        }

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

    private Color GetPointColor(int index)
    {
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
        Handles.DrawWireArc(position, Vector3.forward, Vector3.right, 360f, HANDLE_SIZE * 0.5f, HANDLE_LINE_WIDTH);

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
        Handles.Label(labelPosition, index.ToString(), k_LabelStyle);
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
        if (Application.isPlaying)
            return tracker.GetCurrentPosition(index);

        return tracker.CalculateWeightedPosition(index);
    }

    private void UpdatePointPosition(int index, Vector3 localPosition)
    {
        if (index < 0 || index >= Tracker.trackedPoints.Length)
            return;

        Vector2 point2D = new Vector2(localPosition.x, localPosition.y);
        var newData = CalculateBarycentricData(point2D, Tracker.targetDrawable);

        var points = Tracker.trackedPoints;
        points[index].trackingData = newData;

        EditorUtility.SetDirty(Tracker);
    }

    private static Live2DDeformationTracker.BarycentricData CalculateBarycentricData(Vector2 point, CubismDrawable targetDrawable)
    {
        float minDistance = float.MaxValue;
        Live2DDeformationTracker.BarycentricData bestData = default;

        var vertices = targetDrawable.VertexPositions;
        var indices = targetDrawable.Indices;

        for (int i = 0; i < indices.Length; i += 3)
        {
            int i1 = indices[i];
            int i2 = indices[i + 1];
            int i3 = indices[i + 2];

            var barycentricCoords = CalculateBarycentricCoordinates(
                point, vertices[i1], vertices[i2], vertices[i3]);

            bool isInside = barycentricCoords.x >= 0 && barycentricCoords.y >= 0 &&
                           barycentricCoords.z >= 0 &&
                           (barycentricCoords.x + barycentricCoords.y + barycentricCoords.z <= 1.001f);

            if (isInside)
            {
                return new Live2DDeformationTracker.BarycentricData(barycentricCoords, i1, i2, i3);
            }

            var testPoint = vertices[i1] * barycentricCoords.x +
                           vertices[i2] * barycentricCoords.y +
                           vertices[i3] * barycentricCoords.z;
            float distance = Vector3.Distance(point, testPoint);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestData = new Live2DDeformationTracker.BarycentricData(barycentricCoords, i1, i2, i3);
            }
        }

        return bestData;

        static Vector3 CalculateBarycentricCoordinates(Vector2 point, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 v0 = b - a;
            Vector3 v1 = c - a;
            Vector3 v2 = (Vector3)point - a;

            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;
            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return new Vector3(u, v, w);
        }
    }

    #endregion
}