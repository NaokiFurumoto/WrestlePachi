#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace App
{
    /// <summary>
    /// ステージに設定された敵スキルをまとめて管理する。
    /// 周期発動型（EnemySkillBase）は毎フレーム Tick し、
    /// 常時効果型（EnemyPassiveSkillBase）は OnEnter/OnRelease で状態を切り替える。
    /// GameMainController から Initialize → Tick(毎フレーム) → Release の順で呼ぶ。
    /// </summary>
    public sealed class EnemySkillManager
    {
        private readonly List<EnemySkillBase>        _periodicSkills = new();
        private readonly List<EnemyPassiveSkillBase> _passiveSkills  = new();
        private readonly EnemySkillAssets            _assets;
        private readonly EnemySkillBalanceConfig     _balance;

        private GameContext? _ctx;

        /// <summary>
        /// assets / balance が未アサイン（null）の場合は、それぞれ空の設定を生成してフォールバックする。
        /// </summary>
        public EnemySkillManager(EnemySkillAssets? assets, EnemySkillBalanceConfig? balance)
        {
            _assets  = assets  != null ? assets  : ScriptableObject.CreateInstance<EnemySkillAssets>();
            _balance = balance != null ? balance : ScriptableObject.CreateInstance<EnemySkillBalanceConfig>();
        }

        /// <summary>
        /// 指定エントリからスキルを生成し、OnEnter を呼ぶ。
        /// ステージ開始・敵切替のたびに呼ぶ（切替時は事前に Release() で前ステージ分を解除すること）。
        /// </summary>
        public void Initialize(GameContext ctx, IReadOnlyList<EnemySkillEntry> entries)
        {
            _ctx = ctx;
            foreach (var entry in entries)
            {
                switch (EnemySkillFactory.Create(entry.Type, entry.Level, _assets, _balance))
                {
                    case EnemySkillBase periodic:
                        _periodicSkills.Add(periodic);
                        periodic.OnEnter(ctx);
                        break;
                    case EnemyPassiveSkillBase passive:
                        _passiveSkills.Add(passive);
                        passive.OnEnter(ctx);
                        break;
                }
            }
        }

        /// <summary>周期発動型スキルの Tick を進める。GameMainController.Update から毎フレーム呼ぶ</summary>
        public void Tick(float deltaTime)
        {
            if (_ctx == null) return;
            foreach (var skill in _periodicSkills)
                skill.Tick(deltaTime, _ctx);
        }

        /// <summary>全スキルを解除する。ステージ終了・敵切替時に呼ぶ</summary>
        public void Release()
        {
            if (_ctx != null)
            {
                foreach (var skill in _periodicSkills) skill.OnRelease(_ctx);
                foreach (var skill in _passiveSkills)  skill.OnRelease(_ctx);
            }
            _periodicSkills.Clear();
            _passiveSkills.Clear();
            _ctx = null;
        }
    }
}
#nullable disable
