using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Live2D.Cubism.Editor.Inspectors
{
    public sealed class CubismParameterXYSlider : VisualElement
    {
        #region Types

        public readonly ref struct AxisParameter
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

        #endregion

        #region Fields

        private readonly Vector2 _minValue;
        private readonly Vector2 _maxValue;
        private Vector2 _value;
        private Vector2 _pendingValue;
        private bool _hasPendingValue;
        private bool _isDragging;

        private readonly VisualElement _handle;
        private readonly Label _horizontalLabel;
        private readonly Label _verticalLabel;

        #endregion

        #region Properties

        public System.Action<Vector2> OnValueChanged;

        public Vector2 Value
        {
            get => _value;
            set
            {
                _value = ClampValue(value);
                TryUpdateVisuals();
            }
        }

        #endregion

        #region Constructor

        public CubismParameterXYSlider(in AxisParameter horizontal, in AxisParameter vertical)
        {
            _minValue = new Vector2(horizontal.MinValue, vertical.MinValue);
            _maxValue = new Vector2(horizontal.MaxValue, vertical.MaxValue);

            // Initialize readonly fields
            _handle = new VisualElement { style = { width = 10, height = 10 } };
            _handle.AddToClassList("xy-slider-handle");

            _horizontalLabel = new Label(horizontal.Label);
            _verticalLabel = new Label(vertical.Label);

            // Build the UI
            InitializeVisuals();
            RegisterCallbacks();
            Value = new Vector2(horizontal.Value, vertical.Value);
        }

        #endregion

        #region Setup

        private void InitializeVisuals()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.live2d.cubism/Editor/Inspectors/CubismParameterXYSlider.uss"));

            var container = CreateContainer();
            var labelsContainer = CreateLabelsContainer();
            var sliderArea = CreateSliderArea();

            container.Add(labelsContainer);
            container.Add(sliderArea);
            Add(container);
        }

        private VisualElement CreateContainer()
        {
            var container = new VisualElement();
            container.AddToClassList("xy-slider-root");
            container.AddToClassList("unity-base-field");
            return container;
        }

        private VisualElement CreateLabelsContainer()
        {
            var container = new VisualElement();
            container.AddToClassList("xy-slider-labels");

            container.Add(_horizontalLabel);
            container.Add(_verticalLabel);

            return container;
        }

        private VisualElement CreateSliderArea()
        {
            var sliderArea = new VisualElement();
            sliderArea.AddToClassList("xy-slider-area");

            var sliderContainer = new VisualElement();
            sliderContainer.AddToClassList("xy-slider-container");

            var backgroundContainer = CreateBackgroundContainer();

            sliderContainer.Add(backgroundContainer);
            sliderContainer.Add(_handle);
            sliderArea.Add(sliderContainer);

            // Register mouse events on the slider container
            sliderContainer.RegisterCallback<MouseDownEvent>(OnMouseDown);

            return sliderArea;
        }

        private VisualElement CreateBackgroundContainer()
        {
            var container = new VisualElement();
            container.AddToClassList("xy-slider-background-container");

            var centerLineH = new VisualElement();
            var centerLineV = new VisualElement();
            centerLineH.AddToClassList("xy-slider-center-line-h");
            centerLineV.AddToClassList("xy-slider-center-line-v");

            container.Add(centerLineH);
            container.Add(centerLineV);

            return container;
        }

        private void RegisterCallbacks()
        {
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        #endregion

        #region Event Handlers

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (!_isDragging) return;
            UnregisterRootCallbacks();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;

            UpdateValueFromMousePosition(evt.mousePosition);
            _isDragging = true;
            evt.StopPropagation();

            RegisterRootCallbacks();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!_isDragging) return;
            UpdateValueFromMousePosition(evt.mousePosition);
            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!_isDragging || evt.button != 0) return;

            _isDragging = false;
            evt.StopPropagation();
            UnregisterRootCallbacks();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_hasPendingValue && _handle?.parent?.layout.width > 0)
            {
                _value = _pendingValue;
                UpdateHandlePosition();
                _hasPendingValue = false;
            }
        }

        #endregion

        #region Update Methods

        private void UpdateValueFromMousePosition(Vector2 mousePosition)
        {
            var sliderContainer = _handle.parent;
            var containerRect = sliderContainer.worldBound;
            var localPos = mousePosition - new Vector2(containerRect.x, containerRect.y);

            var containerLayout = _handle.parent.layout;
            var normalizedX = Mathf.Clamp01(localPos.x / containerLayout.width);
            var normalizedY = 1 - Mathf.Clamp01(localPos.y / containerLayout.height);

            var newValue = new Vector2(
                Mathf.Lerp(_minValue.x, _maxValue.x, normalizedX),
                Mathf.Lerp(_minValue.y, _maxValue.y, normalizedY)
            );

            if (newValue != Value)
            {
                Value = newValue;
                OnValueChanged?.Invoke(Value);
            }
        }

        private void TryUpdateVisuals()
        {
            if (_handle?.parent?.layout.width > 0)
            {
                UpdateHandlePosition();
            }
            else
            {
                _pendingValue = _value;
                _hasPendingValue = true;
            }
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

        #endregion

        #region Utility Methods

        private Vector2 ClampValue(Vector2 value) => new Vector2(
            Mathf.Clamp(value.x, _minValue.x, _maxValue.x),
            Mathf.Clamp(value.y, _minValue.y, _maxValue.y)
        );

        private void RegisterRootCallbacks()
        {
            var root = EditorWindow.focusedWindow?.rootVisualElement;
            if (root != null)
            {
                root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                root.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }
        }

        private void UnregisterRootCallbacks()
        {
            var root = EditorWindow.focusedWindow?.rootVisualElement;
            if (root != null)
            {
                root.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                root.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }
        }

        #endregion
    }
}