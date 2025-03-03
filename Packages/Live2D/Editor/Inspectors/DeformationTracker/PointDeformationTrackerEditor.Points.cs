using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Live2D.Cubism.Framework;

namespace Live2D.Cubism.Editor.Inspectors
{
    public sealed partial class PointDeformationTrackerEditor
    {
        // Default radius value
        private const float DEFAULT_HANDLE_RADIUS = 0.1f;

        #region Point Editor State

        private bool _isDeleteMode;
        private int _selectedPointIndex = -1;
        private bool _isDragging;
        private Vector3 _dragStartPosition;
        private Vector3 _draggingPoint;
        private bool _isAxisConstrained;
        private Vector3 _constraintOrigin;
        private bool _isAxisConstraintKeyPressed;
        private bool _isGuidelineKeyPressed;

        /// <summary>
        /// Resets the point editor state variables.
        /// Called from the main editor when resetting state.
        /// </summary>
        private void ResetPointsEditorState()
        {
            _selectedPointIndex = -1;
            _isDragging = false;
        }

        #endregion

        #region Unity Methods

        /// <summary>
        /// Handles the scene GUI interactions for deformation tracker points.
        /// </summary>
        private void OnSceneGUI()
        {
            if (!IsExpanded || !Tracker.enabled)
                return;

            var sceneView = SceneView.currentDrawingSceneView;
            var currentEvent = Event.current;

            SetupSceneViewForEditing();
            DrawAndHandlePoints(sceneView);

            if (!_isEditing)
                return;

            HandleKeyboardInteraction(sceneView);

            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == (int)MouseButton.LeftMouse &&
                currentEvent.control &&
                !_isDeleteMode)
            {
                CreatePointAtMousePosition();
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseMove)
                sceneView.Repaint();
        }

        #endregion

        #region Scene View Logic

        /// <summary>
        /// Sets up the scene view for editing tracked points.
        /// </summary>
        private void SetupSceneViewForEditing()
        {
            if (!_isEditing)
                return;

            Tools.current = Tool.None;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        /// <summary>
        /// Creates a new tracked point at the current mouse position.
        /// </summary>
        private void CreatePointAtMousePosition()
        {
            var mousePosition = Event.current.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var plane = new Plane(Tracker.transform.forward, Tracker.transform.position);

            if (plane.Raycast(ray, out float distance))
            {
                var newPointPosition = ray.GetPoint(distance);
                AddNewTrackedPoint(newPointPosition);
            }
        }

        /// <summary>
        /// Adds a new tracked point at the specified world position.
        /// </summary>
        /// <param name="position">World position to add the point.</param>
        private void AddNewTrackedPoint(Vector3 position)
        {
            Undo.RecordObject(Tracker, "Create Track Point");

            var localPosition = Tracker.transform.InverseTransformPoint(position);
            var point2D = new Vector2(localPosition.x, localPosition.y);

            var newTrackedPoint = new PointDeformationTracker.TrackedPoint
            {
                radius = DEFAULT_HANDLE_RADIUS
            };

            newTrackedPoint.vertexReferences = FindVerticesInRadius(
                point2D,
                DEFAULT_HANDLE_RADIUS,
                Tracker.includedDrawables
            );

            var points = Tracker.trackedPoints;
            Array.Resize(ref points, points.Length + 1);
            int newIndex = points.Length - 1;
            points[newIndex] = newTrackedPoint;

            Tracker.trackedPoints = points;
            EditorUtility.SetDirty(Tracker);
        }

        /// <summary>
        /// Draws and handles interactions with all tracked points.
        /// </summary>
        /// <param name="sceneView">Current scene view.</param>
        private void DrawAndHandlePoints(SceneView sceneView)
        {
            var tracker = Tracker;
            using (new Handles.DrawingScope(tracker.transform.localToWorldMatrix))
            {
                bool anyPointHovered = false;

                // Skip drawing if the tracker is disabled and not in edit mode
                if (!tracker.enabled && !_isEditing)
                    return;

                for (int i = 0; i < tracker.trackedPoints.Length; i++)
                {
                    var position = CalculatePointPosition(tracker, i);

                    if (_isEditing)
                    {
                        var (newPosition, isHovering) = HandlePointInteraction(i, position, sceneView);
                        position = newPosition;

                        if (isHovering)
                        {
                            anyPointHovered = true;
                        }
                    }

                    DrawPointVisuals(position, i, sceneView, _isEditing, tracker.enabled);
                }

                // If in editing mode and no point is hovered or being dragged, reset the selection
                if (_isEditing && !anyPointHovered && !_isDragging && Event.current.type == EventType.Layout)
                {
                    _selectedPointIndex = -1;
                }
            }
        }

        /// <summary>
        /// Handles user interaction with a tracked point.
        /// </summary>
        /// <param name="index">Index of the point being interacted with.</param>
        /// <param name="position">Position of the point.</param>
        /// <param name="sceneView">Current scene view.</param>
        /// <returns>A tuple with the new position after interaction and a flag indicating if the point is being hovered.</returns>
        private (Vector3 position, bool isHovering) HandlePointInteraction(int index, Vector3 position, SceneView sceneView)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            float distanceToMouse = HandleUtility.DistanceToCircle(position, Style.HANDLE_SIZE * 0.5f);
            float interactionThreshold = Style.HANDLE_SIZE * 1.5f;
            bool isWithinInteractionRange = distanceToMouse <= interactionThreshold;

            if (!isWithinInteractionRange && !(GUIUtility.hotControl == controlID && _isDragging) && _selectedPointIndex != index)
            {
                if (Event.current.type == EventType.Layout)
                {
                    HandleUtility.AddControl(controlID, distanceToMouse);
                }
                return (position, false);
            }

            if (isWithinInteractionRange && GUIUtility.hotControl == 0)
            {
                _selectedPointIndex = index;
            }

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (isWithinInteractionRange && Event.current.button == 0)
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
                    HandleUtility.AddControl(controlID, distanceToMouse);
                    break;

                case EventType.ScrollWheel:
                    if (isWithinInteractionRange && Event.current.control)
                    {
                        float scrollDelta = Event.current.delta.y;
                        AdjustPointRadius(index, scrollDelta);
                        Event.current.Use();
                        sceneView.Repaint();
                    }
                    break;
            }

