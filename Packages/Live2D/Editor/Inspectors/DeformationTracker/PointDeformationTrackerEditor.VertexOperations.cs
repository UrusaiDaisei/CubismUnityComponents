using System;
using UnityEngine;
using Live2D.Cubism.Core;
using System.Linq;
using Live2D.Cubism.Framework.Utils;

using VertexReference = Live2D.Cubism.Framework.PointDeformationTracker.VertexReference;

namespace Live2D.Cubism.Editor.Inspectors
{
    public sealed partial class PointDeformationTrackerEditor
    {
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
        private static VertexReference[] FindVerticesInRadius(
            Vector2 position,
            float radius,
            ReadOnlySpan<CubismDrawable> includedDrawables)
        {
            // Skip processing if there are no included drawables
            if (includedDrawables.IsEmpty)
                return new VertexReference[0];

            const int MAX_TOTAL_VERTICES = 9;        // Maximum total vertices to use
            var vertexReferences = new VertexReference[MAX_TOTAL_VERTICES];

            // Use the combined function that handles both collection and selection
            int vertexesFound = CollectAndSelectVertices(vertexReferences, position, radius, includedDrawables);
            if (vertexesFound < MAX_TOTAL_VERTICES)
                Array.Resize(ref vertexReferences, vertexesFound);

            // Calculate optimal weights for the selected vertices (modifies selectedVertices in-place)
            CalculateVertexWeights(position, vertexReferences, includedDrawables);

            return vertexReferences;
        }

        /// <summary>
        /// Calculates the final vertex weights for the selected vertices and updates them in-place.
        /// </summary>
        private static void CalculateVertexWeights(
            Vector2 position,
            Span<VertexReference> vertexReferences,
            ReadOnlySpan<CubismDrawable> includedDrawables)
        {
            // Special case: Single vertex
            if (vertexReferences.Length == 1)
            {
                vertexReferences[0].weight = 1.0f;
                return;
            }

            // Collect vertex positions and initial weights
            Span<Vector2> vertexPositions = stackalloc Vector2[vertexReferences.Length];
            Span<float> initialWeights = stackalloc float[vertexReferences.Length];

            for (int i = 0; i < vertexReferences.Length; i++)
            {
                var drawable = includedDrawables[vertexReferences[i].drawableIndex];
                vertexPositions[i] = drawable.VertexPositions[vertexReferences[i].vertexIndex];
                initialWeights[i] = vertexReferences[i].weight;
            }

            // Calculate optimal weights biased by initial weights
            float[] weights = CalculateOptimalWeights(vertexPositions, position, initialWeights);

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
        private static float[] CalculateOptimalWeights(ReadOnlySpan<Vector2> vertices, Vector2 targetPosition, ReadOnlySpan<float> initialWeights)
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
        private static float[] InitializeDistanceBasedWeights(ReadOnlySpan<Vector2> vertices, Vector2 targetPosition, ReadOnlySpan<float> initialWeights)
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
            ReadOnlySpan<Vector2> vertices,
            Vector2 targetPosition,
            float[] weights,
            ReadOnlySpan<float> initialWeights)
        {
            const int MAX_COMPUTE_TIME_MS = 250;
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

            // Start timer for maximum compute time limit
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds < MAX_COMPUTE_TIME_MS)
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

                // Calculate time progress (0 at start, approaching 1 at end of compute time)
                float timeProgress = (float)stopwatch.ElapsedMilliseconds / MAX_COMPUTE_TIME_MS;

