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
using System.Linq;
using System.Collections.Generic;

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
        private bool IsInitialized => Parameters != null;

        // Change the field declaration
        private List<(CubismParameterXYSlider slider, CubismParameter horizontal, CubismParameter vertical)> _xySliderParameters;

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

            // Handle Ctrl state globally
            void UpdateCtrlState(bool isCtrlPressed)
            {
                if (isCtrlPressed)
                    _root.AddToClassList("ctrl-pressed");
                else
                    _root.RemoveFromClassList("ctrl-pressed");
            }

            // Register for mouse enter/leave at root level
            _root.RegisterCallback<MouseEnterEvent>(evt =>
            {
                _root.Focus();
                UpdateCtrlState(evt.ctrlKey);
            });

            _root.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                UpdateCtrlState(false);
            });

            // Add Ctrl key handling at root level
            _root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.LeftControl || evt.keyCode == KeyCode.RightControl)
                    UpdateCtrlState(true);
            });

            _root.RegisterCallback<KeyUpEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.LeftControl || evt.keyCode == KeyCode.RightControl)
                    UpdateCtrlState(false);
            });

            // Also handle focus loss
            _root.RegisterCallback<FocusOutEvent>(evt =>
            {
                UpdateCtrlState(false);
            });

            if (!IsInitialized)
            {
                Initialize();
            }

            _xySliderParameters = new List<(CubismParameterXYSlider slider, CubismParameter horizontal, CubismParameter vertical)>();

            CreateParameterControls();
            resetButton.clicked += OnResetButtonClicked;
        }

        private void CreateParameterControls()
        {
            var groupsComponent = (target as Component).GetComponentInChildren<CubismParameterGroups>();
            var combinedInfo = (target as Component).GetComponentInChildren<CubismDisplayInfoCombinedParameterInfo>();
            _parametersContainer.Clear();

            if (groupsComponent?.Groups == null)
            {
                CreateFlatParameterList(combinedInfo);
                return;
            }

            CreateGroupedParameters(groupsComponent, combinedInfo);
        }

        private void CreateFlatParameterList(CubismDisplayInfoCombinedParameterInfo combinedInfo)
        {
            var processedParams = new HashSet<int>();

            // Handle combined parameters first
            if (combinedInfo?.CombinedParameters != null)
            {
                foreach (var combined in combinedInfo.CombinedParameters)
                {
                    var horizontalIndex = System.Array.FindIndex(Parameters, p => p.Id == combined.HorizontalParameterId);
                    var verticalIndex = System.Array.FindIndex(Parameters, p => p.Id == combined.VerticalParameterId);

                    if (horizontalIndex < 0 || verticalIndex < 0)
                    {
                        Debug.LogError($"Combined parameter not found: {combined.HorizontalParameterId} or {combined.VerticalParameterId}");
                        continue;
                    }

                    var combinedContainer = new VisualElement();
                    combinedContainer.AddToClassList("combined-parameter");

                    CreateParameterSlider(horizontalIndex, Parameters[horizontalIndex], combinedContainer);
                    CreateParameterSlider(verticalIndex, Parameters[verticalIndex], combinedContainer);

                    _parametersContainer.Add(combinedContainer);
                    processedParams.Add(horizontalIndex);
                    processedParams.Add(verticalIndex);
                }
            }

            // Handle remaining parameters
            for (int i = 0; i < Parameters.Length; i++)
            {
                if (!processedParams.Contains(i))
                {
                    CreateParameterSlider(i, Parameters[i], _parametersContainer);
                }
            }
        }

        private void CreateGroupedParameters(CubismParameterGroups groupsComponent, CubismDisplayInfoCombinedParameterInfo combinedInfo)
        {
            var modelId = (target as Component).FindCubismModel().name;
            var processedParams = new HashSet<string>();

            // Track combined parameters
            if (combinedInfo?.CombinedParameters != null)
            {
                foreach (var combined in combinedInfo.CombinedParameters)
                {
                    processedParams.Add(combined.HorizontalParameterId);
                    processedParams.Add(combined.VerticalParameterId);
                }
            }

            foreach (var group in groupsComponent.Groups)
            {
                var foldoutKey = $"{modelId}_{group.Name}";
                var foldout = new Foldout
                {
                    text = group.Name,
                    value = SessionState.GetBool(foldoutKey, false)
                };
                foldout.RegisterValueChangedCallback(evt => SessionState.SetBool(foldoutKey, evt.newValue));
                foldout.AddToClassList("parameter-group");
                _parametersContainer.Add(foldout);

                // Handle regular parameters first
                foreach (var parameter in group.Parameters.Where(p => !processedParams.Contains(p.Name)))
                {
                    var index = System.Array.FindIndex(Parameters, p => p.Id == parameter.Name);
                    if (index < 0) continue;

                    CreateParameterSlider(index, Parameters[index], foldout);
                }

                // Handle combined parameters
                if (combinedInfo?.CombinedParameters != null)
                {
                    foreach (var combined in combinedInfo.CombinedParameters)
                    {
                        var horizontalParamInfo = group.Parameters.FirstOrDefault(p => p.Name == combined.HorizontalParameterId);
                        var verticalParamInfo = group.Parameters.FirstOrDefault(p => p.Name == combined.VerticalParameterId);

                        if (horizontalParamInfo == null || verticalParamInfo == null) continue;

                        var horizontalIndex = System.Array.FindIndex(Parameters, p => p.Id == horizontalParamInfo.Name);
                        var verticalIndex = System.Array.FindIndex(Parameters, p => p.Id == verticalParamInfo.Name);

                        if (horizontalIndex < 0 || verticalIndex < 0)
                        {
                            Debug.LogError($"Combined parameter not found: {combined.HorizontalParameterId} or {combined.VerticalParameterId}");
                            continue;
                        }

                        var combinedContainer = new VisualElement();
                        combinedContainer.AddToClassList("combined-parameter");

                        CreateCombinedParameterControl(
                            Parameters[horizontalIndex],
                            Parameters[verticalIndex],
                            combinedContainer);

                        foldout.Add(combinedContainer);
                    }
                }
            }
        }

        private void CreateParameterSlider(int index, CubismParameter parameter, VisualElement container)
        {
            var displayInfo = parameter.GetComponent<CubismDisplayInfoParameterName>();

            var name = displayInfo != null && !string.IsNullOrEmpty(displayInfo.DisplayName)
                ? displayInfo.DisplayName
                : parameter.Id;

            var slider = new Slider(name, parameter.MinimumValue, parameter.MaximumValue)
            {
                value = parameter.Value
            };

            slider.AddToClassList("parameter-slider");
            ConfigureSliderLabel(slider, index, parameter);
            ConfigureSliderCallback(slider, parameter);

            container.Add(slider);
        }

        private void ConfigureSliderLabel(Slider slider, int index, CubismParameter parameter)
        {
            var label = slider.Q<Label>();
            ConfigureParameterLabel(label, parameter);
        }

        private void ConfigureSliderCallback(Slider slider, CubismParameter parameter)
        {
            slider.RegisterValueChangedCallback(evt =>
            {
                parameter.Value = evt.newValue;
                EditorUtility.SetDirty(parameter);
                UpdateModel();
            });
        }

        private void ConfigureParameterLabel(Label label, CubismParameter parameter)
        {
            label.tooltip = "Ctrl+Click to highlight in hierarchy";
            label.AddToClassList("parameter-label");

            label.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (!evt.ctrlKey) return;

                EditorGUIUtility.PingObject(parameter);
                Selection.activeObject = parameter;
                evt.StopPropagation();
            });
        }

        private void CreateCombinedParameterControl(CubismParameter horizontalParam, CubismParameter verticalParam, VisualElement container)
        {
            var horizontalInfo = horizontalParam.GetComponent<CubismDisplayInfoParameterName>();
            var verticalInfo = verticalParam.GetComponent<CubismDisplayInfoParameterName>();

            var horizontalName = horizontalInfo != null && !string.IsNullOrEmpty(horizontalInfo.DisplayName)
                ? horizontalInfo.DisplayName
                : horizontalParam.Id;

            var verticalName = verticalInfo != null && !string.IsNullOrEmpty(verticalInfo.DisplayName)
                ? verticalInfo.DisplayName
                : verticalParam.Id;

            var horizontal = new CubismParameterXYSlider.AxisParameter(
                horizontalName,
                horizontalParam.MinimumValue,
                horizontalParam.MaximumValue,
                horizontalParam.Value
            );

            var vertical = new CubismParameterXYSlider.AxisParameter(
                verticalName,
                verticalParam.MinimumValue,
                verticalParam.MaximumValue,
                verticalParam.Value
            );

            var xySlider = new CubismParameterXYSlider(horizontal, vertical);

            // Ensure the visual state matches the current parameter values
            xySlider.Value = new Vector2(horizontalParam.Value, verticalParam.Value);

            // Store the parameter association
            _xySliderParameters.Add((xySlider, horizontalParam, verticalParam));

            // Configure labels with parameter behavior
            var labels = xySlider.Query<Label>().ToList();
            ConfigureParameterLabel(labels[0], horizontalParam);
            ConfigureParameterLabel(labels[1], verticalParam);

            xySlider.OnValueChanged += value =>
            {
                horizontalParam.Value = value.x;
                verticalParam.Value = value.y;
                EditorUtility.SetDirty(horizontalParam);
                EditorUtility.SetDirty(verticalParam);
                UpdateModel();
            };

            container.Add(xySlider);
        }

        #endregion

        #region Event Handlers

        private void OnResetButtonClicked()
        {
            if (Parameters == null)
            {
                return;
            }

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
            if (Parameters == null)
            {
                return;
            }

            // Update regular sliders
            var sliders = _parametersContainer.Query<Slider>().ToList();
            for (var i = 0; i < sliders.Count; i++)
            {
                if (i < Parameters.Length)
                {
                    sliders[i].value = Parameters[i].Value;
                }
            }

            // Update XY sliders using stored associations
            foreach (var (slider, horizontalParam, verticalParam) in _xySliderParameters)
            {
                slider.Value = new Vector2(horizontalParam.Value, verticalParam.Value);
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
        }

        #endregion
    }
}

