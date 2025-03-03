using System;
using UnityEngine;
using Live2D.Cubism.Core;
using System.Linq;
using Live2D.Cubism.Framework.Utils;
using VertexReference = Live2D.Cubism.Framework.PointDeformationTracker.VertexReference;
using Live2D.Cubism.Framework;

namespace Live2D.Cubism.Editor.Inspectors
{
    public sealed partial class PointDeformationTrackerEditor
    {
        /// <summary>
        /// Contains all constants used in vertex operations.
        /// </summary>
        private static class Constants
        {
            /// <summary>
            /// Maximum number of vertices to include in deformation calculations.
            /// </summary>
            public const int MAX_TOTAL_VERTICES = PointDeformationTracker.MAX_TOTAL_VERTICES;

            #region Gradient Descent Parameters

            /// <summary>
            /// Starting learning rate for gradient descent optimization.
            /// </summary>
            public const float INITIAL_LEARNING_RATE = 0.5f;

            /// <summary>
            /// Factor by which learning rate decays over time.
            /// </summary>
            public const float LEARNING_RATE_DECAY_FACTOR = 0.7f;

            /// <summary>
            /// Factor to reduce learning rate when overshooting occurs.
            /// </summary>
            public const float LEARNING_RATE_OVERSHOOT_DECAY = 0.5f;

            /// <summary>
            /// Minimum learning rate to prevent optimization from stalling.
            /// </summary>
            public const float MIN_LEARNING_RATE = 0.01f;

            /// <summary>
            /// Minimum distance considered significant during optimization.
            /// </summary>
            public const float DISTANCE_TOLERANCE = 0.00001f;

            /// <summary>
            /// Squared distance tolerance used as error threshold.
            /// </summary>
            public const float ERROR_TOLERANCE = DISTANCE_TOLERANCE * DISTANCE_TOLERANCE;

            /// <summary>
            /// Factor used to bias weights toward initial values.
            /// </summary>
            public const float BIAS_FACTOR = 0.9f;

            /// <summary>
            /// Threshold below which gradient is considered insignificant.
            /// </summary>
            public const float MIN_GRADIENT_THRESHOLD = 0.00001f;

            /// <summary>
            /// Maximum amount of blend influence toward best solution.
            /// </summary>
            public const float MAX_BLEND_INFLUENCE = 0.3f;

            /// <summary>
            /// Progress threshold at which blending begins.
            /// </summary>
            public const float BLEND_START_THRESHOLD = 0.5f;

            /// <summary>
            /// Maximum computation time in milliseconds to prevent excessive processing.
            /// </summary>
            public const int MAX_COMPUTE_TIME_MS = 250;

            #endregion

            #region Weight Bias Parameters

            /// <summary>
            /// Minimum initial weight to consider for bias adjustment.
            /// </summary>
            public const float SIGNIFICANT_WEIGHT_THRESHOLD = 0.001f;

            /// <summary>
            /// Safety value to prevent division by zero in weight calculations.
            /// </summary>
            public const float EPSILON = 0.001f;

            /// <summary>
            /// Target ratio between initial and current weights.
            /// </summary>
            public const float REFERENCE_RATIO = 1.0f;

            /// <summary>
            /// Maximum allowed bias modification to prevent excessive correction.
            /// </summary>
            public const float MAX_BIAS_EFFECT = 0.5f;

            #endregion

            #region Vertex Collection Parameters

            /// <summary>
            /// Maximum vertices to collect per drawable.
            /// </summary>
            public const int MAX_VERTICES_PER_DRAWABLE = 3;

            /// <summary>
            /// Minimum distance threshold between vertices to be considered separate.
            /// </summary>
            public const float MIN_DISTANCE_THRESHOLD = 0.001f;

            /// <summary>
            /// Multiplier used to determine when to ignore vertices based on distance.
            /// </summary>
            public const float IGNORE_DISTANCE_MULTIPLIER = 2;

            #endregion

            #region Weight Calculation Parameters

            /// <summary>
            /// Minimum distance value to prevent division by zero.
            /// </summary>
            public const float MIN_DISTANCE = 0.000001f;

            /// <summary>
            /// Factor controlling the decay rate of weight with distance.
            /// </summary>
            public const float DECAY_FACTOR = 1.5f;

            #endregion
        }

        /// <summary>
        /// Represents a candidate vertex with its distance from a point.
        /// </summary>
        private struct VertexCandidate
        {
            public int DrawableIndex;
            public int VertexIndex;
            public float Distance;

            public VertexCandidate(int drawableIndex, int vertexIndex, float distance)
            {
                DrawableIndex = drawableIndex;
                VertexIndex = vertexIndex;
                Distance = distance;
            }
        }

