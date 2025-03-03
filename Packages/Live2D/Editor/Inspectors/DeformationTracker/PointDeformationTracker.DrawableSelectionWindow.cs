using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using System.Linq;

namespace Live2D.Cubism.Editor.Inspectors
{
    public sealed partial class PointDeformationTrackerEditor
    {
        private sealed class DrawableSelectionWindow : EditorWindow
        {
            #region UI Constants

            private const string UxmlPath = BasePath + "PointDeformationTrackerDrawableSelectionWindow.uxml";
            private const string StylePath = BasePath + "PointDeformationTrackerDrawableSelectionWindow.uss";

            #endregion

            #region Runtime

            private PointDeformationTracker _tracker;
            private IList<CubismDrawable> _allDrawables;
            private readonly List<CubismDrawable> _availableDrawables = new List<CubismDrawable>();
            private readonly List<CubismDrawable> _includedDrawables = new List<CubismDrawable>();
            private readonly List<CubismDrawable> _selectedAvailableDrawables = new List<CubismDrawable>();
            private readonly List<CubismDrawable> _selectedIncludedDrawables = new List<CubismDrawable>();
            private VisualElement _root;
            private TextField _availableSearchField;
            private TextField _includedSearchField;
            private VisualElement _availableListContainer;
            private VisualElement _includedListContainer;

            #endregion

            #region Public Methods

            /// <summary>
            /// Shows the drawable selection window.
            /// </summary>
            /// <param name="tracker">The PointDeformationTracker to update</param>
            /// <param name="allDrawables">List of all available drawables in the model</param>
            public static void ShowWindow(PointDeformationTracker tracker, IList<CubismDrawable> allDrawables)
            {
                var window = GetWindow<DrawableSelectionWindow>("Include Drawables");
                window.minSize = new Vector2(600, 400);
                window._tracker = tracker;
                window._allDrawables = allDrawables;
                window.Initialize();
                window.Show();
            }

            #endregion

            #region Unity Methods

            private void CreateGUI()
            {
                LoadUIAssets();
                BindUIElements();
                RegisterCallbacks();
            }

            private void OnDisable()
            {
                UnregisterCallbacks();
            }

            #endregion

            #region UI Logic

