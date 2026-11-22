# 敵データのアセット化と敵スキル割り当て 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 敵を1体1アセット（`EnemyData`）にし、ステージは「敵＋2戦目フラグ」から技を自動決定する形へ既存30ステージを移行し、全ステージに敵スキルを割り当てる。

**Architecture:** 技の決定は Unity 非依存の純粋関数 `EnemySkillRule` に置き、`StageConfig` は旧フィールドと同名の読み取りプロパティで呼び出し側を無変更に保つ。移行は「新旧共存 → 1回限りのエディタメニューで変換 → 旧フィールド削除」の3段階。盤面画像のない技（炎・爆弾・岩）はスプライト未設定時のみ色付けで仮表示する。

**Tech Stack:** Unity 6 / C# (`#nullable enable`) / ScriptableObject / Unity Test Framework 1.6.0（EditMode、`Assembly-CSharp-Editor` に配置）/ UniTask / DOTween

**Spec:** `docs/superpowers/specs/2026-11-22-enemy-data-design.md`

## Global Constraints

- コメントは日本語で書く。`#nullable enable` / `#nullable disable` で囲む既存スタイルに従う
- Lv上限 `MaxSkillLevel = 3`
- 2戦目＝得意技の各Lv+1（上限3）＋ `RematchExtraSkills`
- `EnemyLevel` 表示は `IsRematch ? 2 : 1`
- `EnemyData` アセットの置き場所：`Assets/Settings/Enemies/`（`Resources` 配下にしない）
- 既存30ステージの敵名・画像・HP・盤面パラメータを変えない。例外はステージ1の HP（2500 → 20）だけ
- 仮表示の色の目安：炎マス `#FF7A1A` α0.45 / 爆弾 `#FF4040` / 岩 `#8C8C8C`。スプライトが設定されていればそちらを優先する
- Unity エディタはユーザーが開いている。メニュー実行・テスト実行・プレイ確認は computer-use で Unity を操作して行う（Unity のメニュー名は日本語UI）
- コミットメッセージ末尾に `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`

## Review Focus

1. **移行メニューの2回実行**：アセットが重複作成されず、`IsRematch` やスキルが変わらないこと（Task 3 Step 5 で2回実行して確認）
2. **敵未設定のステージ**（今後ステージを足して `Enemy` を入れ忘れた場合）：`EnemyName` が `""`、`EnemySkills` が空、`EnemyLevel` が 1 で、例外を出さないこと（Task 2 のテスト）
3. **得意技が空の敵の2戦目**（クロマスク）：追加技だけが発動すること（Task 1 のテスト）
4. **Lv3の得意技を持つ敵の2戦目**：Lv が 4 にならず 3 で止まること（Task 1 のテスト）
5. **爆弾・岩の色が演出で消える**：お邪魔着地のバウンスや点滅（`PuyoPiece` が `_renderer.color` を一時変更して戻す処理）の後も仮の色が残ること（Task 4 Step 4 のプレイ確認）

---

### Task 1: `EnemyData` と技の決定ルール

**Files:**
- Create: `Assets/Scripts/App/GameMain/EnemyData.cs`
- Create: `Assets/Scripts/App/GameMain/EnemySkills/EnemySkillRule.cs`
- Test: `Assets/Scripts/App/GameMain/Editor/Tests/EnemySkillRuleTests.cs`

**Interfaces:**
- Produces:
  - `namespace App` `public sealed class EnemyData : ScriptableObject`（`[CreateAssetMenu(fileName = "EnemyData", menuName = "WrestlePachi/EnemyData")]`）
    - `[SerializeField] private string _displayName = ""; private Sprite? _faceSprite; private EnemySkillEntry[] _signatureSkills = Array.Empty<EnemySkillEntry>(); private EnemySkillEntry[] _rematchExtraSkills = Array.Empty<EnemySkillEntry>();`
    - `public string DisplayName { get; }` / `public Sprite? FaceSprite { get; }` / `public IReadOnlyList<EnemySkillEntry> SignatureSkills { get; }` / `public IReadOnlyList<EnemySkillEntry> RematchExtraSkills { get; }`
    - `#if UNITY_EDITOR public void Configure(string displayName, Sprite? faceSprite, EnemySkillEntry[] signatureSkills, EnemySkillEntry[] rematchExtraSkills) #endif`（移行メニューとテスト用）
  - `namespace App` `public static class EnemySkillRule`
    - `public const int MaxSkillLevel = 3;`
    - `public static EnemySkillEntry[] Resolve(IReadOnlyList<EnemySkillEntry> signature, IReadOnlyList<EnemySkillEntry> rematchExtra, bool isRematch)`
    - `public static EnemySkillEntry[] Resolve(EnemyData? enemy, bool isRematch)`（null なら空配列）

