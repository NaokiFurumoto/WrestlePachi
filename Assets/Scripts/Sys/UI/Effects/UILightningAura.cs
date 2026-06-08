#nullable enable
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameSys
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/Effects/Lightning Aura")]
    public sealed class UILightningAura : MonoBehaviour
    {
        public enum AuraLayerPlacement
        {
            SiblingBehindSource = 0,
            SiblingInFrontOfSource = 1,
            ChildOverlay = 2,
        }

        private const string ShaderName = "WrestlePachi/UI/Lightning Aura";

#if UNITY_EDITOR
        private const string DefaultMaterialPath = "Assets/Materials/M_UILightningAura.mat";
#endif

        private static readonly int AuraColorId = Shader.PropertyToID("_AuraColor");
        private static readonly int LightningColorId = Shader.PropertyToID("_LightningColor");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int EffectAmountId = Shader.PropertyToID("_EffectAmount");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int SampleScaleId = Shader.PropertyToID("_SampleScale");
        private static readonly int AlphaWeightId = Shader.PropertyToID("_AlphaWeight");
        private static readonly int LuminanceWeightId = Shader.PropertyToID("_LuminanceWeight");
        private static readonly int MaskThresholdId = Shader.PropertyToID("_MaskThreshold");
        private static readonly int GlowSizeId = Shader.PropertyToID("_GlowSize");
        private static readonly int GlowPowerId = Shader.PropertyToID("_GlowPower");
        private static readonly int InsideSuppressionId = Shader.PropertyToID("_InsideSuppression");
        private static readonly int LightningDensityId = Shader.PropertyToID("_LightningDensity");
        private static readonly int LightningWidthId = Shader.PropertyToID("_LightningWidth");
        private static readonly int LightningSpeedId = Shader.PropertyToID("_LightningSpeed");
        private static readonly int SparkDensityId = Shader.PropertyToID("_SparkDensity");
        private static readonly int SparkSpeedId = Shader.PropertyToID("_SparkSpeed");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");

        [Header("Target")]
        [SerializeField] private Graphic? _sourceGraphic;
        [SerializeField] private Material? _auraMaterial;
        [SerializeField] private AuraLayerPlacement _placement = AuraLayerPlacement.SiblingBehindSource;
        [SerializeField] private bool _autoCreateLayer = true;
        [SerializeField] private bool _syncEveryFrame = true;
        [SerializeField] private bool _raycastTarget = false;
        [Range(1f, 1.8f)]
        [SerializeField] private float _paddingScale = 1.22f;

        [Header("Mask")]
        [Range(0f, 1f)]
        [SerializeField] private float _alphaWeight = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _luminanceWeight = 0.35f;
        [Range(0f, 0.5f)]
        [SerializeField] private float _maskThreshold = 0.055f;

        [Header("Aura")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _auraColor = new(1f, 0.45f, 0.02f, 1f);
        [ColorUsage(false, true)]
        [SerializeField] private Color _lightningColor = new(1f, 0.95f, 0.45f, 1f);
        [ColorUsage(false, true)]
        [SerializeField] private Color _coreColor = new(0.18f, 0.75f, 1f, 1f);
        [Range(0f, 1f)]
        [SerializeField] private float _effectAmount = 1f;
        [Range(0f, 10f)]
        [SerializeField] private float _intensity = 3.2f;
        [Range(0.001f, 0.08f)]
        [SerializeField] private float _glowSize = 0.024f;
        [Range(0.1f, 6f)]
        [SerializeField] private float _glowPower = 1.45f;
        [Range(0f, 1f)]
        [SerializeField] private float _insideSuppression = 0.86f;

        [Header("Lightning")]
        [Range(2f, 28f)]
        [SerializeField] private float _lightningDensity = 11f;
        [Range(0.001f, 0.12f)]
        [SerializeField] private float _lightningWidth = 0.026f;
        [Range(0f, 12f)]
        [SerializeField] private float _lightningSpeed = 3.6f;

        [Header("Sparks")]
        [Range(5f, 90f)]
        [SerializeField] private float _sparkDensity = 36f;
        [Range(0f, 12f)]
        [SerializeField] private float _sparkSpeed = 4.4f;
        [Range(0f, 12f)]
        [SerializeField] private float _pulseSpeed = 4.8f;

        [SerializeField, HideInInspector] private Graphic? _auraGraphic;
        [SerializeField, HideInInspector] private bool _ownsAuraLayer;

        private Material? _runtimeMaterial;
        private Material? _runtimeTemplate;

        public Graphic? SourceGraphic => _sourceGraphic;
        public Graphic? AuraGraphic => _auraGraphic;
        public float EffectAmount => _effectAmount;

        private void Reset()
        {
            _sourceGraphic = GetComponent<Graphic>();
            LoadDefaultMaterialInEditor();
            EnsureReady();
        }

        private void OnEnable()
        {
            EnsureReady();

            if (_auraGraphic != null)
                _auraGraphic.enabled = _effectAmount > 0.0001f;
        }

        private void OnDisable()
        {
            if (_auraGraphic != null)
                _auraGraphic.enabled = false;
        }

        private void OnDestroy()
        {
            DestroyRuntimeMaterial();
        }

        private void LateUpdate()
        {
            EnsureReady();

            if (_syncEveryFrame)
            {
                SyncLayerTransform();
                SyncGraphicSource();
            }

            ApplyProperties();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampValues();
            LoadDefaultMaterialInEditor();

            if (!isActiveAndEnabled)
                return;

            EnsureReady();
        }
#endif

        public void SetEffectAmount(float amount)
        {
            _effectAmount = Mathf.Clamp01(amount);
            ApplyProperties();

            if (_auraGraphic != null)
                _auraGraphic.enabled = _effectAmount > 0.0001f;
        }

        public void SetEffectEnabled(bool enabled)
        {
            SetEffectAmount(enabled ? 1f : 0f);
        }

        public void SetColors(Color auraColor, Color lightningColor, Color coreColor)
        {
            _auraColor = auraColor;
            _lightningColor = lightningColor;
            _coreColor = coreColor;
            ApplyProperties();
        }

        public void SetIntensity(float intensity)
        {
            _intensity = Mathf.Max(0f, intensity);
            ApplyProperties();
        }

        public void SetMaskWeights(float alphaWeight, float luminanceWeight)
        {
            _alphaWeight = Mathf.Clamp01(alphaWeight);
            _luminanceWeight = Mathf.Clamp01(luminanceWeight);
            ApplyProperties();
        }

        public void Refresh()
        {
            EnsureReady();
            SyncLayerTransform();
            SyncGraphicSource();
            ApplyProperties();
        }

        private void EnsureReady()
        {
            EnsureSource();

            if (_sourceGraphic == null)
                return;

            LoadDefaultMaterialInEditor();
            EnsureLayer();
            EnsureRuntimeMaterial();
            SyncLayerTransform();
            SyncGraphicSource();
            ApplyProperties();
        }

        private void EnsureSource()
        {
            if (_sourceGraphic == null)
                _sourceGraphic = GetComponent<Graphic>();
        }

        private void EnsureLayer()
        {
            if (!_autoCreateLayer || _sourceGraphic == null)
                return;

            if (_auraGraphic != null && WantsRawImage() != (_auraGraphic is RawImage))
            {
                if (_ownsAuraLayer)
                    DestroyObject(_auraGraphic.gameObject);

                _auraGraphic = null;
                _ownsAuraLayer = false;
            }

            if (_auraGraphic == null)
                _auraGraphic = FindExistingLayer();

            if (_auraGraphic == null)
                CreateLayer();

            if (_auraGraphic == null)
                return;

            _auraGraphic.gameObject.layer = gameObject.layer;
            _auraGraphic.raycastTarget = _raycastTarget;
            SyncMaskable();
            ParentLayer();
        }

        private bool WantsRawImage()
        {
            return _sourceGraphic is RawImage;
        }

        private Graphic? FindExistingLayer()
        {
            var layerName = GetLayerName();
            var root = GetLayerParent();

            if (root == null)
                return null;

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name != layerName)
                    continue;

                if (child.TryGetComponent<Graphic>(out var graphic))
                {
                    _ownsAuraLayer = true;
                    return graphic;
                }
            }

            return null;
        }

        private void CreateLayer()
        {
            var layerObject = new GameObject(GetLayerName(), typeof(RectTransform), typeof(CanvasRenderer));
            layerObject.layer = gameObject.layer;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCreatedObjectUndo(layerObject, "Create UI Lightning Aura Layer");
#endif

            _auraGraphic = WantsRawImage()
                ? layerObject.AddComponent<RawImage>()
                : layerObject.AddComponent<Image>();
            _ownsAuraLayer = true;
        }

        private Transform? GetLayerParent()
        {
            if (_sourceGraphic == null)
                return null;

            if (_placement == AuraLayerPlacement.ChildOverlay || _sourceGraphic.transform.parent == null)
                return _sourceGraphic.transform;

            return _sourceGraphic.transform.parent;
        }

        private string GetLayerName()
        {
            return $"{gameObject.name}_LightningAura";
        }

        private void ParentLayer()
        {
            if (_auraGraphic == null)
                return;

            var parent = GetLayerParent();
            if (parent == null)
                return;

            var auraTransform = _auraGraphic.rectTransform;
            if (auraTransform.parent != parent)
                auraTransform.SetParent(parent, false);

            ApplyLayerOrder();
        }

        private void ApplyLayerOrder()
        {
            if (_sourceGraphic == null || _auraGraphic == null)
                return;

            var auraTransform = _auraGraphic.transform;
            var sourceTransform = _sourceGraphic.transform;

            if (_placement == AuraLayerPlacement.ChildOverlay || auraTransform.parent != sourceTransform.parent)
            {
                auraTransform.SetAsFirstSibling();
                return;
            }

            var sourceIndex = sourceTransform.GetSiblingIndex();
            var auraIndex = auraTransform.GetSiblingIndex();

            if (_placement == AuraLayerPlacement.SiblingBehindSource)
            {
                var targetIndex = auraIndex < sourceIndex ? sourceIndex - 1 : sourceIndex;
                auraTransform.SetSiblingIndex(Mathf.Max(0, targetIndex));
            }
            else
            {
                var targetIndex = auraIndex < sourceIndex ? sourceIndex : sourceIndex + 1;
                auraTransform.SetSiblingIndex(targetIndex);
            }
        }

        private void SyncLayerTransform()
        {
            if (_sourceGraphic == null || _auraGraphic == null)
                return;

            var sourceRect = _sourceGraphic.rectTransform;
            var auraRect = _auraGraphic.rectTransform;

            if (_placement == AuraLayerPlacement.ChildOverlay || auraRect.parent == sourceRect)
            {
                auraRect.anchorMin = new Vector2(0.5f, 0.5f);
                auraRect.anchorMax = new Vector2(0.5f, 0.5f);
                auraRect.pivot = new Vector2(0.5f, 0.5f);
                auraRect.anchoredPosition = Vector2.zero;
                auraRect.localRotation = Quaternion.identity;
                auraRect.localScale = Vector3.one;
                auraRect.sizeDelta = sourceRect.rect.size * _paddingScale;
                return;
            }

            auraRect.anchorMin = sourceRect.anchorMin;
            auraRect.anchorMax = sourceRect.anchorMax;
            auraRect.pivot = sourceRect.pivot;
            auraRect.anchoredPosition = sourceRect.anchoredPosition;
            auraRect.localRotation = sourceRect.localRotation;
            auraRect.localScale = sourceRect.localScale;
            auraRect.sizeDelta = sourceRect.sizeDelta * _paddingScale;
        }

        private void SyncGraphicSource()
        {
            if (_sourceGraphic == null || _auraGraphic == null)
                return;

            _auraGraphic.color = Color.white;
            _auraGraphic.raycastTarget = _raycastTarget;
            SyncMaskable();

            if (_sourceGraphic is RawImage sourceRaw && _auraGraphic is RawImage auraRaw)
            {
                auraRaw.texture = sourceRaw.texture;
                auraRaw.uvRect = sourceRaw.uvRect;
            }
            else if (_sourceGraphic is Image sourceImage && _auraGraphic is Image auraImage)
            {
                auraImage.sprite = sourceImage.sprite;
                auraImage.overrideSprite = sourceImage.overrideSprite;
                auraImage.type = Image.Type.Simple;
                auraImage.preserveAspect = sourceImage.preserveAspect;
                auraImage.fillCenter = true;
                auraImage.useSpriteMesh = sourceImage.useSpriteMesh;
                auraImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
            }

            if (_runtimeMaterial != null)
                _auraGraphic.material = _runtimeMaterial;

            _auraGraphic.enabled = _effectAmount > 0.0001f && _sourceGraphic.enabled;
        }

        private void SyncMaskable()
        {
            if (_sourceGraphic == null || _auraGraphic == null)
                return;

            if (_auraGraphic is not MaskableGraphic auraMaskable)
                return;

            auraMaskable.maskable = _sourceGraphic is MaskableGraphic sourceMaskable
                ? sourceMaskable.maskable
                : auraMaskable.maskable;
        }

        private void EnsureRuntimeMaterial()
        {
            Material? template = _auraMaterial;

            if (template == null)
            {
                var shader = Shader.Find(ShaderName);
                if (shader == null)
                    return;

                if (_runtimeMaterial != null && _runtimeTemplate == null && _runtimeMaterial.shader == shader)
                    return;

                DestroyRuntimeMaterial();
                _runtimeMaterial = new Material(shader)
                {
                    name = $"{ShaderName} ({gameObject.name})",
                    hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                };
                _runtimeTemplate = null;
                return;
            }

            if (_runtimeMaterial != null && _runtimeTemplate == template)
                return;

            DestroyRuntimeMaterial();
            _runtimeMaterial = new Material(template)
            {
                name = $"{template.name} ({gameObject.name})",
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
            _runtimeTemplate = template;
        }

        private void ApplyProperties()
        {
            if (_runtimeMaterial == null)
                return;

            ClampValues();

            _runtimeMaterial.SetColor(AuraColorId, _auraColor);
            _runtimeMaterial.SetColor(LightningColorId, _lightningColor);
            _runtimeMaterial.SetColor(CoreColorId, _coreColor);
            _runtimeMaterial.SetFloat(EffectAmountId, _effectAmount);
            _runtimeMaterial.SetFloat(IntensityId, _intensity);
            _runtimeMaterial.SetFloat(SampleScaleId, _paddingScale);
            _runtimeMaterial.SetFloat(AlphaWeightId, _alphaWeight);
            _runtimeMaterial.SetFloat(LuminanceWeightId, _luminanceWeight);
            _runtimeMaterial.SetFloat(MaskThresholdId, _maskThreshold);
            _runtimeMaterial.SetFloat(GlowSizeId, _glowSize);
            _runtimeMaterial.SetFloat(GlowPowerId, _glowPower);
            _runtimeMaterial.SetFloat(InsideSuppressionId, _insideSuppression);
            _runtimeMaterial.SetFloat(LightningDensityId, _lightningDensity);
            _runtimeMaterial.SetFloat(LightningWidthId, _lightningWidth);
            _runtimeMaterial.SetFloat(LightningSpeedId, _lightningSpeed);
            _runtimeMaterial.SetFloat(SparkDensityId, _sparkDensity);
            _runtimeMaterial.SetFloat(SparkSpeedId, _sparkSpeed);
            _runtimeMaterial.SetFloat(PulseSpeedId, _pulseSpeed);
        }

        private void ClampValues()
        {
            _paddingScale = Mathf.Clamp(_paddingScale, 1f, 1.8f);
            _alphaWeight = Mathf.Clamp01(_alphaWeight);
            _luminanceWeight = Mathf.Clamp01(_luminanceWeight);
            _maskThreshold = Mathf.Clamp(_maskThreshold, 0f, 0.5f);
            _effectAmount = Mathf.Clamp01(_effectAmount);
            _intensity = Mathf.Clamp(_intensity, 0f, 10f);
            _glowSize = Mathf.Clamp(_glowSize, 0.001f, 0.08f);
            _glowPower = Mathf.Clamp(_glowPower, 0.1f, 6f);
            _insideSuppression = Mathf.Clamp01(_insideSuppression);
            _lightningDensity = Mathf.Clamp(_lightningDensity, 2f, 28f);
            _lightningWidth = Mathf.Clamp(_lightningWidth, 0.001f, 0.12f);
            _lightningSpeed = Mathf.Clamp(_lightningSpeed, 0f, 12f);
            _sparkDensity = Mathf.Clamp(_sparkDensity, 5f, 90f);
            _sparkSpeed = Mathf.Clamp(_sparkSpeed, 0f, 12f);
            _pulseSpeed = Mathf.Clamp(_pulseSpeed, 0f, 12f);
        }

        private void DestroyRuntimeMaterial()
        {
            if (_runtimeMaterial == null)
                return;

            DestroyObject(_runtimeMaterial);
            _runtimeMaterial = null;
            _runtimeTemplate = null;
        }

        private static void DestroyObject(Object target)
        {
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private void LoadDefaultMaterialInEditor()
        {
#if UNITY_EDITOR
            if (_auraMaterial == null)
                _auraMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
#endif
        }
    }
}

#nullable disable