        /// <summary>
        /// Stores data for weight annealing process.
        /// </summary>
        private struct WeightAnnealingData
        {
            public Vector2 vertex;
            public float initialWeight;
        }

        /// <summary>
        /// Finds vertices within a specified radius from a position.
        /// </summary>
        /// <param name="position">The center position to search from.</param>
        /// <param name="radius">The radius within which to find vertices.</param>
        /// <param name="includedDrawables">Drawables to search for vertices.</param>
        /// <returns>Array of vertex references within the radius.</returns>
        private static VertexReference[] FindVerticesInRadius(
            Vector2 position,
            float radius,
            ReadOnlySpan<CubismDrawable> includedDrawables)
        {
            if (includedDrawables.IsEmpty)
                return new VertexReference[0];

            var vertexReferences = new VertexReference[Constants.MAX_TOTAL_VERTICES];

            int vertexesFound = CollectAndSelectVertices(vertexReferences, position, radius, includedDrawables);
            if (vertexesFound < Constants.MAX_TOTAL_VERTICES)
                Array.Resize(ref vertexReferences, vertexesFound);

            CalculateVertexWeights(position, vertexReferences, includedDrawables);

            return vertexReferences;
        }

        /// <summary>
        /// Calculates weights for vertices based on their position relative to a target point.
        /// </summary>
        /// <param name="position">The target position.</param>
        /// <param name="vertexReferences">The vertex references to calculate weights for.</param>
        /// <param name="includedDrawables">The drawables containing the vertices.</param>
        private static void CalculateVertexWeights(
            Vector2 position,
            Span<VertexReference> vertexReferences,
            ReadOnlySpan<CubismDrawable> includedDrawables)
        {
            if (vertexReferences.Length == 1)
            {
                vertexReferences[0].weight = 1.0f;
                return;
            }

            Span<WeightAnnealingData> weightAnnealingData = stackalloc WeightAnnealingData[vertexReferences.Length];
            Span<float> weights = stackalloc float[vertexReferences.Length];

            for (int i = 0; i < vertexReferences.Length; i++)
            {
                var drawable = includedDrawables[vertexReferences[i].drawableIndex];
                weightAnnealingData[i] = new WeightAnnealingData
                {
                    vertex = drawable.VertexPositions[vertexReferences[i].vertexIndex],
                    initialWeight = vertexReferences[i].weight
                };
            }

            CalculateOptimalWeights(weights, position, weightAnnealingData);

            for (int i = 0; i < vertexReferences.Length; i++)
                vertexReferences[i].weight = weights[i];
        }

        /// <summary>
        /// Calculates optimal vertex weights for a target position.
        /// </summary>
        /// <param name="weights">Output buffer for the calculated weights.</param>
        /// <param name="targetPosition">The target position to reach.</param>
        /// <param name="weightAnnealingData">Data for weight annealing process.</param>
        private static void CalculateOptimalWeights(Span<float> weights, Vector2 targetPosition, ReadOnlySpan<WeightAnnealingData> weightAnnealingData)
        {
            InitializeDistanceBasedWeights(weights, weightAnnealingData, targetPosition);
            RefineWeightsWithGradientDescent(weights, targetPosition, weightAnnealingData);
        }

