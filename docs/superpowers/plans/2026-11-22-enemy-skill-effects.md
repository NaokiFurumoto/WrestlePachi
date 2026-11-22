# 敵スキル演出（予告＋サメ）実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.

**Goal:** 全敵スキル共通の予告アイコンと、サメの「横切ってパクッ」演出を入れる。
**Architecture:** スキルは `GameContext.ShowEnemySkillWarning` を呼ぶだけ。見た目は HUD（予告・回復表示）と `SharkEffect`（盤面上の演出）が担当。
**Tech Stack:** Unity 6 / C# / UniTask / DOTween / NUnit(EditMode, Assembly-CSharp-Editor)
**Spec:** `docs/superpowers/specs/2026-11-22-enemy-skill-effects-design.md`

## Global Constraints
- 予告は周期発動型で発動 0.5 秒前（`EnemySkillBase.WarnLeadSeconds = 0.5f`）、常時効果型は付いた時
- ゲームは止めない。Prefab は追加・変更しない（コード生成）
- 日本語コメント、`#nullable enable`

## Review Focus
1. 発動間隔が 0.5 秒以下の技：予告が毎フレーム出続けない（1周期に1回だけ）
2. デバッグの「今すぐ発動」：予告フラグがリセットされ、次の周期でも予告が出る
3. サメの到着前に狙ったぷよが消えた・別のぷよが落ちてきた：素通りし、別のぷよを誤って食べない
4. ステージ切替・ゲームオーバーで演出途中のサメ：例外を出さず消える（CancellationToken）
5. 回復量0（HP満タン）でも「+N」を出すか：実際に回復した量を出し、0なら出さない

### Task 1: 予告のタイミング（ロジック）
**Files:** Modify `EnemySkills/EnemySkillBase.cs`, `EnemySkills/EnemySkillManager.cs`, `GameContext.cs` / Test `Editor/Tests/EnemySkillBaseTests.cs`
**Interfaces（Produces）:** `GameContext.ShowEnemySkillWarning: Action<EnemySkillType>?` / `EnemySkillBase.WarnLeadSeconds = 0.5f`
- [ ] テスト：間隔10秒の技を `Tick(9.4)` → 予告0回、`Tick(0.2)`（計9.6）→ 1回、`Tick(0.2)` → 1回のまま、`Tick(0.3)`（発動）→ 次周期 `Tick(9.6)` で2回目
- [ ] テスト：`TriggerNow` 後も次周期で予告が出る
- [ ] RED 確認 → 実装 → GREEN
- [ ] 常時効果型は `EnemySkillManager.Initialize` / `Debug_AddAndTrigger` で OnEnter 直後に予告を呼ぶ

### Task 2: 予告アイコンと回復表示（HUD）
**Files:** Modify `GameMainHudView.cs`, `EnemyController.cs`, `GameMainController.cs`
**Interfaces:** `EnemyController.OnHealed: event Action<int>`（実際に増えた量）/ `GameMainHudView.ShowSkillWarning(Sprite? icon)` / `GameMainHudView.ShowHeal(int amount)`
- [ ] `Heal` で実際の増加量を通知（0なら通知しない）
- [ ] `GameMainController` で `_ctx.ShowEnemySkillWarning` を HUD へつなぐ（アイコンは `EnemySkillAssets.GetIcon`）
- [ ] コンパイル確認

### Task 3: サメ演出
**Files:** Create `Puyo/... なし`, `GameMain/Effects/SharkEffect.cs`, 画像 `Textures/GameMain/EnemySkills/Shark_Open.png` `Shark_Closed.png` / Modify `PuyoBoard.cs`（`DetachPiece`）, `EnemySkillAssets.cs`, `SharkSkill.cs`, `Settings/EnemySkillAssets.asset`
**Interfaces:** `PuyoBoard.DetachPiece(Vector2Int cell, PuyoPiece expected): bool`（そのセルに expected が残っていれば盤面から外す）/ `SharkEffect.PlayAsync(PuyoBoard board, Vector2Int cell, Sprite open, Sprite closed, CancellationToken ct): UniTask<bool>`（食べたら true）
- [ ] 画像を位置合わせして取り込み（PPU は既定、スプライトモード Single）
- [ ] `SharkSkill.EatAsync` を演出経由に変更（食べた数だけ回復）
- [ ] デバッグ欄からサメを追加して目視確認（予告・横切り・パクッ・+N）

### Task 4: コミット
