using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Live2D.Cubism.Core;
using System.Linq;

// Reference to the partial file containing IncludeDrawablesWindow
// Assets/Scripts/Editor/Live2DDeformationTrackerIncludeDrawablesWindow.cs

[CustomEditor(typeof(Live2DDeformationTracker))]
public sealed partial class Live2DDeformationTrackerEditor : Editor
{
    #region UI Constants

    private static readonly Color k_LabelBackgroundColor = new Color(0, 0, 0, 0.7f);
    private static GUIStyle k_LabelStyle = null;
    private static GUIStyle LabelStyle
    {
        get
        {
            if (k_LabelStyle == null)
                k_LabelStyle = CreateLabelStyle();

            return k_LabelStyle;
        }
    }

    // Default radius value
    private const float DEFAULT_RADIUS = 0.1f;

    #endregion

    #region Runtime State

    private VisualElement _root;
    private Button _editButton;
    private bool _isEditing;
    private bool _isDeleteMode;
    private int _selectedPointIndex = -1;
    private bool _isDragging;
    private Vector3 _dragStartPosition;
    private Vector3 _draggingPoint;
    private bool _isAxisConstrained;
    private Vector3 _constraintOrigin;
    private bool _isAxisConstraintKeyPressed;
    private bool _isGuidelineKeyPressed;
    private bool _showVertexConnections = false;

    private bool IsExpanded => InternalEditorUtility.GetIsInspectorExpanded(target);
    private Live2DDeformationTracker Tracker => target as Live2DDeformationTracker;

    private CubismDrawable[] _previousDrawables = new CubismDrawable[0];

    #endregion

    #region Unity Methods

    public override VisualElement CreateInspectorGUI()
    {
        _root = new VisualElement();

        AddStyleSheet();
        CreateInspectorElements();

        return _root;

        void AddStyleSheet()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Live2DDeformationTrackerEditor.uss");
            if (styleSheet != null)
                _root.styleSheets.Add(styleSheet);
        }

        void CreateInspectorElements()
        {
            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "trackedPoints")
                {
                    CreateEditModeButton();
                    CreateIncludeDrawablesButton();
                    CreateVisualizationToggles();
                }

                // Skip adding the includedDrawables field directly to the inspector
                if (iterator.propertyPath == "includedDrawables")
                {
                    // Display a read-only version of the field
                    var container = new VisualElement();
                    var header = new Label("Included Drawables (Use the 'Include Drawables' button to modify)");
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.marginTop = 5;
                    header.style.marginBottom = 5;

                    container.Add(header);

                    // Create a disabled property field
                    var propertyField = new PropertyField(iterator);
                    propertyField.SetEnabled(false);

                    container.Add(propertyField);
                    _root.Add(container);

                    continue; // Skip the default property field addition below
                }

