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
                        // 常に効いている技は、付いた時点で1回だけ予告を出す
                        ctx.ShowEnemySkillWarning?.Invoke(passive.Type);
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

#if UNITY_EDITOR
        // ─── デバッグウィンドウ用（エディタ専用） ────────────────────

        /// <summary>発動中の全スキル（周期発動型 → 常時効果型の順）</summary>
        public IReadOnlyList<IEnemySkill> Debug_Skills
        {
            get
            {
                var list = new List<IEnemySkill>(_periodicSkills);
                list.AddRange(_passiveSkills);
                return list;
            }
        }

        /// <summary>
        /// 技をその場で追加する。周期発動型は追加と同時に1回発動し、常時効果型は追加時点で効果が始まる。
        /// ステージ開始前（Initialize 前）は何もしない。
        /// </summary>
        public void Debug_AddAndTrigger(EnemySkillType type, int level)
        {
            if (_ctx == null) return;
            switch (EnemySkillFactory.Create(type, level, _assets, _balance))
            {
                case EnemySkillBase periodic:
                    _periodicSkills.Add(periodic);
                    periodic.OnEnter(_ctx);
                    _ctx.ShowEnemySkillWarning?.Invoke(periodic.Type);
                    periodic.TriggerNow(_ctx);
                    break;
                case EnemyPassiveSkillBase passive:
                    _passiveSkills.Add(passive);
                    passive.OnEnter(_ctx);
                    _ctx.ShowEnemySkillWarning?.Invoke(passive.Type);
                    break;
            }
        }

        /// <summary>周期発動型の技を今すぐ発動する</summary>
        public void Debug_TriggerNow(EnemySkillBase skill)
        {
            if (_ctx != null) skill.TriggerNow(_ctx);
        }

        /// <summary>技を1つ外す（通常の終了時と同じ後始末をする）</summary>
        public void Debug_Remove(IEnemySkill skill)
        {
            if (_ctx == null) return;
            switch (skill)
            {
                case EnemySkillBase periodic when _periodicSkills.Remove(periodic):
                    periodic.OnRelease(_ctx);
                    break;
                case EnemyPassiveSkillBase passive when _passiveSkills.Remove(passive):
                    passive.OnRelease(_ctx);
                    break;
            }
        }

        /// <summary>全ての技を外す。ステージは続くので、その後も Debug_AddAndTrigger で追加できる</summary>
        public void Debug_RemoveAll()
        {
            foreach (var skill in Debug_Skills) Debug_Remove(skill);
        }
#endif

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