        /// <summary>
        /// Initializes weights based on distance from vertices to target position.
        /// </summary>
        /// <param name="weights">Output buffer for the calculated weights.</param>
        /// <param name="weightAnnealingData">Data containing vertex positions and initial weights.</param>
        /// <param name="targetPosition">The target position.</param>
        private static void InitializeDistanceBasedWeights(Span<float> weights, ReadOnlySpan<WeightAnnealingData> weightAnnealingData, Vector2 targetPosition)
        {
            float totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                float dist = Vector2.Distance(targetPosition, weightAnnealingData[i].vertex);
                dist = Mathf.Max(dist, 0.001f);

                weights[i] = weightAnnealingData[i].initialWeight * (1.0f / dist);
                totalWeight += weights[i];
            }

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] /= totalWeight;
            }
        }

        /// <summary>
        /// Refines vertex weights using gradient descent to optimize the weighted position towards a target position.
        /// </summary>
        /// <param name="weights">The weights to optimize.</param>
        /// <param name="targetPosition">The target position to reach.</param>
        /// <param name="weightAnnealingData">Data containing vertex positions and initial weights.</param>
        private static void RefineWeightsWithGradientDescent(
            Span<float> weights,
            Vector2 targetPosition,
            ReadOnlySpan<WeightAnnealingData> weightAnnealingData)
        {
            Span<float> bestWeights = stackalloc float[weights.Length];
            Span<float> gradients = stackalloc float[weights.Length];
            float bestError = float.MaxValue;
            float previousErrorMagnitude = float.NegativeInfinity;

            weights.CopyTo(bestWeights);
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            do
            {
                // Calculate current position
                var currentPos = CalculateWeightedPosition(weights, weightAnnealingData);

                // Calculate error and update best solution if needed
                var error = targetPosition - currentPos;
                float errorMagnitude = error.sqrMagnitude;

                UpdateBestSolution(weights, bestWeights, errorMagnitude);

                if (errorMagnitude < Constants.ERROR_TOLERANCE)
                    break;

                // Adjust learning rate
                float timeProgress = (float)stopwatch.ElapsedMilliseconds / Constants.MAX_COMPUTE_TIME_MS;
                float learningRate = CalculateLearningRate(errorMagnitude, timeProgress);

                // Calculate and apply gradients
                if (!CalculateAndApplyGradients(weights, gradients, bestWeights, weightAnnealingData,
                                               currentPos, error, learningRate, timeProgress))
                    break;

                // Enforce constraints
                NormalizeWeights(weights);

            } while (stopwatch.ElapsedMilliseconds < Constants.MAX_COMPUTE_TIME_MS);

            // Use the best solution found
            bestWeights.CopyTo(weights);

            // Nested function to calculate the weighted position
            static Vector2 CalculateWeightedPosition(
                ReadOnlySpan<float> currentWeights,
                ReadOnlySpan<WeightAnnealingData> data)
            {
                var position = Vector2.zero;
                for (int i = 0; i < currentWeights.Length; i++)
                    position += data[i].vertex * currentWeights[i];
                return position;
            }

            // Nested function to update the best solution
            void UpdateBestSolution(
                ReadOnlySpan<float> currentWeights,
                Span<float> bestWeightsSoFar,
                float currentError)
            {
                if (currentError > bestError)
                    return;

                bestError = currentError;
                currentWeights.CopyTo(bestWeightsSoFar);
            }

            // Nested function to calculate the learning rate
            float CalculateLearningRate(
                float currentError,
                float progress)
            {
                float rate = Constants.INITIAL_LEARNING_RATE;

                // Reduce learning rate if error increased
                if (currentError > previousErrorMagnitude)
                    rate = Mathf.Max(Constants.MIN_LEARNING_RATE, rate * Constants.LEARNING_RATE_OVERSHOOT_DECAY);

                previousErrorMagnitude = currentError;

                // Gradually reduce learning rate as time passes
                rate *= 1.0f - Constants.LEARNING_RATE_DECAY_FACTOR * progress;

                return rate;
            }

            // Nested function to calculate and apply gradients
            static bool CalculateAndApplyGradients(
                Span<float> weights,
                Span<float> gradients,
                ReadOnlySpan<float> bestWeights,
                ReadOnlySpan<WeightAnnealingData> weightData,
                Vector2 currentPos,
                Vector2 error,
                float learningRate,
                float timeProgress)
            {
                // Calculate gradients for each weight
                float totalGradient = CalculateGradients(gradients, weights, weightData, currentPos, error);

                // Early termination if gradients are too small
                if (totalGradient < Constants.MIN_GRADIENT_THRESHOLD)
                    return false;

                // Normalize gradients for stable updates
                NormalizeGradients(gradients, totalGradient);

                // Update weights based on gradients and learning rate
                ApplyGradients(weights, gradients, learningRate);

                // In later stages, blend toward best solution found so far
                if (timeProgress > Constants.BLEND_START_THRESHOLD)
                    BlendWithBestSolution(weights, bestWeights, timeProgress);

                return true;
            }

            // Calculate gradients for each weight
            static float CalculateGradients(
                Span<float> gradients,
                ReadOnlySpan<float> weights,
                ReadOnlySpan<WeightAnnealingData> weightData,
                Vector2 currentPos,
                Vector2 error)
            {
                float totalGradient = 0;

                for (int i = 0; i < weights.Length; i++)
                {
                    // Projection of error onto vector from current position to this vertex
                    var currentToVertex = weightData[i].vertex - currentPos;
                    float errorProjection = Vector2.Dot(error, currentToVertex);
                    gradients[i] = errorProjection;

                    // Apply bias toward initial weights if they were significant
                    ApplyInitialWeightBias(gradients, weights, weightData, i);

                    totalGradient += Mathf.Abs(gradients[i]);
                }

                return totalGradient;
            }

            // Apply bias toward initial weights to preserve original vertex influence
            static void ApplyInitialWeightBias(
                Span<float> gradients,
                ReadOnlySpan<float> weights,
                ReadOnlySpan<WeightAnnealingData> weightData,
                int index)
            {
                // Only apply bias for vertices that had significant influence initially
                if (weightData[index].initialWeight <= Constants.SIGNIFICANT_WEIGHT_THRESHOLD)
                    return;

                // Calculate ratio between initial weight and current weight
                // Values > 1 mean the vertex has lost influence, values < 1 mean it gained influence
                float desiredRatio = weightData[index].initialWeight / (weights[index] + Constants.EPSILON);

                // Calculate bias term:
                // - Sign determines direction (increase or decrease gradient)
                // - Magnitude is capped to prevent excessive correction
                float ratioDeviation = desiredRatio - Constants.REFERENCE_RATIO;
                float biasNudgeMagnitude = Mathf.Min(Constants.MAX_BIAS_EFFECT, Mathf.Abs(ratioDeviation));
                float biasTerm = Constants.BIAS_FACTOR * Mathf.Sign(ratioDeviation) * biasNudgeMagnitude;

                // Adjust gradient to bias toward the initial weight
                gradients[index] *= 1.0f + biasTerm;
            }

            // Normalize gradients
            static void NormalizeGradients(Span<float> gradients, float totalGradient)
            {
                totalGradient += Constants.EPSILON;
                for (int i = 0; i < gradients.Length; i++)
                    gradients[i] /= totalGradient;
            }

            // Apply gradients to weights
            static void ApplyGradients(Span<float> weights, ReadOnlySpan<float> gradients, float learningRate)
            {
                for (int i = 0; i < weights.Length; i++)
                    weights[i] += gradients[i] * learningRate;
            }

            // Blend with best solution
            static void BlendWithBestSolution(Span<float> weights, ReadOnlySpan<float> bestWeights, float timeProgress)
            {
                float smoothFactor = Constants.MAX_BLEND_INFLUENCE * (timeProgress - Constants.BLEND_START_THRESHOLD) / Constants.BLEND_START_THRESHOLD;
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = Mathf.LerpUnclamped(weights[i], bestWeights[i], smoothFactor);
            }

            // Enforce non-negative weights and normalize (improves stability)
            static void NormalizeWeights(Span<float> weights)
            {
                // Enforce non-negative weights
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = Mathf.Max(0, weights[i]);

                // Normalize weights to sum to 1
                float totalWeight = 0;
                for (int i = 0; i < weights.Length; i++)
                {
                    totalWeight += weights[i];
                }

                if (totalWeight <= 0)
                    return;

                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] /= totalWeight;
                }
            }
        }

        /// <summary>
        /// Collects and selects vertices near a position.
        /// </summary>
        /// <param name="resultBuffer">Buffer to store the resulting vertex references.</param>
        /// <param name="position">The position to search near.</param>
        /// <param name="radius">The radius within which to find vertices.</param>
        /// <param name="includedDrawables">The drawables to search.</param>
        /// <returns>Number of vertices collected.</returns>
        private static int CollectAndSelectVertices(
            Span<VertexReference> resultBuffer,
            Vector2 position,
            float radius,
            ReadOnlySpan<CubismDrawable> includedDrawables)
        {
            int maxPossibleVertices = Constants.MAX_VERTICES_PER_DRAWABLE * includedDrawables.Length;

            Span<VertexCandidate> candidateVertices = stackalloc VertexCandidate[maxPossibleVertices];

            CollectNearestVertices(position, includedDrawables, candidateVertices);

            return CopyToResultBuffer(
                candidateVertices,
                resultBuffer,
                position,
                radius);

            void CollectNearestVertices(
                Vector2 pos,
                ReadOnlySpan<CubismDrawable> drawables,
                Span<VertexCandidate> candidateBuffer)
            {
                int totalVerticesFilled = 0;
                var targetTotalVertices = candidateBuffer.Length;

                for (int drawableIndex = 0; drawableIndex < drawables.Length; drawableIndex++)
                {
                    // Exit early if buffer is already full
                    if (totalVerticesFilled >= targetTotalVertices)
                        break;

                    var drawable = drawables[drawableIndex];
                    var vertices = drawable.VertexPositions;

                    // Calculate remaining vertices needed and available drawables
                    var remainingVerticesNeeded = targetTotalVertices - totalVerticesFilled;
                    var remainingDrawables = drawables.Length - drawableIndex;

                    // Determine if we need to relax constraints
                    // If we're at risk of not filling the buffer, start relaxing constraints
                    bool relaxConstraints = remainingVerticesNeeded >
                        remainingDrawables * Constants.MAX_VERTICES_PER_DRAWABLE / 2;  // Conservative estimate

                    // Calculate how many more vertices we can take from this drawable
                    int remainingCapacity = Math.Min(
                        Constants.MAX_VERTICES_PER_DRAWABLE,
                        targetTotalVertices - totalVerticesFilled);

                    var drawableVerticesSlice = candidateBuffer.Slice(totalVerticesFilled, remainingCapacity);
                    drawableVerticesSlice.Fill(new VertexCandidate(-1, -1, float.MaxValue));

                    int filledCount = 0;
                    var maxDistanceInSelection = float.MaxValue;

                    for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                    {
                        float distance = Vector2.Distance(pos, vertices[vertexIndex]);

                        if (filledCount == remainingCapacity && distance >= maxDistanceInSelection)
                            continue;

                        // Skip the proximity check if we're relaxing constraints OR
                        // if we haven't filled this drawable's allocation yet
                        bool skipProximityCheck = relaxConstraints || filledCount < remainingCapacity / 2;

                        if (!skipProximityCheck && filledCount > 0 && IsTooCloseToExistingVertex(
                            vertices,
                            vertexIndex,
                            drawableVerticesSlice,
                            filledCount,
                            distance,
                            Constants.MIN_DISTANCE_THRESHOLD))
                            continue;

                        var insertIndex = FindInsertionIndex(drawableVerticesSlice, filledCount, distance);

                        if (filledCount < remainingCapacity)
                        {
                            if (insertIndex < filledCount)
                                drawableVerticesSlice.RightShift(insertIndex, 1);

                            drawableVerticesSlice[insertIndex] = new VertexCandidate(drawableIndex, vertexIndex, distance);
                            filledCount++;

                            if (filledCount == remainingCapacity)
                                maxDistanceInSelection = drawableVerticesSlice[remainingCapacity - 1].Distance;
                        }
                        else
                        {
                            if (insertIndex < remainingCapacity - 1)
                                drawableVerticesSlice.RightShift(insertIndex, 1);

                            drawableVerticesSlice[insertIndex] = new VertexCandidate(drawableIndex, vertexIndex, distance);
                            maxDistanceInSelection = drawableVerticesSlice[remainingCapacity - 1].Distance;
                        }
                    }

                    totalVerticesFilled += filledCount;
                }
            }

            bool IsTooCloseToExistingVertex(
                ReadOnlySpan<Vector3> vertices,
                int candidateIndex,
                ReadOnlySpan<VertexCandidate> selectedVertices,
                int selectedCount,
                float candidateDistance,
                float minDistanceThreshold)
            {
                var candidatePosition = vertices[candidateIndex];

                for (int i = 0; i < selectedCount; i++)
                {
                    if (selectedVertices[i].VertexIndex < 0)
                        continue;

                    if (selectedVertices[i].Distance > candidateDistance * Constants.IGNORE_DISTANCE_MULTIPLIER)
                        continue;

                    var existingPosition = vertices[selectedVertices[i].VertexIndex];
                    if (Vector2.Distance(existingPosition, candidatePosition) < minDistanceThreshold)
                        return true;
                }

                return false;
            }

            int FindInsertionIndex(
                ReadOnlySpan<VertexCandidate> vertices,
                int count,
                float distance)
            {
                int index = 0;
                while (index < count && vertices[index].Distance < distance)
                    index++;

                return index;
            }

            int CopyToResultBuffer(
                Span<VertexCandidate> candidateVertices,
                Span<VertexReference> resultBuffer,
                Vector2 pos,
                float rad)
            {
                var bestVertices = candidateVertices.OrderBy(v => v.Distance);
                int targetCount = Math.Min(Constants.MAX_TOTAL_VERTICES, resultBuffer.Length);

                // If we have fewer vertices than needed, we take them all
                int totalVerticesToCopy = Math.Min(targetCount, bestVertices.Length);

                for (int i = 0; i < totalVerticesToCopy; i++)
                {
                    resultBuffer[i] = new VertexReference
                    {
                        drawableIndex = bestVertices[i].DrawableIndex,
                        vertexIndex = bestVertices[i].VertexIndex,
                        weight = CalculateVertexWeight(bestVertices[i].Distance, rad)
                    };
                }

                return totalVerticesToCopy;
            }

            float CalculateVertexWeight(float distance, float rad)
            {
                distance = Mathf.Max(distance, Constants.MIN_DISTANCE);
                return Mathf.Exp(-Constants.DECAY_FACTOR * distance / rad);
            }
        }
    }
}