                var field = new PropertyField(iterator);
                _root.Add(field);
            }
        }
    }

    private void CreateIncludeDrawablesButton()
    {
        var container = new VisualElement();
        container.style.marginBottom = 10;

        var includeButton = new Button(ShowIncludeDrawablesDialog)
        {
            text = "Include Drawables"
        };
        includeButton.AddToClassList("edit-button");

        container.Add(includeButton);
        _root.Add(container);
    }

    private void OnEnable()
    {
        // Cache the current list of drawables for change detection
        Live2DDeformationTracker tracker = target as Live2DDeformationTracker;
        if (tracker != null && tracker.includedDrawables != null)
        {
            _previousDrawables = new CubismDrawable[tracker.includedDrawables.Length];
            tracker.includedDrawables.CopyTo(_previousDrawables, 0);
        }

        ResetEditorState();
    }

    private void OnDisable()
    {
        ResetEditorState();
        SceneView.RepaintAll();
    }

    #endregion

    #region UI Setup

    private void CreateEditModeButton()
    {
        var container = new VisualElement();
        container.style.marginBottom = 10;

        _editButton = new Button(ToggleEditMode)
        {
            text = "Edit Points"
        };
        _editButton.AddToClassList("edit-button");

        container.Add(_editButton);
        UpdateEditButtonState();
        _root.Add(container);
    }

    private void CreateVisualizationToggles()
    {
        var container = new VisualElement();
        container.style.marginBottom = 10;
        container.style.marginTop = 10;

        var titleLabel = new Label("Visualization Options");
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 5;
        container.Add(titleLabel);

        var connectionsToggle = new Toggle("Show Vertex Connections") { value = _showVertexConnections };
        connectionsToggle.RegisterValueChangedCallback(evt =>
        {
            _showVertexConnections = evt.newValue;
            SceneView.RepaintAll();
        });

        container.Add(connectionsToggle);
        _root.Add(container);
    }

    /// <summary>
    /// Recalculates all tracked points for a Live2DDeformationTracker.
    /// Call this after changing the included drawables list.
    /// </summary>
    public static void RecalculateTrackedPoints(Live2DDeformationTracker tracker)
    {
        if (tracker == null || tracker.trackedPoints == null || tracker.trackedPoints.Length == 0)
            return;

        Undo.RecordObject(tracker, "Update Tracked Points");

        // Recalculate vertex references for each point
        for (int i = 0; i < tracker.trackedPoints.Length; i++)
        {
            var point = tracker.trackedPoints[i];

            // Calculate current position
            var currentPosition = tracker.CalculatePointPosition(i);
            Vector2 point2D = new Vector2(currentPosition.x, currentPosition.y);

            // Recalculate vertex references using the current position and radius
            point.vertexReferences = FindVerticesInRadius(
                point2D,
                point.radius,
                tracker.includedDrawables
            );

            tracker.trackedPoints[i] = point;
        }

        EditorUtility.SetDirty(tracker);
    }

    private void ToggleEditMode()
    {
        _isEditing = !_isEditing;
        ResetEditorState();
        UpdateEditButtonState();
        SceneView.RepaintAll();
    }

    private void UpdateEditButtonState()
    {
        if (_editButton == null)
            return;

        _editButton.text = _isEditing ? "Done Editing" : "Edit Points";

        if (_isEditing)
        {
            _editButton.AddToClassList("editing");
        }
        else
        {
            _editButton.RemoveFromClassList("editing");
        }
    }

    private void ResetEditorState()
    {
        if (_isEditing)
            return;

        _selectedPointIndex = -1;
        _isDragging = false;
    }

    private static GUIStyle CreateLabelStyle()
    {
        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = Color.white },
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(5, 5, 2, 2)
        };

        style.normal.background = CreateLabelBackground();

        return style;
    }

    private static Texture2D CreateLabelBackground()
    {
        Texture2D backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, k_LabelBackgroundColor);
        backgroundTexture.Apply();
        return backgroundTexture;
    }

    private void ShowIncludeDrawablesDialog()
    {
        var allDrawables = Tracker.Model.Drawables;
        if (allDrawables == null || allDrawables.Length == 0)
        {
            EditorUtility.DisplayDialog("No Drawables", "No drawables found in the model.", "OK");
            return;
        }

        // Create a dialog to select which drawables to include
        DrawableSelectionWindow.ShowWindow(Tracker, allDrawables);
    }

    #endregion

    #region Vertex Finding and Weight Calculation

    /// <summary>
    /// Finds vertices for tracking, prioritizing those within radius but including outside vertices if needed.
    /// </summary>
    /// <remarks>
    /// The algorithm follows these steps:
    /// 1. Collect all vertices from drawables, marking their distances from the position
    /// 2. Select an optimal subset of vertices for performance
    /// 3. Calculate weights for vertices to ensure their weighted average equals the target position
    ///
    /// Vertices outside the radius can still be selected when not enough good vertices are available within radius.
    /// </remarks>
    private static Live2DDeformationTracker.VertexReference[] FindVerticesInRadius(
        Vector2 position,
        float radius,
        CubismDrawable[] includedDrawables)
    {
        // Skip processing if there are no included drawables
        if (includedDrawables == null || includedDrawables.Length == 0)
            return new Live2DDeformationTracker.VertexReference[0];

        // Step 1: Collect all vertices from drawables, prioritizing those within radius
        var vertexCandidates = CollectVerticesInRadius(position, includedDrawables);

        // Step 2: Select a limited subset of vertices for performance
        var selectedVertices = SelectOptimalVertexSubset(radius, vertexCandidates);

        // Step 3: Calculate optimal weights for the selected vertices (modifies selectedVertices in-place)
        CalculateVertexWeights(position, selectedVertices, includedDrawables);

        return selectedVertices;
    }

    /// <summary>
    /// Collects all vertices from drawables, prioritizing those within radius.
    /// </summary>
    /// <remarks>
    /// Vertices outside the radius are included as fallback options but marked with their distances.
    /// </remarks>
    private static IEnumerable<(int drawableIndex, int vertexIndex, float distance)> CollectVerticesInRadius(
        Vector2 position,
        CubismDrawable[] includedDrawables)
    {
        // Collect all vertices from included drawables
        for (int drawableIndex = 0; drawableIndex < includedDrawables.Length; drawableIndex++)
        {
            var drawable = includedDrawables[drawableIndex];
            var vertices = drawable.VertexPositions;

            // Collect all vertices with their distances
            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = Vector2.Distance(position, vertices[i]);
                yield return (drawableIndex, i, distance);
            }
        }
    }

    /// <summary>
    /// Selects an optimal subset of vertices for better performance.
    /// </summary>
    private static Live2DDeformationTracker.VertexReference[] SelectOptimalVertexSubset(
        float radius,
        IEnumerable<(int drawableIndex, int vertexIndex, float distance)> verticeCandidates)
    {
        const int MAX_VERTICES_PER_DRAWABLE = 3; // Limit vertices per drawable for performance
        const int MAX_TOTAL_VERTICES = 9;        // Maximum total vertices to use
        const float MIN_DISTANCE_THRESHOLD = 0.001f; // Minimum distance between vertices

        // Group vertices by drawable and sort by distance
        var groupedVertices = verticeCandidates
            .GroupBy(t => t.drawableIndex)
            .ToList();

        List<(int drawableIndex, int vertexIndex, float distance)> selectedVertices = new List<(int, int, float)>();

        // First pass: select closest vertices from each drawable
        foreach (var group in groupedVertices)
        {
            var orderedVertsInDrawable = group.OrderBy(t => t.distance).ToList();

            // Take first vertex (closest one) from this drawable
            if (orderedVertsInDrawable.Count > 0)
            {
                selectedVertices.Add(orderedVertsInDrawable[0]);
            }

            // Add more vertices from this drawable, ensuring spatial diversity
            for (int i = 1; i < orderedVertsInDrawable.Count && selectedVertices.Count(v => v.drawableIndex == group.Key) < MAX_VERTICES_PER_DRAWABLE; i++)
            {
                var candidate = orderedVertsInDrawable[i];

                // Skip if too close to already selected vertices from the same drawable
                bool isTooClose = selectedVertices
                    .Where(v => v.drawableIndex == group.Key)
                    .Any(existing =>
                    {
                        var existingVertex = groupedVertices
                            .First(g => g.Key == existing.drawableIndex)
                            .First(v => v.vertexIndex == existing.vertexIndex);

                        return Vector2.Distance(
                            new Vector2(existingVertex.vertexIndex % 100, existingVertex.vertexIndex / 100),
                            new Vector2(candidate.vertexIndex % 100, candidate.vertexIndex / 100)
                        ) < MIN_DISTANCE_THRESHOLD * 100;
                    });

                if (!isTooClose)
                {
                    selectedVertices.Add(candidate);
                }
            }
        }

        // Final selection: take the best vertices overall
        var bestVertices = selectedVertices
            .OrderBy(v => v.distance)
            .Take(MAX_TOTAL_VERTICES);

        return bestVertices.Select(d => new Live2DDeformationTracker.VertexReference
        {
            drawableIndex = d.drawableIndex,
            vertexIndex = d.vertexIndex,
            weight = weightCalculation(d.distance, radius)
        }).ToArray();

        float weightCalculation(float distance, float radius)
        {
            // Exponential falloff with tuned parameters
            float falloffFactor = 1.5f; // Controls how quickly the weight falls off (adjusted for more stability)

            // Small epsilon to avoid zero weights
            distance = Mathf.Max(distance, 0.0001f);

            // Exponential decay function: e^(-falloffFactor * distance/radius)
            float weight = Mathf.Exp(-falloffFactor * distance / radius);

            return weight;
        }
    }

    /// <summary>
    /// Calculates the final vertex weights for the selected vertices and updates them in-place.
    /// </summary>
    private static void CalculateVertexWeights(
        Vector2 position,
        Live2DDeformationTracker.VertexReference[] vertexReferences,
        CubismDrawable[] includedDrawables)
    {
        // Special case: Single vertex
        if (vertexReferences.Length == 1)
        {
            vertexReferences[0].weight = 1.0f;
            return;
        }

        // Collect vertex positions and initial weights
        Vector2[] vertexPositions = new Vector2[vertexReferences.Length];
        float[] initialWeights = new float[vertexReferences.Length];

        for (int i = 0; i < vertexReferences.Length; i++)
        {
            var drawable = includedDrawables[vertexReferences[i].drawableIndex];
            vertexPositions[i] = drawable.VertexPositions[vertexReferences[i].vertexIndex];
            initialWeights[i] = vertexReferences[i].weight;
        }

        // Calculate optimal weights biased by initial weights
        float[] weights = CalculateOptimalWeights(vertexPositions, position, initialWeights);

        // Additional stabilization: small weight values below threshold are removed 
        // and their weight is redistributed to prevent flickering
        const float MIN_MEANINGFUL_WEIGHT = 0.01f;
        float weightSum = 0;
        bool hasSmallWeights = false;

        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] < MIN_MEANINGFUL_WEIGHT)
            {
                hasSmallWeights = true;
                weights[i] = 0;
            }
            else
            {
                weightSum += weights[i];
            }
        }

        // Renormalize if we removed small weights
        if (hasSmallWeights && weightSum > 0)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] /= weightSum;
            }
        }

        // Apply weights to result (in-place)
        for (int i = 0; i < vertexReferences.Length; i++)
        {
            var vRef = vertexReferences[i];
            vRef.weight = weights[i];
            vertexReferences[i] = vRef;
        }
    }

    /// <summary>
    /// Calculates weights based on inverse distances and applies iterative refinement
    /// to ensure the weighted average equals the target position.
    /// </summary>
    private static float[] CalculateOptimalWeights(Vector2[] vertices, Vector2 targetPosition, float[] initialWeights)
    {
        // Initialize weights based on initial weights and inverse distance
        float[] weights = InitializeDistanceBasedWeights(vertices, targetPosition, initialWeights);

        // Apply iterative refinement to adjust weights, biased by initial weights
        RefineWeightsWithGradientDescent(vertices, targetPosition, weights, initialWeights);

        return weights;
    }

    /// <summary>
    /// Initializes weights based on a combination of initial weights and inverse distance.
    /// </summary>
    private static float[] InitializeDistanceBasedWeights(Vector2[] vertices, Vector2 targetPosition, float[] initialWeights)
    {
        int n = vertices.Length;
        float[] weights = new float[n];

        // Calculate combined weights (initial weight * inverse distance)
        float totalWeight = 0;
        for (int i = 0; i < n; i++)
        {
            float dist = Vector2.Distance(targetPosition, vertices[i]);
            dist = Mathf.Max(dist, 0.001f); // Avoid division by zero

            // Combine initial weight with inverse distance
            weights[i] = initialWeights[i] * (1.0f / dist);
            totalWeight += weights[i];
        }

        // Normalize weights
        if (totalWeight > 0)
        {
            for (int i = 0; i < n; i++)
            {
                weights[i] /= totalWeight;
            }
        }
        else
        {
            // Fallback to normalized initial weights
            totalWeight = 0;
            for (int i = 0; i < n; i++)
            {
                totalWeight += initialWeights[i];
            }

            if (totalWeight > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    weights[i] = initialWeights[i] / totalWeight;
                }
            }
            else
            {
                // Last resort: equal weights
                for (int i = 0; i < n; i++)
                {
                    weights[i] = 1.0f / n;
                }
            }
        }

        return weights;
    }

    /// <summary>
    /// Refines weights using gradient descent to minimize the error between weighted average
    /// and target position, while respecting initial weight distribution.
    /// </summary>
    private static void RefineWeightsWithGradientDescent(
        Vector2[] vertices,
        Vector2 targetPosition,
        float[] weights,
        float[] initialWeights)
    {
        const int MAX_ITERATIONS = 100;
        const float INITIAL_LEARNING_RATE = 0.5f;
        const float MIN_LEARNING_RATE = 0.01f;
        const float TOLERANCE = 0.00000001f;
        const float BIAS_FACTOR = 0.9f; // How much to bias towards initial weights (0-1)

        // Precompute vertex-to-target vectors for gradient calculation
        Vector2[] vertexToTarget = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexToTarget[i] = vertices[i] - targetPosition;
        }

        // Keep track of best weights found so far
        float[] bestWeights = new float[weights.Length];
        Array.Copy(weights, bestWeights, weights.Length);
        float bestError = float.MaxValue;

        // Initial error calculation
        Vector2 initialPos = Vector2.zero;
        for (int i = 0; i < weights.Length; i++)
        {
            initialPos += vertices[i] * weights[i];
        }
        float previousErrorMagnitude = (targetPosition - initialPos).sqrMagnitude;

        for (int iter = 0; iter < MAX_ITERATIONS; iter++)
        {
            // Calculate current weighted position
            Vector2 currentPos = Vector2.zero;
            for (int i = 0; i < weights.Length; i++)
            {
                currentPos += vertices[i] * weights[i];
            }

            // Current error vector
            Vector2 error = targetPosition - currentPos;
            float errorMagnitude = error.sqrMagnitude;

            // Save best weights if this is the best result so far
            if (errorMagnitude < bestError)
            {
                bestError = errorMagnitude;
                Array.Copy(weights, bestWeights, weights.Length);
            }

            // Check if we're close enough
            if (errorMagnitude < TOLERANCE)
                break;

            // Adaptive learning rate - reduce if error is not decreasing
            float learningRate = INITIAL_LEARNING_RATE;
            if (errorMagnitude > previousErrorMagnitude)
            {
                // Error increased, reduce learning rate
                learningRate = Mathf.Max(MIN_LEARNING_RATE, learningRate * 0.5f);
            }
            previousErrorMagnitude = errorMagnitude;

            // Decay learning rate over iterations for better convergence
            float iterationProgress = (float)iter / MAX_ITERATIONS;
            learningRate *= (1.0f - 0.7f * iterationProgress);

            // Calculate gradients for each weight
            float[] gradients = new float[weights.Length];
            float totalGradient = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                // Project error onto the vector from current position to this vertex
                Vector2 currentToVertex = vertices[i] - currentPos;
                float errorProjection = Vector2.Dot(error, currentToVertex);

                // Basic gradient calculation
                gradients[i] = errorProjection;

                // Apply bias toward initial weights (more gently)
                if (initialWeights[i] > 0.001f)
                {
                    float desiredRatio = initialWeights[i] / (weights[i] + 0.001f);
                    float biasTerm = BIAS_FACTOR * Mathf.Sign(desiredRatio - 1.0f) *
                                     Mathf.Min(0.5f, Mathf.Abs(desiredRatio - 1.0f));
                    gradients[i] *= (1.0f + biasTerm);
                }

                totalGradient += Mathf.Abs(gradients[i]);
            }

            // Skip if no significant gradient
            if (totalGradient < 0.00001f)
                break;

            // Normalize gradients for consistent step size, but more gently
            for (int i = 0; i < gradients.Length; i++)
            {
                gradients[i] /= (totalGradient + 0.001f);
            }

            // Apply scaled gradients to weights
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] += gradients[i] * learningRate;
            }

            // Apply small amount of smoothing with previous weights
            if (iter > MAX_ITERATIONS / 2)
            {
                for (int i = 0; i < weights.Length; i++)
                {
                    // Gradually increase smoothing as iterations progress
                    float smoothFactor = 0.3f * (iter - MAX_ITERATIONS / 2) / (MAX_ITERATIONS / 2);
                    weights[i] = weights[i] * (1 - smoothFactor) + bestWeights[i] * smoothFactor;
                }
            }

            // Ensure weights stay non-negative
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = Mathf.Max(0, weights[i]);
            }

            // Re-normalize weights to sum to 1
            float totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                totalWeight += weights[i];
            }

            if (totalWeight > 0)
            {
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] /= totalWeight;
                }
            }
        }

        // Use the best weights found during optimization
        Array.Copy(bestWeights, weights, weights.Length);
    }

    #endregion
}