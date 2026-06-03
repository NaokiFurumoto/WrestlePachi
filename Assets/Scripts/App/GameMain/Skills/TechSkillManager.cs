using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Skills
{
    /// <summary>
    /// HoldType → ITechSkill のマッピングを保持し、技発動を各スキルへ委譲するクラス。
    /// 新スキルの追加は ITechSkill 実装クラスを作り、
    /// GameMainController の登録配列に1行加えるだけでよい（OCP 準拠）。
    /// </summary>
    public sealed class TechSkillManager
    {
        private readonly Dictionary<HoldType, ITechSkill> _skills;

        public TechSkillManager(IEnumerable<ITechSkill> skills)
        {
            _skills = skills.ToDictionary(s => s.TargetType);
        }

        /// <summary>
        /// 指定した保留種別のスキルを非同期で発動する。
        /// 未登録の HoldType は警告ログを出して何もしない。
        /// </summary>
        public async UniTask ExecuteAsync(HoldType holdType, GameContext ctx, CancellationToken ct)
        {
            if (!_skills.TryGetValue(holdType, out var skill))
            {
                Debug.LogWarning($"[TechSkillManager] HoldType={holdType} に対応するスキルが未登録です");
                return;
            }

            await skill.ExecuteAsync(ctx, ct);
        }
    }
}
