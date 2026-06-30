#nullable enable
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ステージ詳細パネル（動的 View）。
    /// ノードタップ時に PushView で表示し、挑戦ボタンでゲームシーンへ遷移する。
    /// Prefab: Resources/Prefabs/Views/StageDetailPanel
    /// </summary>
    public sealed class StageDetailPanel : ViewBase
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
        [SerializeField] private TMP_Text? _hpText;
        [SerializeField] private TMP_Text? _timeLimitText;

        [Header("スタミナ")]
        [SerializeField] private TMP_Text? _staminaText;

        [Header("ボタン")]
        [SerializeField] private Button? _btnChallenge;
        [SerializeField] private Button? _btnClose;
        [SerializeField] private Button? _btnSkillDetail;

        [Header("スキル詳細グループ（タップで開閉）")]
        [SerializeField] private GameObject? _skillDetailGroup;
        [SerializeField] private TMP_Text?   _skillListText;

        // ─── 初期化 ──────────────────────────────────────────────
        protected override void OnInitialize()
        {
            if (Data is not StageDetailData data || data.Config == null) return;

            var config = data.Config;

            if (_enemyImage != null)
                _enemyImage.sprite = config.EnemySprite;

            if (_stageNameText != null)
                _stageNameText.text = $"STAGE {data.StageIndex + 1}";

            if (_enemyNameText != null)
                _enemyNameText.text = config.EnemyName;

            if (_hpText != null)
                _hpText.text = config.EnemyHp.ToString();

            if (_timeLimitText != null)
                _timeLimitText.text = $"{config.TimeLimit:0}s";

            if (_staminaText != null && StaminaManager.isValid)
                _staminaText.text = $"{StaminaManager.Instance.Current}/{StaminaManager.Instance.Max}";

            // スキル詳細ボタン：スキルがなければ非活性
            var hasSkills = config.EnemySkills is { Length: > 0 };
            if (_btnSkillDetail != null)
            {
                _btnSkillDetail.interactable = hasSkills;
                _btnSkillDetail.onClick.AddListener(OnSkillDetailClicked);
            }

            // スキル一覧テキストを事前生成
            if (_skillListText != null && hasSkills)
                _skillListText.text = BuildSkillListText(config.EnemySkills);

            // スキル詳細グループは最初は非表示
            _skillDetailGroup?.SetActive(false);

            if (_btnChallenge != null)
            {
                _btnChallenge.interactable = StaminaManager.isValid && StaminaManager.Instance.Current >= 1;
                _btnChallenge.onClick.AddListener(OnChallengeClicked);
            }

            if (_btnClose != null)
                _btnClose.onClick.AddListener(OnCloseClicked);
        }

        // ─── スキル一覧テキスト生成 ───────────────────────────────
        private static string BuildSkillListText(EnemySkillType[] skills)
        {
            var sb = new StringBuilder();
            foreach (var skill in skills)
            {
                sb.AppendLine(GetSkillLabel(skill));
            }
            return sb.ToString().TrimEnd();
        }

        private static string GetSkillLabel(EnemySkillType skill) => skill switch
        {
            EnemySkillType.Shark               => "サメマスク：一定時間ごとにぷよを消去してHP回復",
            EnemySkillType.FireTile            => "炎マスク：通過したぷよが消滅",
            EnemySkillType.Pineapple           => "パイナップル：2回消さないと消えない",
            EnemySkillType.Skull               => "どくろ：一定時間後にぷよを別色に変色",
            EnemySkillType.IcePuyo             => "氷ぷよ：隣接消去で解凍されるまで消せない",
            EnemySkillType.Magnet              => "磁石マスク：落下ぷよが一定列に引き寄せられる",
            EnemySkillType.Zombie              => "ゾンビ：消えたぷよがおじゃまとして復活",
            EnemySkillType.Multiply            => "増殖：おじゃまぷよが消されると2個に分裂",
            EnemySkillType.Clock               => "時計マスク：定期的に制限時間を削る",
            EnemySkillType.Mirror              => "鏡マスク：一定時間、左右操作が反転",
            EnemySkillType.NullifyDropkick     => "ドロップキック無効：保留消化を封じる",
            EnemySkillType.NullifyAllSkills    => "全スキル無効：プレイヤーの全スキルを封じる",
            EnemySkillType.NullifySkillByColor => "単色スキル無効：特定色のスキルを封じる",
            EnemySkillType.SealHold            => "保留封印：新しい保留が出現しなくなる",
            EnemySkillType.ChainCap            => "チェーン上限：一定連鎖以上の追加ダメージが0",
            _                                  => skill.ToString(),
        };

        // ─── ボタンハンドラ ───────────────────────────────────────
        public void OnSkillDetailClicked()
        {
            if (_skillDetailGroup == null) return;
            _skillDetailGroup.SetActive(!_skillDetailGroup.activeSelf);
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
