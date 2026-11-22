#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 敵キャラクター1体分の定義（名前・顔・得意技）。
    /// Assets/Settings/Enemies/ に1体1アセットで置き、StageConfig.Enemy から参照する。
    /// 同じ敵が2ステージ連続で出る場合、2戦目の技は EnemySkillRule が
    /// 「得意技の Lv+1 ＋ 2戦目の追加技」として自動で決める。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "WrestlePachi/EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        [Tooltip("表示名（例：クロマスク）")]
        [SerializeField] private string _displayName = "";

        [Tooltip("顔画像。未作成の敵は空のままでよい")]
        [SerializeField] private Sprite? _faceSprite;

        [Tooltip("1戦目（または1回だけ出るボス）で使う技とLv。空なら技なし")]
        [SerializeField] private EnemySkillEntry[] _signatureSkills = Array.Empty<EnemySkillEntry>();

        [Tooltip("2戦目で追加される技とLv。空なら追加なし")]
        [SerializeField] private EnemySkillEntry[] _rematchExtraSkills = Array.Empty<EnemySkillEntry>();

        /// <summary>表示名</summary>
        public string DisplayName => _displayName;

        /// <summary>顔画像（未設定なら null）</summary>
        public Sprite? FaceSprite => _faceSprite;

        /// <summary>1戦目の技（得意技）</summary>
        public IReadOnlyList<EnemySkillEntry> SignatureSkills => _signatureSkills;

        /// <summary>2戦目で追加される技</summary>
        public IReadOnlyList<EnemySkillEntry> RematchExtraSkills => _rematchExtraSkills;

#if UNITY_EDITOR
        /// <summary>
        /// エディタ専用：中身をまとめて設定する（移行メニュー・テスト用）。
        /// 呼び出し側で EditorUtility.SetDirty すること。
        /// </summary>
        public void Configure(string displayName, Sprite? faceSprite,
                              EnemySkillEntry[] signatureSkills, EnemySkillEntry[] rematchExtraSkills)
        {
            _displayName        = displayName;
            _faceSprite         = faceSprite;
            _signatureSkills    = signatureSkills;
            _rematchExtraSkills = rematchExtraSkills;
        }
#endif
    }
}
#nullable disable
