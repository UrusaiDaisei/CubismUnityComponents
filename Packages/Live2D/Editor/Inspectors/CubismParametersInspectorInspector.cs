/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Allows inspecting <see cref="CubismParameter"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismParametersInspector))]
    internal sealed class CubismParametersInspectorInspector : UnityEditor.Editor
    {
        #region Fields

        private VisualElement _root;
        private VisualElement _parametersContainer;

        #endregion

        #region Runtime

        private CubismParameter[] Parameters { get; set; }
        private string[] ParametersNameFromJson { get; set; }
        private bool IsInitialized => Parameters != null;

        #endregion

        #region Unity Methods

        public override VisualElement CreateInspectorGUI()
        {
            try
            {
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Packages/com.live2d.cubism/Editor/Inspectors/CubismParametersInspectorInspector.uxml");

                if (visualTree == null)
                {
                    Debug.LogError("Could not load UXML file for CubismParametersInspectorInspector.");
                    return new Label("Error loading inspector. Check console for details.");
                }

                _root = visualTree.CloneTree();

                ApplyStyles();
                ConfigureScrollView();
                InitializeControls();

                return _root;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating inspector GUI: {e}");
                return new Label("Error loading inspector. Check console for details.");
            }
        }

        #endregion

        #region Internal Methods

        private void ApplyStyles()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.live2d.cubism/Editor/Inspectors/CubismParametersInspectorInspector.uss");

            if (styleSheet != null)
            {
                _root.styleSheets.Add(styleSheet);
            }
        }

        private void ConfigureScrollView()
        {
            var scrollView = _root.Q<ScrollView>();
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.style.maxHeight = 400;
        }

        private void InitializeControls()
        {
            _parametersContainer = _root.Q<VisualElement>("parameters-container");
            var resetButton = _root.Q<Button>("reset-button");

            _root.focusable = true;
            _root.pickingMode = PickingMode.Position;

            // Register for mouse enter/leave at root level
            _root.RegisterCallback<MouseEnterEvent>(evt =>
            {
                _root.Focus();

                if (!evt.ctrlKey) return;
                _root.AddToClassList("ctrl-pressed");
            });

            _root.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                _root.RemoveFromClassList("ctrl-pressed");
            });

            // Add Ctrl key handling at container level
            _root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (!evt.ctrlKey) return;
                _root.AddToClassList("ctrl-pressed");
            });

            _root.RegisterCallback<KeyUpEvent>(evt =>
            {
                if (!evt.ctrlKey) return;
                _root.RemoveFromClassList("ctrl-pressed");
            });

            if (!IsInitialized)
            {
                Initialize();
            }

            CreateParameterControls();
            resetButton.clicked += OnResetButtonClicked;
        }

        private void CreateParameterControls()
        {
            for (var i = 0; i < Parameters.Length; i++)
            {
                CreateParameterSlider(i);
            }
        }

        private void CreateParameterSlider(int index)
        {
            var parameter = Parameters[index];
            var name = string.IsNullOrEmpty(ParametersNameFromJson[index])
                ? parameter.Id
                : ParametersNameFromJson[index];

            var slider = new Slider(name, parameter.MinimumValue, parameter.MaximumValue)
            {
                value = parameter.Value,
                style = { marginLeft = 0 }
            };

            ConfigureSliderLabel(slider, index);
            ConfigureSliderCallback(slider, index);

            _parametersContainer.Add(slider);
        }

        private void ConfigureSliderLabel(Slider slider, int index)
        {
            var label = slider.Q<Label>();

            // Add a permanent tooltip
            label.tooltip = "Ctrl+Click to highlight in hierarchy";

            // Add hover style class
            label.AddToClassList("parameter-label");

            label.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (!evt.ctrlKey) return;

                EditorGUIUtility.PingObject(Parameters[index]);
                Selection.activeObject = Parameters[index];
                evt.StopPropagation();
            });
        }

        private void ConfigureSliderCallback(Slider slider, int index)
        {
            slider.RegisterValueChangedCallback(evt =>
            {
                Parameters[index].Value = evt.newValue;
                EditorUtility.SetDirty(Parameters[index]);
                UpdateModel();
            });
        }

        #endregion

        #region Event Handlers

        private void OnResetButtonClicked()
        {
            foreach (var parameter in Parameters)
            {
                parameter.Value = parameter.DefaultValue;
                EditorUtility.SetDirty(parameter);
            }

            UpdateSliderValues();
            UpdateModel();
        }

        #endregion

        #region Auxiliary Methods

        private void UpdateSliderValues()
        {
            var sliders = _parametersContainer.Query<Slider>().ToList();
            for (var i = 0; i < Parameters.Length; i++)
            {
                sliders[i].value = Parameters[i].DefaultValue;
            }
        }

        private void UpdateModel()
        {
            (target as Component)
                .FindCubismModel()
                .ForceUpdateNow();
        }

        private void Initialize()
        {
            Parameters = (target as Component)
                .FindCubismModel(true)
                .Parameters;

            ParametersNameFromJson = new string[Parameters.Length];

            for (var i = 0; i < Parameters.Length; i++)
            {
                var displayInfoParameterName = Parameters[i].GetComponent<CubismDisplayInfoParameterName>();
                if (displayInfoParameterName == null)
                {
                    ParametersNameFromJson[i] = string.Empty;
                    continue;
                }

                ParametersNameFromJson[i] = string.IsNullOrEmpty(displayInfoParameterName.DisplayName)
                    ? displayInfoParameterName.Name
                    : displayInfoParameterName.DisplayName;
            }
        }

        #endregion
    }
}
