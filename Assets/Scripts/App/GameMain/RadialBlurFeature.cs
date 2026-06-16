#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace App
{
    /// <summary>
    /// URP 2D Renderer にラジアルブラーを差し込む ScriptableRendererFeature。
    /// Renderer2D の Renderer Features に追加してから使用する。
    /// intensity を外から直接書き換えることでブラー強度を制御する。
    /// </summary>
    public sealed class RadialBlurFeature : ScriptableRendererFeature
    {
        public static RadialBlurFeature? Instance { get; private set; }

        [Range(0f, 0.5f)] public float intensity   = 0f;
        [Range(2, 16)]    public int   sampleCount = 8;

        private RadialBlurPass? _pass;
        private Material?       _material;

        private static readonly int IntensityId   = Shader.PropertyToID("_Intensity");
        private static readonly int SampleCountId = Shader.PropertyToID("_SampleCount");

        // ── ScriptableRendererFeature ────────────────────────────────

        public override void Create()
        {
            Instance = this;

            var shader = Shader.Find("Hidden/WrestlePachi/RadialBlur");
            if (shader == null)
            {
                Debug.LogWarning("[RadialBlurFeature] Shader 'Hidden/WrestlePachi/RadialBlur' が見つかりません。");
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(shader);
            _pass = new RadialBlurPass(_material)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
            };
        }

#pragma warning disable CS0672, CS0618
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            => EnqueueIfNeeded(renderer);
#pragma warning restore CS0672, CS0618

        private void EnqueueIfNeeded(ScriptableRenderer renderer)
        {
            if (_pass == null || _material == null || intensity <= 0.001f) return;

            _material.SetFloat(IntensityId,   intensity);
            _material.SetInt(SampleCountId, sampleCount);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            CoreUtils.Destroy(_material);
            if (Instance == this) Instance = null;
        }

        // ── 内部 Pass ────────────────────────────────────────────────

#pragma warning disable CS0672, CS0618
        private sealed class RadialBlurPass : ScriptableRenderPass
        {
            private readonly Material _mat;
            private RTHandle?         _tempRT;

            public RadialBlurPass(Material mat) => _mat = mat;

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, name: "_RadialBlurTemp");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_tempRT == null) return;

                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                if (source == null) return;

                var cmd = CommandBufferPool.Get("RadialBlur");
                Blitter.BlitCameraTexture(cmd, source, _tempRT, _mat, 0);
                Blitter.BlitCameraTexture(cmd, _tempRT, source);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd) { }

            public void Dispose() => _tempRT?.Release();
        }
#pragma warning restore CS0672, CS0618
    }
}
#nullable disable
