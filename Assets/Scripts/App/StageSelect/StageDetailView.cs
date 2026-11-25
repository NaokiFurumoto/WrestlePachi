#nullable enable
using System;
using Cysharp.Threading.Tasks;
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ステージ詳細画面（動的 View）。
    /// ノードタップ時に PushView で表示し、挑戦ボタンでゲームシーンへ遷移する。
    /// Prefab: Resources/Prefabs/Views/StageDetailView
    /// </summary>
    public sealed class StageDetailView : ViewBase
    {
        // ─── ViewData ────────────────────────────────────────────
        public sealed class StageDetailData : ViewData
        {
            public int          StageIndex  { get; set; }
            public StageConfig? Config      { get; set; }
            public Action?      OnChallenge { get; set; }
            public Action?      OnClosed    { get; set; }
        }

        // ─── Inspector フィールド ─────────────────────────────────
        [Header("表示")]
        [SerializeField] private Image?    _enemyImage;
        [SerializeField] private TMP_Text? _stageNameText;
        [SerializeField] private TMP_Text? _enemyNameText;
        [SerializeField] private TMP_Text? _enemyLevelText;
        [SerializeField] private TMP_Text? _hpText;
        [SerializeField] private TMP_Text? _timeLimitText;

        [Header("初期ゴミ行数（0なら枠ごと非表示）")]
        [SerializeField] private GameObject? _garbageRowsGroup;
        [SerializeField] private TMP_Text?   _garbageRowsText;

        [Header("スタミナ")]
        [SerializeField] private TMP_Text?  _staminaText;
        [SerializeField] private Transform? _staminaHeartContainer;
        [SerializeField] private Image?     _staminaHeartTemplate;
        [SerializeField] private Sprite?    _heartFullSprite;
        [SerializeField] private Sprite?    _heartEmptySprite;

        [Header("ボタン")]
        [SerializeField] private Button?   _btnChallenge;
        [SerializeField] private Sprite?   _challengeDisabledSprite;
        [SerializeField] private Button?   _btnClose;
        [SerializeField] private Button?   _btnSkillDetail;
        [SerializeField] private TMP_Text? _skillToggleArrow;

        [Header("スキル詳細グループ（タップで開閉・ScrollView）")]
        [SerializeField] private GameObject? _skillDetailGroup;
        [SerializeField] private Transform?  _skillNodeContainer;

        [Header("アセット参照")]
        [SerializeField] private EnemySkillAssets? _skillAssets;

        // ─── 初期化 ──────────────────────────────────────────────
        protected override void OnInitialize()
        {
            if (Data is not StageDetailData data || data.Config == null) return;

            var config = data.Config;

            if (_enemyImage != null)
                _enemyImage.sprite = config.EnemySprite;

            // 背景フレーム画像側に「STAGE」の見出しが描き込まれているため、ここでは番号のみ表示する
            if (_stageNameText != null)
                _stageNameText.text = $"{data.StageIndex + 1}";

            if (_enemyNameText != null)
                _enemyNameText.text = config.EnemyName;

            if (_enemyLevelText != null)
                _enemyLevelText.text = $"Lv.{config.EnemyLevel}";

            if (_hpText != null)
                _hpText.text = config.EnemyHp.ToString();

            if (_timeLimitText != null)
                _timeLimitText.text = $"{config.TimeLimit:0}{UnitSuffix("秒")}";

            // 初期ゴミ行数は 0 なら枠ごと非表示
            var hasGarbage = config.GarbageInitialRows > 0;
            _garbageRowsGroup?.SetActive(hasGarbage);
            if (_garbageRowsText != null && hasGarbage)
                _garbageRowsText.text = $"{config.GarbageInitialRows}{UnitSuffix("行")}";

            SetupStamina();

            // スキル詳細ボタン：スキルがなければ非活性
            var hasSkills = config.EnemySkills is { Length: > 0 };
            if (_btnSkillDetail != null)
            {
                _btnSkillDetail.interactable = hasSkills;
                _btnSkillDetail.onClick.AddListener(OnSkillDetailClicked);
            }

            if (hasSkills)
                BuildSkillNodes(config.EnemySkills);

            // スキル詳細グループは最初は非表示
            _skillDetailGroup?.SetActive(false);

            if (_btnChallenge != null)
            {
                var canChallenge = StaminaManager.isValid && StaminaManager.Instance.Current >= 1;
                _btnChallenge.interactable = canChallenge;
                if (!canChallenge && _challengeDisabledSprite != null && _btnChallenge.image != null)
                    _btnChallenge.image.sprite = _challengeDisabledSprite;
                _btnChallenge.onClick.AddListener(OnChallengeClicked);
            }

            if (_btnClose != null)
                _btnClose.onClick.AddListener(OnCloseClicked);
        }

        /// <summary>数値の後ろに付ける単位（「秒」「行」）を小さめに表示するリッチテキスト。</summary>
        private static string UnitSuffix(string unit) => $"<size=50%>{unit}</size>";

        // ─── スタミナ表示 ───────────────────────────────────────────

        /// <summary>最大値の数だけハートを並べ、現在値ぶんを赤、残りを黒で表示する。</summary>
        private void SetupStamina()
        {
            if (!StaminaManager.isValid) return;

            var current = StaminaManager.Instance.Current;
            var max     = StaminaManager.Instance.Max;

            if (_staminaText != null)
                _staminaText.text = $"{current}<size=60%> / </size>{max}";

            if (_staminaHeartContainer == null || _staminaHeartTemplate == null) return;

            _staminaHeartTemplate.gameObject.SetActive(false);
            for (var i = 0; i < max; i++)
            {
                var heart = Instantiate(_staminaHeartTemplate, _staminaHeartContainer);
                heart.sprite = i < current ? _heartFullSprite : _heartEmptySprite;
                heart.gameObject.SetActive(true);
            }
        }

        // ─── スキル一覧ノード生成 ───────────────────────────────────

        /// <summary>スキル1件につき SkillEntryNode を1つ ScrollView の Content に生成する。</summary>
        private void BuildSkillNodes(EnemySkillEntry[] skills)
        {
            var prefab = Resources.Load<GameObject>(ViewKeys.SKILL_ENTRY_NODE);
            if (prefab == null || _skillNodeContainer == null) return;

            foreach (var skill in skills)
            {
                var go   = Instantiate(prefab, _skillNodeContainer);
                var node = go.GetComponent<SkillEntryNode>();
                if (node == null) continue;

                var title = $"{EnemySkillNames.Get(skill.Type)}（Lv.{skill.Level}）";
                var icon  = _skillAssets?.GetIcon(skill.Type);
                node.Setup(icon, title, GetSkillColor(skill.Type), GetSkillDescription(skill.Type));
            }
        }

        private static string GetSkillDescription(EnemySkillType skill) => skill switch
        {
            EnemySkillType.Shark          => "一定間隔でぷよを消去し、消した数に応じてHPを回復する。",
            EnemySkillType.FireTile       => "指定マスに炎を出現させ、通過したぷよを消滅させる。",
            EnemySkillType.GarbageDrop    => "いちばん高い列を狙い、拳でお邪魔ぷよを積み上げる。",
            EnemySkillType.Rock           => "ぷよを岩に変え、2回消去に巻き込まないと消えなくする。",
            EnemySkillType.TimeBomb       => "盤面に爆弾ぷよを設置する。放置すると周囲を巻き込んで爆発する。",
            EnemySkillType.SkillSeal      => "一定間隔で、次に出てくる保留を鎖でロックして不発にする。",
            EnemySkillType.Confusion      => "一定時間、左右の操作を反転させる。",
            EnemySkillType.BlackHoldBoost => "黒保留（お邪魔落下の特殊保留）の出現率を上げる。",
            EnemySkillType.BlackHoldPower => "黒保留消化時に降るお邪魔ぷよの数を増やす。",
            EnemySkillType.Clock          => "一定間隔で残り時間を削る。",
            _                             => "",
        };

        private static Color GetSkillColor(EnemySkillType skill) => skill switch
        {
            EnemySkillType.Shark          => new Color(0.30f, 0.70f, 1.00f), // 青
            EnemySkillType.FireTile       => new Color(1.00f, 0.45f, 0.20f), // オレンジ
            EnemySkillType.GarbageDrop    => new Color(0.65f, 0.45f, 1.00f), // 紫
            EnemySkillType.Rock           => new Color(0.60f, 0.55f, 0.45f), // 岩の茶グレー
            EnemySkillType.TimeBomb       => new Color(1.00f, 0.30f, 0.30f), // 赤
            EnemySkillType.SkillSeal      => new Color(0.65f, 0.65f, 0.70f), // グレー
            EnemySkillType.Confusion      => new Color(0.85f, 0.35f, 0.90f), // マゼンタ
            EnemySkillType.BlackHoldBoost => new Color(0.45f, 0.45f, 0.70f), // 紺
            EnemySkillType.BlackHoldPower => new Color(0.35f, 0.35f, 0.55f), // 濃紺
            EnemySkillType.Clock          => new Color(0.25f, 0.80f, 0.80f), // シアン
            _                             => Color.white,
        };

        // ─── ボタンハンドラ ───────────────────────────────────────
        public void OnSkillDetailClicked()
        {
            if (_skillDetailGroup == null) return;
            var isOpen = !_skillDetailGroup.activeSelf;
            _skillDetailGroup.SetActive(isOpen);
            if (_skillToggleArrow != null)
                _skillToggleArrow.text = isOpen ? "▲" : "▼";
        }

        public void OnChallengeClicked()
        {
            if (Data is not StageDetailData data) return;
            var callback = data.OnChallenge;
            PopAsync().ContinueWith(callback).Forget();
        }

        public void OnCloseClicked()
        {
            if (Data is StageDetailData data)
                data.OnClosed?.Invoke();
            PopAsync().Forget();
        }
    }
}
#nullable disable
