using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Live2D.Cubism.Editor.Inspectors
{
    public sealed class CubismParameterXYSlider : VisualElement
    {
        public readonly struct AxisParameter
        {
            public readonly string Label { get; }
            public readonly float MinValue { get; }
            public readonly float MaxValue { get; }
            public readonly float Value { get; }

            public AxisParameter(string label, float minValue, float maxValue, float value)
            {
                Label = label;
                MinValue = minValue;
                MaxValue = maxValue;
                Value = value;
            }
        }

        private readonly Vector2 _minValue;
        private readonly Vector2 _maxValue;
        private Vector2 _value;
        private bool _isDragging;
        private readonly VisualElement _handle;
        private readonly Label _horizontalLabel;
        private readonly Label _verticalLabel;

        public System.Action<Vector2> OnValueChanged;

        public Vector2 Value
        {
            get => _value;
            set
            {
                _value = new Vector2(
                    Mathf.Clamp(value.x, _minValue.x, _maxValue.x),
                    Mathf.Clamp(value.y, _minValue.y, _maxValue.y)
                );
                UpdateHandlePosition();
            }
        }

        public CubismParameterXYSlider(in AxisParameter horizontal, in AxisParameter vertical)
        {
            _minValue = new Vector2(horizontal.MinValue, vertical.MinValue);
            _maxValue = new Vector2(horizontal.MaxValue, vertical.MaxValue);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.live2d.cubism/Editor/Inspectors/CubismParameterXYSlider.uss"));

            // Build visual hierarchy
            var container = new VisualElement();
            container.AddToClassList("xy-slider-root");
            container.AddToClassList("unity-base-field");

            var labelsContainer = new VisualElement();
            labelsContainer.AddToClassList("xy-slider-labels");

            _horizontalLabel = new Label(horizontal.Label);
            _verticalLabel = new Label(vertical.Label);
            labelsContainer.Add(_horizontalLabel);
            labelsContainer.Add(_verticalLabel);

            var sliderArea = new VisualElement();
            sliderArea.AddToClassList("xy-slider-area");

            var sliderContainer = new VisualElement();
            sliderContainer.AddToClassList("xy-slider-container");

            var backgroundContainer = new VisualElement();
            backgroundContainer.AddToClassList("xy-slider-background-container");

            var centerLineH = new VisualElement();
            var centerLineV = new VisualElement();
            centerLineH.AddToClassList("xy-slider-center-line-h");
            centerLineV.AddToClassList("xy-slider-center-line-v");

            backgroundContainer.Add(centerLineH);
            backgroundContainer.Add(centerLineV);
            sliderContainer.Add(backgroundContainer);

            _handle = new VisualElement { style = { width = 10, height = 10 } };
            _handle.AddToClassList("xy-slider-handle");
            sliderContainer.Add(_handle);

            // Register events
            sliderContainer.RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            // Build final hierarchy
            sliderArea.Add(sliderContainer);
            container.Add(labelsContainer);
            container.Add(sliderArea);
            Add(container);

            Value = new Vector2(horizontal.Value, vertical.Value);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (!_isDragging) return;

            var root = EditorWindow.focusedWindow?.rootVisualElement;
            if (root != null)
            {
                root.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                root.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;

            var sliderContainer = _handle.parent;
            var containerRect = sliderContainer.worldBound;
            var localPos = evt.mousePosition - new Vector2(containerRect.x, containerRect.y);

            UpdateValueFromLocalPosition(localPos);
            _isDragging = true;
            evt.StopPropagation();

            var root = EditorWindow.focusedWindow?.rootVisualElement;
            if (root != null)
            {
                root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                root.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!_isDragging) return;

            var sliderContainer = _handle.parent;
            var containerRect = sliderContainer.worldBound;
            var localPos = evt.mousePosition - new Vector2(containerRect.x, containerRect.y);

            UpdateValueFromLocalPosition(localPos);
            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!_isDragging || evt.button != 0) return;

            _isDragging = false;
            evt.StopPropagation();

            var root = EditorWindow.focusedWindow?.rootVisualElement;
            if (root != null)
            {
                root.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                root.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }
        }

        private void UpdateValueFromLocalPosition(Vector2 localPos)
        {
            var containerRect = _handle.parent.layout;
            var normalizedX = Mathf.Clamp01(localPos.x / containerRect.width);
            var normalizedY = 1 - Mathf.Clamp01(localPos.y / containerRect.height);

            var newValue = new Vector2(
                Mathf.Lerp(_minValue.x, _maxValue.x, normalizedX),
                Mathf.Lerp(_minValue.y, _maxValue.y, normalizedY)
            );

            if (newValue == Value) return;

            Value = newValue;
            OnValueChanged?.Invoke(Value);
        }

        private void UpdateHandlePosition()
        {
            var containerRect = _handle.parent.layout;
            var handleWidth = _handle.resolvedStyle.width;
            var handleHeight = _handle.resolvedStyle.height;

            var normalizedX = Mathf.InverseLerp(_minValue.x, _maxValue.x, _value.x);
            var normalizedY = Mathf.InverseLerp(_minValue.y, _maxValue.y, _value.y);

            var x = (normalizedX * containerRect.width) - (handleWidth * 0.5f);
            var y = ((1 - normalizedY) * containerRect.height) - (handleHeight * 0.5f);

            x = Mathf.Clamp(x, 0, containerRect.width - handleWidth);
            y = Mathf.Clamp(y, 0, containerRect.height - handleHeight);

            _handle.style.left = x;
            _handle.style.top = y;
        }
    }
}