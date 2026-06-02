using System;
using UnityEngine;

namespace App
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TengekiGlowController : MonoBehaviour
    {
        public enum GlowColorMode
        {
            Custom = 0,
            Red = 1,
            Blue = 2,
            Yellow = 3,
            Green = 4,
            Rainbow = 5,
        }

        private static readonly int ColorModeId = Shader.PropertyToID("_ColorMode");
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int SecondaryColorId = Shader.PropertyToID("_SecondaryColor");
        private static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
        private static readonly int EffectAmountId = Shader.PropertyToID("_EffectAmount");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int GlassStrengthId = Shader.PropertyToID("_GlassStrength");
        private static readonly int HighlightStrengthId = Shader.PropertyToID("_HighlightStrength");
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static readonly int WaveStrengthId = Shader.PropertyToID("_WaveStrength");
        private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        private static readonly int WaveFrequencyId = Shader.PropertyToID("_WaveFrequency");
        private static readonly int SweepSpeedId = Shader.PropertyToID("_SweepSpeed");
        private static readonly int SweepWidthId = Shader.PropertyToID("_SweepWidth");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int RainbowSpeedId = Shader.PropertyToID("_RainbowSpeed");
        private static readonly int RainbowScaleId = Shader.PropertyToID("_RainbowScale");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        [Header("Target")]
        [SerializeField] private SpriteRenderer[] _targets = Array.Empty<SpriteRenderer>();
        [SerializeField] private Material _glowMaterial = null!;

        [Header("Color")]
        [SerializeField] private GlowColorMode _colorMode = GlowColorMode.Rainbow;
        [SerializeField] private Color _customColor = new(0.25f, 0.75f, 1f, 1f);
        [SerializeField] private Color _secondaryColor = Color.white;
        [Range(0f, 1f)]
        [SerializeField] private float _tintStrength = 0.82f;
        [Range(0f, 1f)]
        [SerializeField] private float _effectAmount = 0f;

        [Header("Glass")]
        [Range(0f, 5f)]
        [SerializeField] private float _glowIntensity = 1.6f;
        [Range(0f, 1f)]
        [SerializeField] private float _glassStrength = 0.75f;
        [Range(0f, 2f)]
        [SerializeField] private float _highlightStrength = 0.9f;
        [Range(0f, 2f)]
        [SerializeField] private float _rimStrength = 0.8f;

        [Header("Animation")]
        [Range(0f, 0.08f)]
        [SerializeField] private float _waveStrength = 0.012f;
        [Range(0f, 5f)]
        [SerializeField] private float _waveSpeed = 1.2f;
        [Range(1f, 60f)]
        [SerializeField] private float _waveFrequency = 18f;
        [Range(0f, 30f)]
        [SerializeField] private float _sweepSpeed = 7.5f;
        [Range(0.005f, 0.4f)]
        [SerializeField] private float _sweepWidth = 0.08f;
        [Range(0f, 8f)]
        [SerializeField] private float _pulseSpeed = 2.3f;
        [Range(0f, 2f)]
        [SerializeField] private float _rainbowSpeed = 0.16f;
        [Range(0.1f, 6f)]
        [SerializeField] private float _rainbowScale = 1.2f;
        [Range(0f, 1f)]
        [SerializeField] private float _alpha = 1f;

        private MaterialPropertyBlock _propertyBlock = null!;
        [HideInInspector]
        [SerializeField] private Material[] _normalMaterials = Array.Empty<Material>();

        public GlowColorMode ColorMode => _colorMode;
        public Color CustomColor => _customColor;
        public float EffectAmount => _effectAmount;

        private void Reset()
        {
            CollectTargets();
        }

        private void Awake()
        {
            EnsureTargets();
            EnsureNormalMaterials();
            ApplyMaterial();
            ApplyProperties();
        }

        private void OnEnable()
        {
            EnsureTargets();
            EnsureNormalMaterials();
            ApplyMaterial();
            ApplyProperties();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureTargets();
            ClampValues();
            ApplyMaterial();
            ApplyProperties();
        }
#endif

        public void SetMode(GlowColorMode mode)
        {
            _colorMode = mode;
            _effectAmount = 1f;
            ApplyMaterial();
            ApplyProperties();
        }

        public void SetModeByIndex(int modeIndex)
        {
            SetMode((GlowColorMode)Mathf.Clamp(modeIndex, (int)GlowColorMode.Custom, (int)GlowColorMode.Rainbow));
        }

        public void SetRed()
        {
            SetMode(GlowColorMode.Red);
        }

        public void SetBlue()
        {
            SetMode(GlowColorMode.Blue);
        }

        public void SetYellow()
        {
            SetMode(GlowColorMode.Yellow);
        }

        public void SetGreen()
        {
            SetMode(GlowColorMode.Green);
        }

        public void SetRainbow()
        {
            SetMode(GlowColorMode.Rainbow);
        }

        public void SetCustomColor(Color color)
        {
            _customColor = color;
            _colorMode = GlowColorMode.Custom;
            _effectAmount = 1f;
            ApplyMaterial();
            ApplyProperties();
        }

        public void SetSecondaryColor(Color color)
        {
            _secondaryColor = color;
            ApplyProperties();
        }

        public void SetGlowIntensity(float intensity)
        {
            _glowIntensity = Mathf.Max(0f, intensity);
            ApplyProperties();
        }

        public void SetEffectAmount(float amount)
        {
            _effectAmount = Mathf.Clamp01(amount);
            ApplyMaterial();
            ApplyProperties();
        }

        public void SetEffectEnabled(bool enabled)
        {
            SetEffectAmount(enabled ? 1f : 0f);
        }

        public void ShowNormal()
        {
            SetEffectAmount(0f);
        }

        public void SetWave(float strength, float speed, float frequency)
        {
            _waveStrength = Mathf.Max(0f, strength);
            _waveSpeed = Mathf.Max(0f, speed);
            _waveFrequency = Mathf.Max(1f, frequency);
            ApplyProperties();
        }

        public void SetSweep(float speed, float width)
        {
            _sweepSpeed = Mathf.Max(0f, speed);
            _sweepWidth = Mathf.Max(0.005f, width);
            ApplyProperties();
        }

        public void SetSweepSpeed(float speed)
        {
            _sweepSpeed = Mathf.Max(0f, speed);
            ApplyProperties();
        }

        public void SetRainbow(float speed, float scale)
        {
            _rainbowSpeed = Mathf.Max(0f, speed);
            _rainbowScale = Mathf.Max(0.1f, scale);
            ApplyProperties();
        }

        public void SetAlpha(float alpha)
        {
            _alpha = Mathf.Clamp01(alpha);
            ApplyProperties();
        }

        public void ApplyProperties()
        {
            EnsureTargets();
            EnsurePropertyBlock();

            foreach (var target in _targets)
            {
                if (target == null)
                    continue;

                var block = _propertyBlock;
                target.GetPropertyBlock(block);
                block.SetFloat(ColorModeId, (float)_colorMode);
                block.SetColor(TintColorId, _customColor);
                block.SetColor(SecondaryColorId, _secondaryColor);
                block.SetFloat(TintStrengthId, _tintStrength);
                block.SetFloat(EffectAmountId, _effectAmount);
                block.SetFloat(GlowIntensityId, _glowIntensity);
                block.SetFloat(GlassStrengthId, _glassStrength);
                block.SetFloat(HighlightStrengthId, _highlightStrength);
                block.SetFloat(RimStrengthId, _rimStrength);
                block.SetFloat(WaveStrengthId, _waveStrength);
                block.SetFloat(WaveSpeedId, _waveSpeed);
                block.SetFloat(WaveFrequencyId, _waveFrequency);
                block.SetFloat(SweepSpeedId, _sweepSpeed);
                block.SetFloat(SweepWidthId, _sweepWidth);
                block.SetFloat(PulseSpeedId, _pulseSpeed);
                block.SetFloat(RainbowSpeedId, _rainbowSpeed);
                block.SetFloat(RainbowScaleId, _rainbowScale);
                block.SetFloat(AlphaId, _alpha);
                target.SetPropertyBlock(block);
            }
        }

        private void ApplyMaterial()
        {
            EnsureNormalMaterials();
            var useGlow = _effectAmount > 0.0001f && _glowMaterial != null;

            for (var i = 0; i < _targets.Length; i++)
            {
                var target = _targets[i];
                if (target == null)
                    continue;

                var material = useGlow ? _glowMaterial : GetNormalMaterial(i);
                if (material != null && target.sharedMaterial != material)
                    target.sharedMaterial = material;
            }
        }

        private void CollectTargets()
        {
            _targets = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void EnsureTargets()
        {
            if (_targets == null || _targets.Length == 0)
                CollectTargets();
        }

        private void EnsureNormalMaterials()
        {
            EnsureTargets();

            if (_normalMaterials == null || _normalMaterials.Length != _targets.Length)
                _normalMaterials = new Material[_targets.Length];

            for (var i = 0; i < _targets.Length; i++)
            {
                var target = _targets[i];
                if (target != null && target.sharedMaterial != _glowMaterial)
                    _normalMaterials[i] = target.sharedMaterial;
            }
        }

        private Material GetNormalMaterial(int index)
        {
            if (_normalMaterials == null || index < 0 || index >= _normalMaterials.Length)
                return null;

            return _normalMaterials[index];
        }

        private void EnsurePropertyBlock()
        {
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
        }

        private void ClampValues()
        {
            _tintStrength = Mathf.Clamp01(_tintStrength);
            _effectAmount = Mathf.Clamp01(_effectAmount);
            _glowIntensity = Mathf.Max(0f, _glowIntensity);
            _glassStrength = Mathf.Clamp01(_glassStrength);
            _highlightStrength = Mathf.Max(0f, _highlightStrength);
            _rimStrength = Mathf.Max(0f, _rimStrength);
            _waveStrength = Mathf.Max(0f, _waveStrength);
            _waveSpeed = Mathf.Max(0f, _waveSpeed);
            _waveFrequency = Mathf.Max(1f, _waveFrequency);
            _sweepSpeed = Mathf.Max(0f, _sweepSpeed);
            _sweepWidth = Mathf.Clamp(_sweepWidth, 0.005f, 0.4f);
            _pulseSpeed = Mathf.Max(0f, _pulseSpeed);
            _rainbowSpeed = Mathf.Max(0f, _rainbowSpeed);
            _rainbowScale = Mathf.Max(0.1f, _rainbowScale);
            _alpha = Mathf.Clamp01(_alpha);
        }
    }
}