- [ ] **Step 1: 失敗するテストを書く**

`EnemySkillRuleTests`（NUnit、`namespace App.Tests`）。`using static App.EnemySkillType;` を使い、`E(EnemySkillType t, int lv)` ヘルパーで `EnemySkillEntry` を作る（struct の既定の等値比較で `Is.EqualTo` が配列の中身を比べる）。`StageConfigTests` も同じヘルパーを使う。

```csharp
[Test] public void FirstFight_ReturnsSignatureAsIs()
    => Assert.That(EnemySkillRule.Resolve(new[] { E(Clock, 1) }, new[] { E(GarbageDrop, 1) }, false),
                   Is.EqualTo(new[] { E(Clock, 1) }));

[Test] public void Rematch_RaisesSignatureLevelAndAddsExtra()
    => Assert.That(EnemySkillRule.Resolve(new[] { E(Clock, 1) }, new[] { E(GarbageDrop, 1) }, true),
                   Is.EqualTo(new[] { E(Clock, 2), E(GarbageDrop, 1) }));

[Test] public void Rematch_LevelIsCappedAt3()
    => Assert.That(EnemySkillRule.Resolve(new[] { E(FireTile, 3) }, Array.Empty<EnemySkillEntry>(), true),
                   Is.EqualTo(new[] { E(FireTile, 3) }));

[Test] public void Rematch_EmptySignature_ReturnsExtraOnly()   // クロマスク
    => Assert.That(EnemySkillRule.Resolve(Array.Empty<EnemySkillEntry>(), new[] { E(GarbageDrop, 1) }, true),
                   Is.EqualTo(new[] { E(GarbageDrop, 1) }));

[Test] public void Rematch_DuplicateType_KeepsHigherLevel()
    => Assert.That(EnemySkillRule.Resolve(new[] { E(Rock, 1) }, new[] { E(Rock, 1) }, true),
                   Is.EqualTo(new[] { E(Rock, 2) }));

[Test] public void NullEnemy_ReturnsEmpty()
    => Assert.That(EnemySkillRule.Resolve((EnemyData?)null, true), Is.Empty);

[Test] public void EnemyDataOverload_UsesConfiguredSkills()
{
    var enemy = ScriptableObject.CreateInstance<EnemyData>();
    enemy.Configure("テスト", null, new[] { E(Shark, 1) }, new[] { E(Rock, 2) });
    Assert.That(EnemySkillRule.Resolve(enemy, true), Is.EqualTo(new[] { E(Shark, 2), E(Rock, 2) }));
    Object.DestroyImmediate(enemy);
}
```

- [ ] **Step 2: テストが失敗することを確認**

Unity に戻ってコンパイルさせ、コンソールに `EnemySkillRule` / `EnemyData` 未定義のコンパイルエラーが出ることを確認する。

- [ ] **Step 3: `EnemyData` と `EnemySkillRule` を実装する**

`Resolve` は得意技（2戦目なら Lv+1、`Mathf.Min(..., MaxSkillLevel)`）を先、追加技を後の順で並べ、同じ Type は先に出たものの位置に Lv の高い方を残す。返り値は常に新しい配列（呼び出し側が書き換えてもアセットに影響しない）。

- [ ] **Step 4: テストが通ることを確認**

Unity のTest Runner ウィンドウ（ウィンドウ > 一般 > Test Runner）→ EditMode → `EnemySkillRuleTests` を実行。
Expected: 7件すべて PASS、コンソールにエラーなし。

- [ ] **Step 5: コミット**

```bash
git add Assets/Scripts/App/GameMain/EnemyData.cs* Assets/Scripts/App/GameMain/EnemySkills/EnemySkillRule.cs* Assets/Scripts/App/GameMain/Editor/Tests*
git commit -m "敵データアセットと敵スキル決定ルールを追加"
```

---

### Task 2: `StageConfig` を新旧共存にし、古い一括生成ツールを削除

**Files:**
- Modify: `Assets/Scripts/App/GameMain/StageConfig.cs`
- Delete: `Assets/Scripts/App/GameMain/Editor/StageConfigPopulatorEditor.cs`（と `.meta`）
- Test: `Assets/Scripts/App/GameMain/Editor/Tests/StageConfigTests.cs`

