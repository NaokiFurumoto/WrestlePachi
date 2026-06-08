using System.Threading;
using Cysharp.Threading.Tasks;

namespace App.Skills
{
    /// <summary>
    /// 保留技の実行を抽象化するインターフェース（Strategyパターン）。
    /// HoldType ごとに対応するスキルクラスを実装し TechSkillManager に登録する。
    /// 新スキルの追加は ITechSkill を実装したクラスを作るだけでよい。
    /// </summary>
    public interface ITechSkill
    {
        /// <summary>このスキルが対応する保留種別</summary>
        HoldType TargetType { get; }

        /// <summary>
        /// カットイン演出・落下ブロックが必要か。
        /// false の場合は SkillExecutingState を経由せず即時実行される。
        /// </summary>
        bool HasCutIn { get; }

        /// <summary>
        /// 技を発動できる状態か判定する。
        /// false の場合は TechSkillManager がストックに積む。
        /// </summary>
        bool CanExecute(GameContext ctx);

        /// <summary>技を発動する。演出が非同期の場合は await して完了まで待つ。</summary>
        UniTask ExecuteAsync(GameContext ctx, CancellationToken ct);
    }
}
