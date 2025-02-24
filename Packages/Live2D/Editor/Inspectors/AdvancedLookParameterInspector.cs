using Live2D.Cubism.Framework.LookAt;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="AdvancedLookParameter"/>s.
    /// </summary>
    [CustomEditor(typeof(AdvancedLookParameter))]
    internal sealed class AdvancedLookParameterInspector : UnityEditor.Editor
    {
        private VisualElement _root;
        private const string UxmlPath = "Packages/com.live2d.cubism/Editor/Inspectors/AdvancedLookParameterInspector.uxml";
        private const string UssPath = "Packages/com.live2d.cubism/Editor/Inspectors/AdvancedLookParameterInspector.uss";

        private const float HandleSize = 0.1f;
        private const float CrosshairSize = 0.3f;
        private const float RangeHandleSize = 0.15f;
        private const float RangeArrowSize = 0.3f;
        private const float TestButtonSize = 0.15f;
        private const float TestButtonOffset = 0.3f;
        private readonly Color HandleColor = new Color(0f, 1f, 1f, 0.8f);
        private readonly Color SelectedColor = new Color(1f, 1f, 0f, 0.8f);
        private readonly Color RangeColor = new Color(0f, 1f, 0.5f, 0.8f);
        private readonly Color TestButtonColor = new Color(0f, 0.8f, 0.2f, 0.8f);

        // Control IDs for handle interaction
        private int _centerPointControlID;
        private int _minDistanceControlID;
        private int _maxDistanceControlID;
        private int _testButtonControlID;
        private bool _isDragging;
        private bool _isTestingMode;
        private float _originalParameterValue;

        private void OnEnable()
        {
            Tools.hidden = true;
            _isDragging = false;
            _isTestingMode = false;
        }

        private void OnDisable()
        {
            Tools.hidden = false;
            if (_isTestingMode)
            {
                RestoreParameterValue();
            }
        }

        private void OnSceneGUI()
        {
            var lookParameter = target as AdvancedLookParameter;
            if (lookParameter == null) return;

            var transform = lookParameter.transform;
            var axisDirection = GetAxisDirection(transform);
            var localPos = new Vector3(lookParameter.CenterPoint.x, lookParameter.CenterPoint.y, 0f);
            var centerPoint = transform.TransformPoint(localPos);
            var minPoint = centerPoint + axisDirection * lookParameter.MinDistance;
            var maxPoint = centerPoint + axisDirection * lookParameter.MaxDistance;

            // Get control IDs
            _centerPointControlID = GUIUtility.GetControlID(FocusType.Passive);
            _minDistanceControlID = GUIUtility.GetControlID(FocusType.Passive);
            _maxDistanceControlID = GUIUtility.GetControlID(FocusType.Passive);
            _testButtonControlID = GUIUtility.GetControlID(FocusType.Passive);

            // Handle different event types
            var evt = Event.current;

            // Check for mouse up anywhere (including outside scene view)
            if ((evt.type == EventType.MouseUp || evt.rawType == EventType.MouseUp) && _isDragging)
            {
                if (_isTestingMode)
                {
                    RestoreParameterValue();
                }
                GUIUtility.hotControl = 0;
                _isDragging = false;
                evt.Use();
                SceneView.RepaintAll();
                return;
            }

            switch (evt.GetTypeForControl(_centerPointControlID))
            {
                case EventType.MouseDown:
                    if (HandleMouseDown(evt, centerPoint, minPoint, maxPoint))
                        evt.Use();
                    break;

                case EventType.MouseDrag:
                    if (_isDragging)
                    {
                        if (_isTestingMode)
                        {
                            HandleTestingMode(lookParameter);
                            evt.Use();
                        }
                        else
                        {
                            Undo.RecordObject(target, GUIUtility.hotControl == _centerPointControlID ?
                                "Move Center Point" : "Adjust Range");

                            if (HandleMouseDrag(evt, lookParameter, transform, axisDirection))
                                evt.Use();
                        }
                    }
                    break;

                case EventType.Repaint:
                    if (_isTestingMode)
                    {
                        DrawRangeHandles(centerPoint, minPoint, maxPoint, axisDirection, true);
                    }
                    else
                    {
                        DrawHandles(centerPoint, minPoint, maxPoint, axisDirection);
                    }
                    break;
            }
        }

        private Vector3 GetAxisDirection(Transform transform)
        {
            var lookParameter = target as AdvancedLookParameter;
            if (lookParameter == null) return Vector3.right;

            return lookParameter.Axis switch
            {
                CubismLookAxis.X => Vector3.Cross(transform.forward, transform.up).normalized,
                CubismLookAxis.Y => transform.up,
                CubismLookAxis.Z => transform.forward,
                _ => Vector3.zero
            };
        }

        private bool HandleMouseDown(Event evt, Vector3 centerPoint, Vector3 minPoint, Vector3 maxPoint)
        {
            if (evt.button != 0) return false;

            float screenSize = HandleUtility.GetHandleSize(centerPoint);

            // Check test button
            var testButtonPos = centerPoint + Vector3.down * screenSize * TestButtonOffset;
            float testButtonDistance = HandleUtility.DistanceToCircle(testButtonPos, screenSize * TestButtonSize);
            if (testButtonDistance < 10f)
            {
                _isDragging = true;
                _isTestingMode = true;
                GUIUtility.hotControl = _testButtonControlID;
                var lookParameter = target as AdvancedLookParameter;
                if (lookParameter != null)
                {
                    _originalParameterValue = lookParameter.Parameter.Value;
                }
                return true;
            }

            // Check center point
            float centerDistance = HandleUtility.DistanceToCircle(centerPoint, screenSize * HandleSize);
            if (centerDistance < 10f)
            {
                _isDragging = true;
                GUIUtility.hotControl = _centerPointControlID;
                Undo.RecordObject(target, "Move Center Point");
                return true;
            }

            // Check min distance handle
            float minDistance = HandleUtility.DistanceToCircle(minPoint, screenSize * RangeHandleSize);
            if (minDistance < 10f)
            {
                _isDragging = true;
                GUIUtility.hotControl = _minDistanceControlID;
                Undo.RecordObject(target, "Adjust Range");
                return true;
            }

            // Check max distance handle
            float maxDistance = HandleUtility.DistanceToCircle(maxPoint, screenSize * RangeHandleSize);
            if (maxDistance < 10f)
            {
                _isDragging = true;
                GUIUtility.hotControl = _maxDistanceControlID;
                Undo.RecordObject(target, "Adjust Range");
                return true;
            }

            return false;
        }

        private bool HandleMouseDrag(Event evt, AdvancedLookParameter lookParameter, Transform transform, Vector3 axisDirection)
        {
            if (!_isDragging) return false;

            var mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            var modelPlane = new Plane(transform.forward, transform.position);

            if (modelPlane.Raycast(mouseRay, out float distance))
            {
                var worldPoint = mouseRay.GetPoint(distance);

                if (GUIUtility.hotControl == _centerPointControlID)
                {
                    // Handle center point drag
                    var localPoint = transform.InverseTransformPoint(worldPoint);
                    lookParameter.CenterPoint = new Vector2(localPoint.x, localPoint.y);
                    return true;
                }
                else
                {
                    // Handle min/max distance drag
                    var centerPoint = transform.TransformPoint(new Vector3(lookParameter.CenterPoint.x, lookParameter.CenterPoint.y, 0f));
                    var toPoint = worldPoint - centerPoint;
                    var projectedDistance = Vector3.Dot(toPoint, axisDirection);

                    var isMinHandle = GUIUtility.hotControl == _minDistanceControlID;
                    var delta = isMinHandle ?
                        projectedDistance - lookParameter.MinDistance :
                        projectedDistance - lookParameter.MaxDistance;

                    if (evt.control)
                    {
                        // Move handles in opposite directions
                        if (isMinHandle)
                        {
                            var newMin = projectedDistance;
                            var newMax = lookParameter.MaxDistance - delta;
                            if (newMin <= newMax)
                            {
                                lookParameter.MinDistance = newMin;
                                lookParameter.MaxDistance = newMax;
                            }
                        }
                        else
                        {
                            var newMax = projectedDistance;
                            var newMin = lookParameter.MinDistance - delta;
                            if (newMin <= newMax)
                            {
                                lookParameter.MaxDistance = newMax;
                                lookParameter.MinDistance = newMin;
                            }
                        }
                    }
                    else if (evt.shift)
                    {
                        // Move both handles in same direction
                        lookParameter.MinDistance += delta;
                        lookParameter.MaxDistance += delta;
                    }
                    else
                    {
                        // Normal single handle movement
                        if (isMinHandle)
                        {
                            var newMin = projectedDistance;
                            if (newMin <= lookParameter.MaxDistance)
                            {
                                lookParameter.MinDistance = newMin;
                            }
                        }
                        else
                        {
                            var newMax = projectedDistance;
                            if (lookParameter.MinDistance <= newMax)
                            {
                                lookParameter.MaxDistance = newMax;
                            }
                        }
                    }
                    return true;
                }
            }

            return false;
        }

        private void DrawHandles(Vector3 centerPoint, Vector3 minPoint, Vector3 maxPoint, Vector3 axisDirection)
        {
            float screenSize = HandleUtility.GetHandleSize(centerPoint);

            // Draw center point and crosshair
            using (new Handles.DrawingScope(GUIUtility.hotControl == _centerPointControlID ? SelectedColor : HandleColor))
            {
                // Draw 2D crosshair with constant screen size
                Handles.DrawLine(
                    centerPoint + Vector3.left * screenSize * CrosshairSize,
                    centerPoint + Vector3.right * screenSize * CrosshairSize
                );
                Handles.DrawLine(
                    centerPoint + Vector3.up * screenSize * CrosshairSize,
                    centerPoint + Vector3.down * screenSize * CrosshairSize
                );

                // Draw center point
                Handles.DrawWireDisc(centerPoint, Vector3.forward, screenSize * HandleSize);
                Handles.DrawSolidDisc(centerPoint, Vector3.forward, screenSize * HandleSize * 0.5f);
            }

            // Draw test button (Unity style button look)
            var testButtonPos = centerPoint + Vector3.down * screenSize * TestButtonOffset;
            Handles.BeginGUI();

            // Convert world position to screen position
            var screenPos = HandleUtility.WorldToGUIPoint(testButtonPos);

            // Create centered rect around the screen position
            var buttonWidth = 50;
            var buttonHeight = 20;
            var rect = new Rect(
                screenPos.x - buttonWidth * 0.5f,
                screenPos.y - buttonHeight * 0.5f,
                buttonWidth,
                buttonHeight
            );

            // Draw button with Unity style
            if (GUI.Button(rect, "Test", EditorStyles.miniButton))
            {
                _isDragging = true;
                _isTestingMode = true;
                GUIUtility.hotControl = _testButtonControlID;
                var lookParameter = target as AdvancedLookParameter;
                if (lookParameter != null)
                {
                    _originalParameterValue = lookParameter.Parameter.Value;
                }
            }
            Handles.EndGUI();

            DrawRangeHandles(centerPoint, minPoint, maxPoint, axisDirection, false);
        }

        private void DrawRangeHandles(Vector3 centerPoint, Vector3 minPoint, Vector3 maxPoint, Vector3 axisDirection, bool isTestMode)
        {
            float screenSize = HandleUtility.GetHandleSize(centerPoint);

            // Draw axis line
            Handles.color = Color.gray;
            Handles.DrawDottedLine(minPoint, maxPoint, 4f);

            if (isTestMode)
            {
                // Draw simplified semi-circle handles in test mode
                using (new Handles.DrawingScope(RangeColor))
                {
                    // Min handle
                    var right = Vector3.Cross(axisDirection, Vector3.forward).normalized;
                    Handles.DrawWireArc(minPoint, Vector3.forward, right, 180f, screenSize * RangeHandleSize);

                    // Max handle
                    Handles.DrawWireArc(maxPoint, Vector3.forward, -right, 180f, screenSize * RangeHandleSize);
                }
            }
            else
            {
                // Draw min distance handle
                using (new Handles.DrawingScope(GUIUtility.hotControl == _minDistanceControlID ? SelectedColor : RangeColor))
                {
                    // Draw handle circle
                    Handles.DrawWireDisc(minPoint, Vector3.forward, screenSize * RangeHandleSize);
                    Handles.DrawSolidDisc(minPoint, Vector3.forward, screenSize * RangeHandleSize * 0.8f);

                    // Draw direction indicator (arrow pointing inward)
                    var right = Vector3.Cross(axisDirection, Vector3.forward).normalized;
                    var arrowSize = screenSize * RangeArrowSize * 0.8f;
                    var arrowOffset = screenSize * RangeHandleSize * 0.5f;
                    var arrowTip = minPoint + axisDirection * arrowOffset;
                    var arrowBase = arrowTip + axisDirection * arrowSize;

                    Handles.DrawLine(arrowTip, arrowBase, 3f);
                    Handles.DrawLine(arrowTip, arrowBase + right * arrowSize * 0.5f, 3f);
                    Handles.DrawLine(arrowTip, arrowBase - right * arrowSize * 0.5f, 3f);
                }

                // Draw max distance handle
                using (new Handles.DrawingScope(GUIUtility.hotControl == _maxDistanceControlID ? SelectedColor : RangeColor))
                {
                    // Draw handle circle
                    Handles.DrawWireDisc(maxPoint, Vector3.forward, screenSize * RangeHandleSize);
                    Handles.DrawSolidDisc(maxPoint, Vector3.forward, screenSize * RangeHandleSize * 0.8f);

                    // Draw direction indicator (arrow pointing outward)
                    var right = Vector3.Cross(axisDirection, Vector3.forward).normalized;
                    var arrowSize = screenSize * RangeArrowSize * 0.8f;
                    var arrowOffset = screenSize * RangeHandleSize * 0.5f;
                    var arrowTip = maxPoint - axisDirection * arrowOffset;
                    var arrowBase = arrowTip - axisDirection * arrowSize;

                    Handles.DrawLine(arrowTip, arrowBase, 3f);
                    Handles.DrawLine(arrowTip, arrowBase + right * arrowSize * 0.5f, 3f);
                    Handles.DrawLine(arrowTip, arrowBase - right * arrowSize * 0.5f, 3f);
                }
            }
        }

        private void HandleTestingMode(AdvancedLookParameter lookParameter)
        {
            var transform = lookParameter.transform;
            var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var worldPoint = mouseRay.origin;

            var value = lookParameter.Evaluate(
                worldPoint,
                transform.forward,
                transform.up
            );

            lookParameter.Parameter.Value = value;
            lookParameter.Model.ForceUpdateNow();

            // Draw debug visualization
            using (new Handles.DrawingScope(Color.green))
            {
                var centerPoint = transform.TransformPoint(new Vector3(lookParameter.CenterPoint.x, lookParameter.CenterPoint.y, 0f));
                Handles.DrawLine(centerPoint, worldPoint);
                Handles.DrawWireDisc(worldPoint, transform.forward, HandleUtility.GetHandleSize(worldPoint) * 0.1f);
            }
        }

        private void RestoreParameterValue()
        {
            if (!_isTestingMode) return;

            _isTestingMode = false;
            var lookParameter = target as AdvancedLookParameter;
            if (lookParameter != null)
            {
                lookParameter.Parameter.Value = _originalParameterValue;
                lookParameter.Model.ForceUpdateNow();
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                return new Label("Error loading inspector. Check console for details.");
            }

            _root = visualTree.CloneTree();
            ApplyStyles();

            // Add validation for min/max distance fields
            var minDistanceField = _root.Q<PropertyField>("min-distance");
            var maxDistanceField = _root.Q<PropertyField>("max-distance");

            if (minDistanceField != null && maxDistanceField != null)
            {
                minDistanceField.RegisterValueChangeCallback(evt =>
                {
                    var lookParameter = target as AdvancedLookParameter;
                    if (lookParameter != null)
                    {
                        var newValue = evt.changedProperty.floatValue;
                        if (newValue > lookParameter.MaxDistance)
                        {
                            evt.changedProperty.floatValue = lookParameter.MaxDistance;
                            evt.changedProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                });

                maxDistanceField.RegisterValueChangeCallback(evt =>
                {
                    var lookParameter = target as AdvancedLookParameter;
                    if (lookParameter != null)
                    {
                        var newValue = evt.changedProperty.floatValue;
                        if (newValue < lookParameter.MinDistance)
                        {
                            evt.changedProperty.floatValue = lookParameter.MinDistance;
                            evt.changedProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                });
            }

            return _root;
        }

        private void ApplyStyles()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (styleSheet != null)
            {
                _root.styleSheets.Add(styleSheet);
            }
        }
    }
}