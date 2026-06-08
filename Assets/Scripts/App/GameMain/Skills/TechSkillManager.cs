using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Skills
{
    /// <summary>
    /// HoldType → ITechSkill のマッピングを保持し、CanExecute 判定と SkillExecutingState 起動を担うクラス。
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
        /// 指定した保留種別のスキルを起動し、完了を表す UniTask を返す。
        /// HasCutIn=true  → SkillExecutingState に遷移。スキル完了まで待機可能。
        /// HasCutIn=false → ブロックなしで即時実行。UniTask.CompletedTask を返す。
        /// CanExecute=false はストックに積み UniTask.CompletedTask を返す。
        /// </summary>
        public UniTask StartSkillAsync(HoldType holdType, GameContext ctx, SkillStockSystem stocks, CancellationToken ct)
        {
            if (!_skills.TryGetValue(holdType, out var skill))
            {
                Debug.LogWarning($"[TechSkillManager] HoldType={holdType} に対応するスキルが未登録です");
                return UniTask.CompletedTask;
            }

            if (!skill.CanExecute(ctx))
            {
                if (!stocks.TryAddStock(holdType))
                    Debug.LogWarning($"[TechSkillManager] ストックが満杯のため HoldType={holdType} を破棄");
                return UniTask.CompletedTask;
            }

            if (!skill.HasCutIn)
            {
                skill.ExecuteAsync(ctx, ct).Forget();
                return UniTask.CompletedTask;
            }

            var tcs = new UniTaskCompletionSource();
            ctx.ChangeState(new SkillExecutingState(ctx, skill, tcs));
            return tcs.Task;
        }
    }
}