                // Decay learning rate over time for better convergence
                learningRate *= (1.0f - 0.7f * timeProgress);

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
                // Use time-based smoothing factor instead of iteration-based
                if (timeProgress > 0.5f)
                {
                    for (int i = 0; i < weights.Length; i++)
                    {
                        // Gradually increase smoothing as time progresses
                        float smoothFactor = 0.3f * (timeProgress - 0.5f) / 0.5f;
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

        private static int CollectAndSelectVertices(
            Span<VertexReference> resultBuffer,
            Vector2 position,
            float radius,
            ReadOnlySpan<CubismDrawable> includedDrawables)
        {
            const int MaxVerticesPerDrawable = 3; // Limit vertices per drawable for performance
            const float MinDistanceThreshold = 0.001f; // Minimum distance between vertices

            // Pre-allocate buffer for all potential vertices (MaxVerticesPerDrawable per drawable)
            int maxPossibleVertices = MaxVerticesPerDrawable * includedDrawables.Length;
            Span<(int drawableIndex, int vertexIndex, float distance)> candidateVertices =
                stackalloc (int, int, float)[maxPossibleVertices];

            int totalCandidates = CollectNearestVertices(
                position,
                includedDrawables,
                candidateVertices,
                MaxVerticesPerDrawable,
                MinDistanceThreshold);

            // Sort candidates by distance and copy to result buffer
            return CopyToResultBuffer(
                candidateVertices.Slice(0, totalCandidates),
                resultBuffer,
                position,
                radius);

            // Nested function: Collects the nearest vertices from each drawable with spatial diversity check
            int CollectNearestVertices(
                Vector2 pos,
                ReadOnlySpan<CubismDrawable> drawables,
                Span<(int drawableIndex, int vertexIndex, float distance)> candidateBuffer,
                int maxVerticesPerDrawable,
                float minDistanceThreshold)
            {
                int totalVerticesFilled = 0;

                for (int drawableIndex = 0; drawableIndex < drawables.Length; drawableIndex++)
                {
                    var drawable = drawables[drawableIndex];
                    var vertices = drawable.VertexPositions;

                    if (vertices.Length == 0)
                        continue;

                    // Get a slice for this drawable's vertices in the candidate buffer
                    var drawableVerticesSlice = candidateBuffer.Slice(totalVerticesFilled, maxVerticesPerDrawable);
                    drawableVerticesSlice.Fill((-1, -1, float.MaxValue));

                    int filledCount = 0;
                    float maxDistanceInSelection = float.MaxValue;

                    // Process each vertex in this drawable
                    for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                    {
                        float distance = Vector2.Distance(pos, vertices[vertexIndex]);

                        // Skip if buffer is full and this vertex is further than our furthest
                        if (filledCount == maxVerticesPerDrawable && distance >= maxDistanceInSelection)
                            continue;

                        // Skip if too close to an already selected vertex (spatial diversity check)
                        if (filledCount > 0 && IsTooCloseToExistingVertex(
                            vertices,
                            vertexIndex,
                            drawableVerticesSlice,
                            filledCount,
                            distance,
                            minDistanceThreshold))
                            continue;

                        // Insert vertex at correct position to maintain sorted order
                        int insertIndex = FindInsertionIndex(drawableVerticesSlice, filledCount, distance);

                        if (filledCount < maxVerticesPerDrawable)
                        {
                            // Shift elements if needed
                            if (insertIndex < filledCount)
                                drawableVerticesSlice.RightShift(insertIndex, 1);

                            drawableVerticesSlice[insertIndex] = (drawableIndex, vertexIndex, distance);
                            filledCount++;

                            if (filledCount == maxVerticesPerDrawable)
                                maxDistanceInSelection = drawableVerticesSlice[maxVerticesPerDrawable - 1].distance;
                        }
                        else
                        {
                            // Buffer is full, but this vertex is closer than at least one entry
                            if (insertIndex < maxVerticesPerDrawable - 1)
                                drawableVerticesSlice.RightShift(insertIndex, 1);

                            drawableVerticesSlice[insertIndex] = (drawableIndex, vertexIndex, distance);
                            maxDistanceInSelection = drawableVerticesSlice[maxVerticesPerDrawable - 1].distance;
                        }
                    }

                    totalVerticesFilled += filledCount;
                }

                return totalVerticesFilled;
            }

            // Nested function: Check if a vertex is too close to any already selected vertex
            bool IsTooCloseToExistingVertex(
                ReadOnlySpan<Vector3> vertices,
                int candidateIndex,
                ReadOnlySpan<(int drawableIndex, int vertexIndex, float distance)> selectedVertices,
                int selectedCount,
                float candidateDistance,
                float minDistanceThreshold)
            {
                Vector2 candidatePosition = vertices[candidateIndex];

                for (int i = 0; i < selectedCount; i++)
                {
                    // Skip invalid vertices
                    if (selectedVertices[i].vertexIndex < 0)
                        continue;

                    // Optimization: Skip checking vertices that are much further away
                    if (selectedVertices[i].distance > candidateDistance * 2)
                        continue;

                    // Check spatial distance between vertices
                    Vector2 existingPosition = vertices[selectedVertices[i].vertexIndex];
                    if (Vector2.Distance(existingPosition, candidatePosition) < minDistanceThreshold)
                        return true;
                }

                return false;
            }

            // Nested function: Find the index where a vertex should be inserted to maintain sorted order
            int FindInsertionIndex(
                ReadOnlySpan<(int drawableIndex, int vertexIndex, float distance)> vertices,
                int count,
                float distance)
            {
                int index = 0;
                while (index < count && vertices[index].distance < distance)
                    index++;

                return index;
            }

            // Nested function: Copy sorted vertices to the result buffer with weights
            int CopyToResultBuffer(
                Span<(int drawableIndex, int vertexIndex, float distance)> sortedVertices,
                Span<VertexReference> resultBuffer,
                Vector2 pos,
                float rad)
            {
                // Sort by distance
                var bestVertices = sortedVertices.OrderBy(v => v.distance);
                int totalVerticesToCopy = Math.Min(resultBuffer.Length, bestVertices.Length);

                for (int i = 0; i < totalVerticesToCopy; i++)
                {
                    resultBuffer[i] = new VertexReference
                    {
                        drawableIndex = bestVertices[i].drawableIndex,
                        vertexIndex = bestVertices[i].vertexIndex,
                        weight = CalculateVertexWeight(bestVertices[i].distance, rad)
                    };
                }

                return totalVerticesToCopy;
            }

            // Nested function: Calculate weight for a vertex based on its distance from target position
            float CalculateVertexWeight(float distance, float rad)
            {
                // Prevent division by zero and ensure minimum weight
                const float minDistance = 0.0001f;
                distance = Mathf.Max(distance, minDistance);

                // Higher decay factor = sharper falloff from center point
                const float decayFactor = 1.5f;
                return Mathf.Exp(-decayFactor * distance / rad);
            }
        }
    }
}