/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using System.Runtime.CompilerServices;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.Profiling;
#endif

namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// Lightweight per-frame diagnostics for Cubism rendering submission metrics.
    /// </summary>
    internal static class CubismRenderDiagnostics
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static int _frameCount = -1;
        private static int _drawSubmissions;
        private static int _propertyBlockWrites;
        private static int _renderTargetBinds;
        private static int _clearRenderTargetCalls;
        private static int _blitCalls;

        internal static int DrawSubmissions => _drawSubmissions;
        internal static int PropertyBlockWrites => _propertyBlockWrites;
        internal static int RenderTargetBinds => _renderTargetBinds;
        internal static int ClearRenderTargetCalls => _clearRenderTargetCalls;
        internal static int BlitCalls => _blitCalls;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureFrameState()
        {
            var currentFrame = Time.frameCount;
            if (currentFrame == _frameCount)
            {
                return;
            }

            _frameCount = currentFrame;
            _drawSubmissions = 0;
            _propertyBlockWrites = 0;
            _renderTargetBinds = 0;
            _clearRenderTargetCalls = 0;
            _blitCalls = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountDrawSubmission()
        {
            EnsureFrameState();
            _drawSubmissions++;
            Profiler.BeginSample("CubismMetrics.DrawSubmission");
            Profiler.EndSample();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountPropertyBlockWrite()
        {
            EnsureFrameState();
            _propertyBlockWrites++;
            Profiler.BeginSample("CubismMetrics.PropertyBlockWrite");
            Profiler.EndSample();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountRenderTargetBind()
        {
            EnsureFrameState();
            _renderTargetBinds++;
            Profiler.BeginSample("CubismMetrics.RenderTargetBind");
            Profiler.EndSample();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountClearRenderTarget()
        {
            EnsureFrameState();
            _clearRenderTargetCalls++;
            Profiler.BeginSample("CubismMetrics.ClearRenderTarget");
            Profiler.EndSample();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountBlit()
        {
            EnsureFrameState();
            _blitCalls++;
            Profiler.BeginSample("CubismMetrics.Blit");
            Profiler.EndSample();
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountDrawSubmission() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountPropertyBlockWrite() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountRenderTargetBind() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountClearRenderTarget() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountBlit() { }
#endif
    }
}