**Interfaces:**
- Consumes: `EnemyData`, `EnemySkillRule.Resolve(EnemyData?, bool)`（Task 1）
- Produces（`StageConfig`）:
  - 新フィールド：`public EnemyData? Enemy;` / `[Tooltip("直前のステージと同じ敵の2戦目か")] public bool IsRematch;`（`[Header("敵")]` の先頭に置く）
  - 旧フィールド（このタスクから Task 5 まで一時的に存在）：`[HideInInspector, FormerlySerializedAs("EnemyName")] public string LegacyEnemyName = "";` / `[HideInInspector, FormerlySerializedAs("EnemySprite")] public Sprite? LegacyEnemySprite;` / `[HideInInspector, FormerlySerializedAs("EnemyLevel")] public int LegacyEnemyLevel = 1;` / `[HideInInspector, FormerlySerializedAs("EnemySkills")] public EnemySkillEntry[] LegacyEnemySkills = Array.Empty<EnemySkillEntry>();`
  - 読み取りプロパティ（呼び出し側はこの名前のまま使う）：
    - `public string EnemyName` → `Enemy != null ? Enemy.DisplayName : LegacyEnemyName`
    - `public Sprite? EnemySprite` → `Enemy != null ? Enemy.FaceSprite : LegacyEnemySprite`
    - `public int EnemyLevel` → `Enemy != null ? (IsRematch ? 2 : 1) : LegacyEnemyLevel`
    - `public EnemySkillEntry[] EnemySkills` → `Enemy != null ? EnemySkillRule.Resolve(Enemy, IsRematch) : LegacyEnemySkills`

- [ ] **Step 1: 失敗するテストを書く**

```csharp
[Test] public void NoEnemyNoLegacy_ReturnsSafeDefaults()
{
    var s = new StageConfig();
    Assert.That(s.EnemyName, Is.EqualTo(""));
    Assert.That(s.EnemySprite, Is.Null);
    Assert.That(s.EnemyLevel, Is.EqualTo(1));
    Assert.That(s.EnemySkills, Is.Empty);
}

[Test] public void NoEnemy_FallsBackToLegacy()
{
    var s = new StageConfig { LegacyEnemyName = "クロマスク", LegacyEnemyLevel = 1 };
    Assert.That(s.EnemyName, Is.EqualTo("クロマスク"));
}

[Test] public void WithEnemy_Rematch_UsesRuleAndLevel2()
{
    var enemy = ScriptableObject.CreateInstance<EnemyData>();
    enemy.Configure("アニマスク", null, new[] { E(Clock, 1) }, new[] { E(GarbageDrop, 1) });
    var s = new StageConfig { Enemy = enemy, IsRematch = true, LegacyEnemyName = "古い名前" };
    Assert.That(s.EnemyName, Is.EqualTo("アニマスク"));
    Assert.That(s.EnemyLevel, Is.EqualTo(2));
    Assert.That(s.EnemySkills, Is.EqualTo(new[] { E(Clock, 2), E(GarbageDrop, 1) }));
    Object.DestroyImmediate(enemy);
}
```

- [ ] **Step 2: テストが失敗することを確認**

Unity でコンパイル → `LegacyEnemyName` などが未定義のエラー。

- [ ] **Step 3: `StageConfig` を上の Interfaces どおりに変更し、`StageConfigPopulatorEditor.cs` を削除**

`using UnityEngine.Serialization;` を追加。`EnemySkillType.cs` / `EnemySkillEntry.cs` / `EnemySkillEntryDrawer.cs` の「StageConfig.EnemySkills に設定」というコメントを「EnemyData の SignatureSkills / RematchExtraSkills に設定」へ更新。

- [ ] **Step 4: テストとデータ保持を確認**

- Test Runner で EditMode 全件（Task 1 の7件＋3件）が PASS
- `Assets/Resources/StageConfigList.asset` を Inspector で開き、ステージ1・10・30 を選んで、敵名などが消えていないことを確認する（`[HideInInspector]` で見えないため、デバッグモードの Inspector で確認するか、ステージ選択画面をプレイして敵の顔と名前が出ることを確認する）
- `git diff Assets/Resources/StageConfigList.asset` が空であること（`FormerlySerializedAs` で読めているだけで、まだ保存し直していない）

- [ ] **Step 5: コミット**

```bash
git add -A Assets/Scripts/App/GameMain
git commit -m "StageConfigを敵アセット参照に対応（旧フィールドは移行用に一時保持）"
```