            return (position, isWithinInteractionRange || (GUIUtility.hotControl == controlID && _isDragging));
        }

        /// <summary>
        /// Handles keyboard interactions for the editor.
        /// </summary>
        /// <param name="sceneView">Current scene view.</param>
        private void HandleKeyboardInteraction(SceneView sceneView)
        {
            var currentEvent = Event.current;

            if (currentEvent.type != EventType.KeyDown && currentEvent.type != EventType.KeyUp)
                return;

            bool isKeyDown = currentEvent.type == EventType.KeyDown;
            void UseEventAndRepaint()
            {
                currentEvent.Use();
                sceneView.Repaint();
            }

            switch (currentEvent.keyCode)
            {
                case KeyCode.E:
                    _isDeleteMode = isKeyDown;
                    UseEventAndRepaint();
                    break;

                case KeyCode.A:
                    if (isKeyDown && _isDragging && !_isAxisConstraintKeyPressed)
                    {
                        _isAxisConstrained = true;
                        _constraintOrigin = _draggingPoint;
                        _isAxisConstraintKeyPressed = true;
                    }
                    else if (!isKeyDown)
                    {
                        _isAxisConstraintKeyPressed = false;
                        if (_isDragging)
                        {
                            _isAxisConstrained = false;
                        }
                    }
                    UseEventAndRepaint();
                    break;

                case KeyCode.G:
                    _isGuidelineKeyPressed = isKeyDown;
                    if (isKeyDown && _isDragging)
                    {
                        _constraintOrigin = _draggingPoint;
                    }
                    UseEventAndRepaint();
                    break;
            }
        }

        /// <summary>
        /// Starts dragging a point.
        /// </summary>
        /// <param name="index">Index of the point to drag.</param>
        /// <param name="position">Initial position.</param>
        /// <param name="controlID">Control ID for the handle.</param>
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

        /// <summary>
        /// Updates the position of the point being dragged.
        /// </summary>
        /// <param name="position">Current position.</param>
        /// <param name="sceneView">Current scene view.</param>
        /// <returns>The updated position.</returns>
        private Vector3 UpdateDragPosition(Vector3 position, SceneView sceneView)
        {
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var plane = new Plane(Tracker.transform.forward, _dragStartPosition);

            if (plane.Raycast(ray, out float distance))
            {
                var newPoint = ray.GetPoint(distance);

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

        /// <summary>
        /// Finalizes the dragging operation and updates the point's data.
        /// </summary>
        /// <param name="index">Index of the point that was dragged.</param>
        private void FinalizeDrag(int index)
        {
            Undo.RecordObject(Tracker, "Move Tracked Point");

            var point = Tracker.trackedPoints[index];
            var newPosition = Tracker.transform.InverseTransformPoint(_draggingPoint);
            var point2D = new Vector2(newPosition.x, newPosition.y);

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

        /// <summary>
        /// Draws axis guidelines for constrained movement.
        /// </summary>
        /// <param name="origin">Origin point for the guidelines.</param>
        /// <param name="view">Current scene view.</param>
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
                Handles.color = Style.GuideLineColor;
                Handles.DrawLine(hStartWorld, hEndWorld, Style.GUIDE_LINE_WIDTH);
                Handles.DrawLine(vStartWorld, vEndWorld, Style.GUIDE_LINE_WIDTH);
            }
            else
            {
                var delta = _draggingPoint - _constraintOrigin;
                bool isHorizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);

                Handles.color = Style.GuideLineConstraintColor;
                if (isHorizontal)
                {
                    Handles.DrawLine(hStartWorld, hEndWorld, Style.GUIDE_LINE_WIDTH);
                    Handles.color = Style.GuideLineInactiveColor;
                    Handles.DrawLine(vStartWorld, vEndWorld, Style.GUIDE_LINE_WIDTH);
                }
                else
                {
                    Handles.DrawLine(vStartWorld, vEndWorld, Style.GUIDE_LINE_WIDTH);
                    Handles.color = Style.GuideLineInactiveColor;
                    Handles.DrawLine(hStartWorld, hEndWorld, Style.GUIDE_LINE_WIDTH);
                }
            }

            Handles.color = originalColor;
        }

        #endregion

        #region UI Logic

        /// <summary>
        /// Draws the visual representation of a tracked point.
        /// </summary>
        /// <param name="position">Position to draw at.</param>
        /// <param name="index">Index of the point.</param>
        /// <param name="sceneView">Current scene view.</param>
        /// <param name="isEditing">Whether the tracker is in edit mode.</param>
        /// <param name="isTrackerEnabled">Whether the tracker is enabled.</param>
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

            if (_showVertexConnections && (isEditing || isTrackerEnabled))
            {
                DrawVertexConnections(position, point);
            }

            if (isEditing)
            {
                Handles.color = Style.RadiusColor;
                Handles.DrawWireDisc(position, Vector3.forward, point.radius, 2f);
            }

            Handles.color = GetPointColor(index);
            DrawFullHandle(position);
        }

        /// <summary>
        /// Draws connections between a point and its referenced vertices.
        /// </summary>
        /// <param name="position">Position of the point.</param>
        /// <param name="point">The tracked point data.</param>
        private void DrawVertexConnections(Vector3 position, PointDeformationTracker.TrackedPoint point)
        {
            var originalColor = Handles.color;

            for (int i = 0; i < point.vertexReferences.Length; i++)
            {
                var vertexRef = point.vertexReferences[i];
                var drawable = Tracker.includedDrawables[vertexRef.drawableIndex];
                var vertex = drawable.VertexPositions[vertexRef.vertexIndex];

                float alpha = Mathf.Clamp01(vertexRef.weight * Style.VERTEX_WEIGHT_MULTIPLIER) * Style.VERTEX_ALPHA_MULTIPLIER;
                Handles.color = new Color(Style.VertexReferenceColor.r, Style.VertexReferenceColor.g,
                                         Style.VertexReferenceColor.b, alpha);

                Handles.DrawLine(position, vertex, Style.VERTEX_LINE_WIDTH);
                Handles.DrawSolidDisc(vertex, Vector3.forward, Style.HANDLE_SIZE * Style.VERTEX_POINT_SIZE_MULTIPLIER * alpha);
            }

            Handles.color = originalColor;
        }

        /// <summary>
        /// Gets the color for a point based on its state.
        /// </summary>
        /// <param name="index">Index of the point.</param>
        /// <returns>The color to use for the point.</returns>
        private Color GetPointColor(int index)
        {
            if (_isDeleteMode)
                return Style.HandleColorDelete;

            if (_selectedPointIndex == index)
                return Style.HandleColorSelected;

            return _isEditing ? Style.HandleColorEdit : Style.HandleColorNormal;
        }

        /// <summary>
        /// Draws a full handle representation for a point.
        /// </summary>
        /// <param name="position">Position to draw at.</param>
        private void DrawFullHandle(Vector3 position)
        {
            Handles.DrawWireArc(position, Vector3.forward, Vector3.right, 360f,
                               Style.HANDLE_SIZE * 0.5f, Style.HANDLE_LINE_WIDTH);

            float crossSize = Style.HANDLE_SIZE * Style.CROSS_SIZE_MULTIPLIER;
            Handles.DrawLine(
                position + new Vector3(-crossSize, -crossSize, 0),
                position + new Vector3(crossSize, crossSize, 0),
                Style.HANDLE_LINE_WIDTH
            );
            Handles.DrawLine(
                position + new Vector3(-crossSize, crossSize, 0),
                position + new Vector3(crossSize, -crossSize, 0),
                Style.HANDLE_LINE_WIDTH
            );
        }

        #endregion

        #region Auxiliary Code

        /// <summary>
        /// Converts a screen point to a world point on a plane.
        /// </summary>
        /// <param name="screenPoint">Screen point to convert.</param>
        /// <param name="plane">Plane to project onto.</param>
        /// <returns>World point on the plane.</returns>
        private Vector3 GetWorldPointOnPlane(Vector3 screenPoint, Plane plane)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(screenPoint);
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            return ray.origin;
        }

        /// <summary>
        /// Calculates the position of a tracked point.
        /// </summary>
        /// <param name="tracker">The tracker component.</param>
        /// <param name="index">Index of the point.</param>
        /// <returns>The calculated position.</returns>
        private Vector3 CalculatePointPosition(PointDeformationTracker tracker, int index)
        {
            if (!Application.isPlaying && _isDragging && _selectedPointIndex == index)
                return _draggingPoint;

            return tracker.GetTrackedPosition(index);
        }

        /// <summary>
        /// Deletes a tracked point.
        /// </summary>
        /// <param name="index">Index of the point to delete.</param>
        private void DeletePoint(int index)
        {
            Undo.RecordObject(Tracker, "Delete Track Point");

            var points = Tracker.trackedPoints;

            if (index < 0 || index >= points.Length)
                return;

            var newPoints = new PointDeformationTracker.TrackedPoint[points.Length - 1];

            if (index > 0)
                Array.Copy(points, 0, newPoints, 0, index);

            if (index < points.Length - 1)
                Array.Copy(points, index + 1, newPoints, index, points.Length - index - 1);

            Tracker.trackedPoints = newPoints;
            _selectedPointIndex = -1;

            EditorUtility.SetDirty(Tracker);
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Adjusts the radius of a tracked point.
        /// </summary>
        /// <param name="index">Index of the point.</param>
        /// <param name="delta">Amount to adjust by.</param>
        private void AdjustPointRadius(int index, float delta)
        {
            var tracker = Tracker;
            var point = tracker.trackedPoints[index];

            Vector3 position = CalculatePointPosition(tracker, index);
            Vector2 point2D = new Vector2(position.x, position.y);

            float newRadius = Mathf.Max(point.radius - delta * 0.002f, Style.RADIUS_MINIMUM_SIZE);

            Undo.RecordObject(tracker, "Change Point Radius");

            point.radius = newRadius;
            point.vertexReferences = FindVerticesInRadius(
                point2D,
                point.radius,
                tracker.includedDrawables
            );

            tracker.trackedPoints[index] = point;
            EditorUtility.SetDirty(tracker);
        }

        #endregion

        #region Styling Information
        /// <summary>
        /// Styling constants and colors for the point deformation tracker editor
        /// </summary>
        private static class Style
        {
            // Size constants
            public const float HANDLE_SIZE = 0.015f;
            public const float HANDLE_LINE_WIDTH = 4f;
            public const float GUIDE_LINE_WIDTH = 4f;
            public const float SMALL_POINT_SIZE_MULTIPLIER = 0.25f;
            public const float CROSS_SIZE_MULTIPLIER = 0.35f;
            public const float LABEL_POSITION_OFFSET_MULTIPLIER = 0.8f;
            public const float RADIUS_MINIMUM_SIZE = 0.0001f;

            // Colors
            public static readonly Color HandleColorNormal = new Color(1f, 0.92f, 0.016f);
            public static readonly Color HandleColorEdit = new Color(0.2f, 0.9f, 0.2f);
            public static readonly Color HandleColorSelected = new Color(0.2f, 0.2f, 0.9f);
            public static readonly Color HandleColorDelete = new Color(1f, 0f, 0f);
            public static readonly Color RadiusColor = new Color(0.4f, 0.7f, 1.0f, 0.3f);
            public static readonly Color VertexReferenceColor = new Color(0.2f, 0.6f, 1.0f, 0.8f);
            public static readonly Color GuideLineColor = new Color(1f, 1f, 1f, 0.75f);
            public static readonly Color GuideLineConstraintColor = new Color(1f, 0.5f, 0.5f, 1f);
            public static readonly Color GuideLineInactiveColor = new Color(1f, 1f, 1f, 0.5f);

            // Vertex connection constants
            public const float VERTEX_WEIGHT_MULTIPLIER = 1.6f;
            public const float VERTEX_ALPHA_MULTIPLIER = 0.7f;
            public const float VERTEX_LINE_WIDTH = 1.5f;
            public const float VERTEX_POINT_SIZE_MULTIPLIER = 0.5f;
        }


        #endregion
    }
}