            /// <summary>
            /// Loads the UXML and USS assets for the window.
            /// </summary>
            private void LoadUIAssets()
            {
                // Load UXML
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
                if (visualTree == null)
                {
                    throw new System.Exception($"Could not load UXML file at path: {UxmlPath}");
                }
                _root = visualTree.Instantiate();
                rootVisualElement.Add(_root);

                // Load and apply USS
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
                if (styleSheet == null)
                {
                    throw new System.Exception($"Could not load USS file at path: {StylePath}");
                }
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            /// <summary>
            /// Binds UI elements and sets up button events.
            /// </summary>
            private void BindUIElements()
            {
                _availableSearchField = _root.Q<TextField>("available-search-field");
                _includedSearchField = _root.Q<TextField>("included-search-field");
                _availableListContainer = _root.Q<VisualElement>("available-list-container");
                _includedListContainer = _root.Q<VisualElement>("included-list-container");

                // Bind navigation buttons
                _root.Q<Button>("move-right-button").clicked += MoveSelectedToAvailable;
                _root.Q<Button>("move-all-right-button").clicked += MoveAllToAvailable;
                _root.Q<Button>("move-left-button").clicked += MoveSelectedToIncluded;
                _root.Q<Button>("move-all-left-button").clicked += MoveAllToIncluded;

                // Bind action buttons
                _root.Q<Button>("apply-button").clicked += SaveInclusions;
                _root.Q<Button>("cancel-button").clicked += Close;
            }

            /// <summary>
            /// Registers event callbacks for search fields.
            /// </summary>
            private void RegisterCallbacks()
            {
                _availableSearchField.RegisterValueChangedCallback(evt => PopulateAvailableList());
                _includedSearchField.RegisterValueChangedCallback(evt => PopulateIncludedList());
            }

            /// <summary>
            /// Unregisters event callbacks.
            /// </summary>
            private void UnregisterCallbacks()
            {
                if (_availableSearchField != null)
                {
                    _availableSearchField.UnregisterValueChangedCallback(evt => PopulateAvailableList());
                }

                if (_includedSearchField != null)
                {
                    _includedSearchField.UnregisterValueChangedCallback(evt => PopulateIncludedList());
                }
            }

            #endregion

            #region Auxiliary Code

            /// <summary>
            /// Initializes the window and populates drawable lists.
            /// </summary>
            private void Initialize()
            {
                InitializeDrawableLists();
                PopulateDrawableLists();
            }

            /// <summary>
            /// Initializes the available and included drawable lists.
            /// </summary>
            private void InitializeDrawableLists()
            {
                _includedDrawables.Clear();
                _availableDrawables.Clear();
                _selectedAvailableDrawables.Clear();
                _selectedIncludedDrawables.Clear();

                // Add existing included drawables
                if (_tracker.includedDrawables != null)
                {
                    foreach (var drawable in _tracker.includedDrawables)
                    {
                        if (drawable != null)
                        {
                            _includedDrawables.Add(drawable);
                        }
                    }
                }

                // Add all drawables not already included to available list
                foreach (var drawable in _allDrawables)
                {
                    if (!_includedDrawables.Contains(drawable))
                    {
                        _availableDrawables.Add(drawable);
                    }
                }
            }

            /// <summary>
            /// Populates both drawable lists in the UI.
            /// </summary>
            private void PopulateDrawableLists()
            {
                PopulateAvailableList();
                PopulateIncludedList();
            }

            /// <summary>
            /// Populates the available drawables list based on search criteria.
            /// </summary>
            private void PopulateAvailableList()
            {
                if (_availableListContainer == null) return;

                // Clear existing items
                _availableListContainer.Clear();

                var searchQuery = _availableSearchField?.value?.ToLower() ?? string.Empty;

                // Add each drawable that matches the search
                foreach (var drawable in _availableDrawables)
                {
                    // Skip if filtered out by search
                    if (!string.IsNullOrEmpty(searchQuery) &&
                        !drawable.name.ToLower().Contains(searchQuery))
                    {
                        continue;
                    }

                    CreateDrawableItem(_availableListContainer, drawable, _selectedAvailableDrawables);
                }
            }

            /// <summary>
            /// Populates the included drawables list based on search criteria.
            /// </summary>
            private void PopulateIncludedList()
            {
                if (_includedListContainer == null) return;

                // Clear existing items
                _includedListContainer.Clear();

                var searchQuery = _includedSearchField?.value?.ToLower() ?? string.Empty;

                // Add each drawable that matches the search
                foreach (var drawable in _includedDrawables)
                {
                    // Skip if filtered out by search
                    if (!string.IsNullOrEmpty(searchQuery) &&
                        !drawable.name.ToLower().Contains(searchQuery))
                    {
                        continue;
                    }

                    CreateDrawableItem(_includedListContainer, drawable, _selectedIncludedDrawables);
                }
            }

            /// <summary>
            /// Creates a UI item for a drawable and adds it to the specified container.
            /// </summary>
            private void CreateDrawableItem(VisualElement container, CubismDrawable drawable, List<CubismDrawable> selectionList)
            {
                var item = new Label(drawable.name);
                item.AddToClassList("drawable-item");

                // Set selected state if in selection list
                if (selectionList.Contains(drawable))
                {
                    item.AddToClassList("selected");
                }

                // Add click handler for selection and highlighting in the editor
                item.RegisterCallback<ClickEvent>(evt =>
                {
                    var isMultiSelect = (evt.ctrlKey || evt.commandKey || evt.shiftKey);
                    ToggleItemSelection(item, drawable, selectionList, isMultiSelect);

                    // Highlight the drawable in the editor
                    HighlightDrawableInEditor(drawable);
                });

                container.Add(item);
            }

            /// <summary>
            /// Highlights a drawable in the Unity Editor.
            /// </summary>
            private void HighlightDrawableInEditor(CubismDrawable drawable)
            {
                if (drawable != null)
                {
                    EditorGUIUtility.PingObject(drawable.gameObject);
                }
            }

            /// <summary>
            /// Toggles selection state of a drawable item.
            /// </summary>
            private void ToggleItemSelection(Label item, CubismDrawable drawable, List<CubismDrawable> selectionList, bool isMultiSelect)
            {
                var isSelected = selectionList.Contains(drawable);

                // If not multi-select, clear all existing selections
                if (!isMultiSelect)
                {
                    ClearAllSelections(selectionList == _selectedAvailableDrawables);
                }

                // Toggle selection
                if (isSelected)
                {
                    selectionList.Remove(drawable);
                    item.RemoveFromClassList("selected");
                }
                else
                {
                    selectionList.Add(drawable);
                    item.AddToClassList("selected");
                }
            }

            /// <summary>
            /// Clears all selections in either the available or included list.
            /// </summary>
            private void ClearAllSelections(bool isAvailableList)
            {
                var selectionList = isAvailableList ? _selectedAvailableDrawables : _selectedIncludedDrawables;
                var container = isAvailableList ? _availableListContainer : _includedListContainer;

                selectionList.Clear();

                // Remove selection styling from all items
                foreach (var child in container.Children())
                {
                    child.RemoveFromClassList("selected");
                }
            }

            /// <summary>
            /// Moves selected drawables from available to included list.
            /// </summary>
            private void MoveSelectedToIncluded()
            {
                if (_selectedAvailableDrawables.Count == 0) return;

                foreach (var drawable in _selectedAvailableDrawables.ToList())
                {
                    _availableDrawables.Remove(drawable);
                    _includedDrawables.Add(drawable);
                }

                _selectedAvailableDrawables.Clear();
                _selectedIncludedDrawables.Clear();

                PopulateDrawableLists();
            }

            /// <summary>
            /// Moves selected drawables from included to available list.
            /// </summary>
            private void MoveSelectedToAvailable()
            {
                if (_selectedIncludedDrawables.Count == 0) return;

                foreach (var drawable in _selectedIncludedDrawables.ToList())
                {
                    _includedDrawables.Remove(drawable);
                    _availableDrawables.Add(drawable);
                }

                _selectedAvailableDrawables.Clear();
                _selectedIncludedDrawables.Clear();

                PopulateDrawableLists();
            }

            /// <summary>
            /// Moves all drawables (filtered by search) from available to included list.
            /// </summary>
            private void MoveAllToIncluded()
            {
                // Apply search filter if any
                var searchQuery = _availableSearchField?.value?.ToLower() ?? string.Empty;
                var drawablesToMove = _availableDrawables.ToList();

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    drawablesToMove = _availableDrawables
                        .Where(d => d.name.ToLower().Contains(searchQuery))
                        .ToList();
                }

                foreach (var drawable in drawablesToMove)
                {
                    _includedDrawables.Add(drawable);
                }

                _availableDrawables.RemoveAll(d => drawablesToMove.Contains(d));

                _selectedAvailableDrawables.Clear();
                _selectedIncludedDrawables.Clear();

                PopulateDrawableLists();
            }

            /// <summary>
            /// Moves all drawables (filtered by search) from included to available list.
            /// </summary>
            private void MoveAllToAvailable()
            {
                // Apply search filter if any
                var searchQuery = _includedSearchField?.value?.ToLower() ?? string.Empty;
                var drawablesToMove = _includedDrawables.ToList();

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    drawablesToMove = _includedDrawables
                        .Where(d => d.name.ToLower().Contains(searchQuery))
                        .ToList();
                }

                foreach (var drawable in drawablesToMove)
                {
                    _availableDrawables.Add(drawable);
                }

                _includedDrawables.RemoveAll(d => drawablesToMove.Contains(d));

                _selectedAvailableDrawables.Clear();
                _selectedIncludedDrawables.Clear();

                PopulateDrawableLists();
            }

            /// <summary>
            /// Saves the included drawables to the tracker and closes the window.
            /// </summary>
            private void SaveInclusions()
            {
                Undo.RecordObject(_tracker, "Update Included Drawables");

                // Set the included drawables
                _tracker.includedDrawables = _includedDrawables.ToArray();

                // Recalculate all tracked points
                RecalculateTrackedPoints(_tracker);


                EditorUtility.SetDirty(_tracker);
                Close();
            }

            #endregion
        }
    }
}