---

### Task 3: 移行メニューを作って実行する

**Files:**
- Create: `Assets/Scripts/App/GameMain/Editor/EnemyDataMigration.cs`
- Create（メニュー実行で生成）: `Assets/Settings/Enemies/Enemy_*.asset` 18個
- Modify（メニュー実行で更新）: `Assets/Resources/StageConfigList.asset`

**Interfaces:**
- Consumes: `EnemyData.Configure(...)`（Task 1）、`StageConfig.Enemy` / `IsRematch` / `Legacy*`（Task 2）
- Produces: `[MenuItem("WrestlePachi/敵データ移行（1回限り）")] private static void Migrate()`（`namespace App.EditorTools`、`static class EnemyDataMigration`）

移行の中身（spec §6.4・§6.5・§6.6 の値をこの表のとおりにハードコードする）：

| 旧 EnemyName | アセット名 | Signature | RematchExtra | 顔の上書き |
|---|---|---|---|---|
| へなちょこマスク | `Enemy_Henachoko` | — | — | — |
| クロマスク | `Enemy_KuroMask` | — | GarbageDrop1 | — |
| アニマスク | `Enemy_AniMask` | Clock1 | GarbageDrop1 | — |
| デビルマスク | `Enemy_DevilMask` | FireTile1 | Clock1 | — |
| パインマスク | `Enemy_PineMask` | TimeBomb1 | GarbageDrop1 | — |
| マッハキラー | `Enemy_MachKiller` | Clock2, GarbageDrop1 | — | — |
| スコーピオンX | `Enemy_ScorpionX` | SkillSeal1 | BlackHoldBoost1 | — |
| サンダーボルト | `Enemy_Thunderbolt` | Confusion1 | BlackHoldPower1 | — |
| デスドライバー | `Enemy_DeathDriver` | TimeBomb2, Clock2 | — | — |
| バジリスク王 | `Enemy_BasiliskKing` | Rock1, SkillSeal1 | BlackHoldBoost1 | `Enemys01_7` |
| 竜神ビッグバン | `Enemy_RyujinBigBang` | FireTile2, TimeBomb1 | GarbageDrop2 | — |
| 骸骨仮面グリム | `Enemy_SkullGrim` | Clock3, BlackHoldBoost2, GarbageDrop2 | — | `Enemys01_6` |
| デストラクター山田 | `Enemy_DestructorYamada` | GarbageDrop2, Rock1 | Clock2 | — |
| ブラッディゴリラ | `Enemy_BloodyGorilla` | Shark1, Rock1 | GarbageDrop2 | `Enemys01_2` |
| カオスエンペラー | `Enemy_ChaosEmperor` | Confusion2, SkillSeal2, TimeBomb2 | — | — |
| 超神ゴジファイター | `Enemy_GodziFighter` | FireTile2, Shark1 | Clock2 | — |
| 魔王プリンスダーク | `Enemy_PrinceDark` | BlackHoldBoost3, BlackHoldPower3, Confusion3 | — | — |
| 絶対王者ゴッドキング | `Enemy_GodKing` | Shark3, TimeBomb3, Clock3 | — | — |

- 顔：上書きがなければ、その敵が最初に出るステージの `LegacyEnemySprite`。上書きは `AssetDatabase.LoadAllAssetsAtPath("Assets/Textures/GameMain/Puyo/Enemys01.png")` から名前で探す
- `IsRematch`：直前のステージの `LegacyEnemyName` と同じなら true
- 冪等：同名アセットが既にあれば作らずに `Configure` し直す
- 表にない敵名が出たら、何も書き換えずにエラーダイアログを出して中断する
- ステージ1（index 0）の `EnemyHp` が 2500 のときだけ 20 にする
- 実行前に確認ダイアログを出す。実行後、ステージごとに `番号 / 敵アセット名 / 2戦目 / 発動する技` をログに出す
- `StageConfigList._stages` は private なので `SerializedObject` 経由で読み書きする（`FindProperty("_stages")` → 各要素の `LegacyEnemyName` / `LegacyEnemySprite` / `EnemyHp` を読み、`Enemy`（objectReferenceValue）/ `IsRematch` / `EnemyHp` を書く）→ `ApplyModifiedProperties` → `AssetDatabase.SaveAssets`

- [ ] **Step 1: 移行前のスナップショットを取る**

