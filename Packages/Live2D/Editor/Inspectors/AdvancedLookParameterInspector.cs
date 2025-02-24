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
        private readonly Color HandleColor = new Color(0f, 1f, 1f, 0.8f);
        private readonly Color SelectedColor = new Color(1f, 1f, 0f, 0.8f);
        private readonly Color RangeColor = new Color(0f, 1f, 0.5f, 0.8f);

        // Control IDs for handle interaction
        private int _centerPointControlID;
        private int _minDistanceControlID;
        private int _maxDistanceControlID;
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

            if (_isTestingMode)
            {
                HandleTestingMode(lookParameter);
                DrawRangeHandles(centerPoint, minPoint, maxPoint, axisDirection, true);
                SceneView.RepaintAll();
                return;
            }

            // Get control IDs
            _centerPointControlID = GUIUtility.GetControlID(FocusType.Passive);
            _minDistanceControlID = GUIUtility.GetControlID(FocusType.Passive);
            _maxDistanceControlID = GUIUtility.GetControlID(FocusType.Passive);

            // Handle different event types
            var evt = Event.current;
            switch (evt.GetTypeForControl(_centerPointControlID))
            {
                case EventType.MouseDown:
                    if (HandleMouseDown(evt, centerPoint, minPoint, maxPoint))
                        evt.Use();
                    break;

                case EventType.MouseDrag:
                    if (_isDragging)
                    {
                        Undo.RecordObject(target, GUIUtility.hotControl == _centerPointControlID ?
                            "Move Center Point" : "Adjust Range");

                        if (HandleMouseDrag(evt, lookParameter, transform, axisDirection))
                            evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isDragging)
                    {
                        GUIUtility.hotControl = 0;
                        _isDragging = false;
                        evt.Use();
                        SceneView.RepaintAll();
                    }
                    break;

                case EventType.Repaint:
                    DrawHandles(centerPoint, minPoint, maxPoint, axisDirection);
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
                            lookParameter.MinDistance = projectedDistance;
                            lookParameter.MaxDistance -= delta;
                        }
                        else
                        {
                            lookParameter.MaxDistance = projectedDistance;
                            lookParameter.MinDistance -= delta;
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
                            lookParameter.MinDistance = projectedDistance;
                        else
                            lookParameter.MaxDistance = projectedDistance;
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

            DrawRangeHandles(centerPoint, minPoint, maxPoint, axisDirection, false);
        }

        private void DrawRangeHandles(Vector3 centerPoint, Vector3 minPoint, Vector3 maxPoint, Vector3 axisDirection, bool isTestMode)
        {
            float screenSize = HandleUtility.GetHandleSize(centerPoint);

            // Draw axis line
            Handles.color = Color.gray;
            Handles.DrawDottedLine(minPoint, maxPoint, 4f);

            // Draw min distance handle
            using (new Handles.DrawingScope(isTestMode ? RangeColor :
                (GUIUtility.hotControl == _minDistanceControlID ? SelectedColor : RangeColor)))
            {
                // Draw arrow pointing inward
                var right = Vector3.Cross(axisDirection, Vector3.forward).normalized;
                var arrowSize = screenSize * RangeArrowSize;
                var arrowTip = minPoint;
                var arrowBase = minPoint - axisDirection * arrowSize;

                Handles.DrawLine(arrowTip, arrowBase);
                Handles.DrawLine(arrowTip, arrowBase + right * arrowSize * 0.5f);
                Handles.DrawLine(arrowTip, arrowBase - right * arrowSize * 0.5f);
            }

            // Draw max distance handle
            using (new Handles.DrawingScope(isTestMode ? RangeColor :
                (GUIUtility.hotControl == _maxDistanceControlID ? SelectedColor : RangeColor)))
            {
                // Draw arrow pointing outward
                var right = Vector3.Cross(axisDirection, Vector3.forward).normalized;
                var arrowSize = screenSize * RangeArrowSize;
                var arrowTip = maxPoint;
                var arrowBase = maxPoint + axisDirection * arrowSize;

                Handles.DrawLine(arrowTip, arrowBase);
                Handles.DrawLine(arrowTip, arrowBase + right * arrowSize * 0.5f);
                Handles.DrawLine(arrowTip, arrowBase - right * arrowSize * 0.5f);
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

            // Add test mode toggle handling
            var testModeToggle = _root.Q<Toggle>("test-mode-toggle");
            if (testModeToggle != null)
            {
                testModeToggle.value = _isTestingMode;
                testModeToggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue != _isTestingMode)
                    {
                        if (evt.newValue)
                        {
                            var lookParameter = target as AdvancedLookParameter;
                            if (lookParameter != null)
                            {
                                _originalParameterValue = lookParameter.Parameter.Value;
                            }
                        }
                        else
                        {
                            RestoreParameterValue();
                        }
                        _isTestingMode = evt.newValue;
                        SceneView.RepaintAll();
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