スクラッチパッドに、`StageConfigList.asset` から各ステージの EnemyName・EnemySprite（fileID）・EnemyHp と盤面パラメータ（RowCount, ColorCount, GarbageInitialRows, Weight*, RainbowDenominator, PuyoFallSpeed, BallsPerPuyo, TimeLimit）を JSON で書き出す Python スクリプトを作って実行する。

- [ ] **Step 2: `EnemyDataMigration.cs` を実装する**

- [ ] **Step 3: Unity でメニュー「WrestlePachi > 敵データ移行（1回限り）」を実行**

Expected: コンソールに30行のログ、エラーなし。`Assets/Settings/Enemies/` に18個のアセット。

- [ ] **Step 4: スナップショットと照合する**

照合スクリプトで、移行後の `StageConfigList.asset` と `Enemies/*.asset`（guid で `Enemy` 参照を解決）から各ステージの「敵の表示名・顔・HP・盤面パラメータ」を組み立て、Step 1 と比較する。
Expected: 差分は「ステージ1の HP 2500→20」と「バジリスク王・骸骨仮面グリム・ブラッディゴリラの顔（null → Enemys01_7/6/2）」だけ。各ステージの `IsRematch` と技が spec §6.4 の表と一致する。

- [ ] **Step 5: 2回目を実行して冪等性を確認**

もう一度メニューを実行し、`git status` で `Assets/Settings/Enemies/` に新しいファイルが増えていないこと、Step 4 の照合結果が変わらないことを確認する。

- [ ] **Step 6: コミット**

```bash
git add Assets/Scripts/App/GameMain/Editor/EnemyDataMigration.cs* Assets/Settings/Enemies* Assets/Resources/StageConfigList.asset
git commit -m "既存30ステージを敵データアセットへ移行し敵スキルを割り当て"
```

---

### Task 4: 盤面画像のない技の仮表示

**Files:**
- Create: `Assets/Scripts/App/Puyo/FallbackSkillVisuals.cs`
- Modify: `Assets/Scripts/App/Puyo/PuyoPiece.cs`（`SetTint` 追加）
- Modify: `Assets/Scripts/App/Puyo/PuyoBoard.cs:644-711`（`ConvertToRock` / `PlaceBomb` / `AddFireTile`）
- Modify: `Assets/Scripts/App/Puyo/RockPuyoState.cs`

**Interfaces:**
- Produces:
  - `namespace App.Puyo` `public static class FallbackSkillVisuals`
    - `public static readonly Color FireTile = new(1f, 0.478f, 0.102f, 0.45f);`
    - `public static readonly Color Bomb = new(1f, 0.251f, 0.251f, 1f);`
    - `public static readonly Color Rock = new(0.549f, 0.549f, 0.549f, 1f);`
    - `public static readonly Color RockHit = new(0.35f, 0.35f, 0.35f, 1f);`
    - `public static Sprite SquareSprite { get; }`（`Texture2D.whiteTexture` から1回だけ `Sprite.Create` して使い回す。1マスの大きさに合うよう `pixelsPerUnit` を調整するか、マーカー側の `localScale` で合わせる）
  - `PuyoPiece.SetTint(Color color)`：`_renderer.color = color`

振る舞い（スプライト未設定のときだけ）：
- `PlaceBomb(null, …)`：`_colorSprites[(int)PuyoColor.OJAMA]` で `Setup` し、`SetTint(FallbackSkillVisuals.Bomb)`
- `AddFireTile(cell, null)`：当たり判定に加えて `SquareSprite` のマーカーを `FallbackSkillVisuals.FireTile` の色で生成（既存マーカー生成処理と共通化し、`RemoveFireTile` / `ClearFireTiles` でそのまま破棄されるようにする）
- `ConvertToRock(cell, d, null)`：岩化した時点で `SetTint(FallbackSkillVisuals.Rock)`。スプライトありの場合も、岩化時点で見分けがつくよう同じ色付けを行う（ヒット後はスプライトで差し替わる）
- `RockPuyoState.OnHit`：`_hitSprite` が null なら1回目のヒットで `SetTint(FallbackSkillVisuals.RockHit)`

- [ ] **Step 1: `FallbackSkillVisuals` と `PuyoPiece.SetTint` を実装**

- [ ] **Step 2: `PuyoBoard` と `RockPuyoState` を上の振る舞いどおりに変更**

- [ ] **Step 3: コンパイル確認**

Unity のコンソールにエラーがなく、Test Runner の EditMode 全件が PASS。

- [ ] **Step 4: プレイして見た目を確認**

デバッグ用にステージを選んでプレイする（ステージ選択画面でクリア済みでないと選べない場合は、`StageProgress` の PlayerPrefs を一時的に操作するか、`GameSettings.CurrentStageIndex` を変えて GameScene から起動する）。
- ステージ7（炎1）：オレンジ半透明のマスが見え、その上に置いたぷよが焼けて消える
- ステージ9（時限爆弾1）：赤いお邪魔ぷよが現れ、時間経過で爆発する／隣を消すと解除される
- ステージ17（岩1＋封印1）：灰色に変わったぷよが1回目の消去で暗い灰色になり、2回目で消える
- 爆弾・岩の色が、お邪魔着地のバウンスや消去前の点滅の後も残っている（Review Focus 5）

- [ ] **Step 5: コミット**

```bash
git add Assets/Scripts/App/Puyo
git commit -m "画像未設定の敵スキル（炎・爆弾・岩）を色付けで仮表示"
```

---

### Task 5: 旧フィールドと移行メニューの後片付け

**Files:**
- Modify: `Assets/Scripts/App/GameMain/StageConfig.cs`（`Legacy*` フィールドとフォールバックを削除）
- Delete: `Assets/Scripts/App/GameMain/Editor/EnemyDataMigration.cs`（と `.meta`）
- Create（一時）: 再シリアライズ用のエディタメニュー（実行後に削除する。同じコミットに含めない）
- Modify（再シリアライズで更新）: `Assets/Resources/StageConfigList.asset`
- Modify: `Assets/Scripts/App/GameMain/Editor/Tests/StageConfigTests.cs`（`NoEnemy_FallsBackToLegacy` を削除）

**Interfaces:**
- Produces（最終形）：`StageConfig` の敵関連は `Enemy` / `IsRematch` フィールドと、`EnemyName` / `EnemySprite` / `EnemyLevel` / `EnemySkills` の読み取りプロパティ（`Enemy` が null なら `""` / null / 1 / 空配列）

- [ ] **Step 1: テストを最終形に合わせる**

`NoEnemy_FallsBackToLegacy` を削除。`NoEnemyNoLegacy_ReturnsSafeDefaults` は `NoEnemy_ReturnsSafeDefaults` に改名して残す。

- [ ] **Step 2: `StageConfig` から `Legacy*` とフォールバックを削除し、`EnemyDataMigration.cs` を削除**

- [ ] **Step 3: アセットを再シリアライズして旧キーを消す**

一時メニューで `AssetDatabase.ForceReserializeAssets(new[] { "Assets/Resources/StageConfigList.asset" })` を実行し、実行後にその一時スクリプトを削除する。

- [ ] **Step 4: 確認**

- `grep -c "LegacyEnemy\|EnemyName:\|EnemySprite:\|EnemySkills:" Assets/Resources/StageConfigList.asset` が 0
- Task 3 Step 4 の照合スクリプトを再実行して結果が同じ
- Test Runner の EditMode 全件が PASS
- `grep -rn "LegacyEnemy\|StageConfigPopulator\|EnemyDataMigration" Assets/Scripts` が 0 件

- [ ] **Step 5: コミット**

```bash
git add -A Assets/Scripts/App/GameMain Assets/Resources/StageConfigList.asset
git commit -m "敵データ移行の後片付け（旧フィールドと移行メニューを削除）"
```

---

### Task 6: 画面での通し確認

**Files:** なし（確認のみ。問題が見つかったら該当タスクの範囲で修正してコミット）

- [ ] **Step 1: ステージ詳細画面**

ステージ選択画面から、ステージ 2・4・6・11・18・30 の詳細を開く。
Expected:
- 敵名と顔が移行前と同じ（18・21・24 は新しい顔）
- 「Lv.N」が1戦目で1、2戦目で2
- 「敵スキルを確認」を開くと spec §6.4 の技が並ぶ（2は技なしでボタンが押せない、30は3つ）

- [ ] **Step 2: ゲーム画面**

ステージ5（時計1）と8（炎2＋時計1）をプレイし、HUD の敵名・顔・Lv が正しく、技が発動することを確認する。クリア画面とゲームオーバー画面で敵名・顔が出ることも確認する。

- [ ] **Step 3: 進捗メモの更新**

`project_enemy_skills.md` と `project_stage_select.md` のメモリに、敵データの新しい構造（`EnemyData` / `IsRematch` / `EnemySkillRule`）と、盤面画像が仮の色付けのままであることを反映する。
