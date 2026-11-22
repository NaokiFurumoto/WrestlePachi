# WrestlePachi — アーキテクチャガイド

## 概要

Unity 2D モバイルゲーム。SceneBase / ViewBase / ViewManager の3層構成で画面遷移と UI を管理する。

**主要ライブラリ**
- UniTask (Cysharp.Threading.Tasks) — 非同期処理
- DOTween — アニメーション
- TextMeshPro — テキスト

**コーディング規約**
- `#nullable enable` を全ファイルに付ける
- SerializeField は `_camelCase`、public プロパティは `PascalCase`
- 非同期メソッドは `Async` suffix、fire-and-forget は `.Forget()`
- コメントは「なぜ」必要なときだけ。何をするかは書かない

---

## Scene の構成

### クラス図

```
SceneBase (GameSys)
└── MyScene : SceneBase
      ├── [SerializeField] GameUIContents _ui
      ├── [SerializeField] MyController _controller
      └── ViewManager ViewMng  ← SceneBase が生成
```

### GameUIContents の役割

Scene の Canvas 配下に2つの Transform を持つコンポーネント。

| プロパティ | 用途 |
|---|---|
| `DynamicViewRoot` | PushView で動的生成される View の親 |
| `ResidentRoot` | シーン起動時に常駐する View の親 |

SceneBase の以下をオーバーライドして渡す：

```csharp
protected override Transform? GetUIParent()
    => _ui != null ? _ui.DynamicViewRoot : base.GetUIParent();

protected override Transform? GetResidentUIParent()
    => _ui != null ? _ui.ResidentRoot    : base.GetResidentUIParent();
```

### Scene を追加する手順

1. `SceneBase` を継承したクラスを作成
2. `_ui`（GameUIContents）と各 Controller を SerializeField で持つ
3. `OnInitialize()` で Controller を初期化
4. 常駐 View にデータを渡すなら `GetResidentViewData()` をオーバーライド
5. Unity Editor で .unity ファイルを作成し、GameUIContents をアサイン
6. 常駐 View を ResidentRoot 配下の Hierarchy に配置

```csharp
public sealed class MyScene : SceneBase
{
    [SerializeField] private GameUIContents? _ui;
    [SerializeField] private MyController?  _controller;

    protected override Transform? GetUIParent()         => _ui?.DynamicViewRoot ?? base.GetUIParent();
    protected override Transform? GetResidentUIParent() => _ui?.ResidentRoot    ?? base.GetResidentUIParent();

    protected override async UniTask OnInitialize()
    {
        _controller?.Initialize(ViewMng);
        await UniTask.Yield();
    }

    protected override ViewBase.ViewData? GetResidentViewData(ViewBase view)
    {
        if (view is MyResidentView && _controller != null)
            return new MyResidentView.MyViewData { Controller = _controller };
        return base.GetResidentViewData(view);
    }
}
```

### エディタ直接再生（Bootstrap パターン）

BootScene を通さず直接 Play したとき、DontDestroyOnLoad な Manager が存在しないため自動生成する。

```csharp
private void Start()
{
    if (!SceneManager.isValid)
        BootstrapAsync().Forget();
}

private async UniTaskVoid BootstrapAsync()
{
    _AutoCreate<SceneFade>("[SceneFade]");
    _AutoCreate<PrefabManager>("[PrefabManager]");
    _AutoCreate<AppSound>("[AppSound]");
    _AutoCreate<LocalizationManager>("[LocalizationManager]");
    _AutoCreate<StaminaManager>("[StaminaManager]");

    await Intialize(null);
    _OnEndSceneFadeOut();
}

private static void _AutoCreate<T>(string name) where T : MonoBehaviour
{
    if (FindObjectOfType<T>() == null)
        new GameObject(name).AddComponent<T>();
}
```

---

## View の構成

### 種類

| 種類 | 用途 | 配置 |
|---|---|---|
| **常駐 View** | シーン起動時から表示され続ける HUD など | Scene の ResidentRoot 配下の Hierarchy |
| **動的 View** | PushView で表示・PopView で破棄するパネル・ダイアログ | Resources/Prefabs/Views/ に Prefab |

### ライフサイクル

```
Awake()
  → CanvasGroup を alpha=0・無効に初期化

Initialize(ViewData?)
  → OnInitialize()        // UI 初期化（同期）
  → OnInitializeAsync()   // UI 初期化（非同期）

OpenAsync()
  → gameObject.SetActive(true)
  → ViewAnimation.PlayOpenAsync()  // Fade/Slide など
  → CanvasGroup を alpha=1・有効に

CloseAsync()
  → ViewAnimation.PlayCloseAsync()
  → gameObject.SetActive(false)

Release()
  → OnRelease()  // イベント購読解除など
```

### View を追加する手順

**① ViewKeys に定数を追加**

```csharp
// Assets/Scripts/App/View/ViewKeys.cs
public const string MY_VIEW = "Prefabs/Views/MyView";
```

**② Prefab を作成**

```
Assets/Resources/Prefabs/Views/MyView.prefab
```

Prefab のルートに必要なコンポーネント：
- `Canvas`
- `CanvasGroup`
- `ViewCanvasOrder`
- `ViewAnimation`（アニメあり View のみ）
- `MyView`（スクリプト）

**③ ViewBase を継承したスクリプトを作成**

```csharp
#nullable enable
using Cysharp.Threading.Tasks;
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    public sealed class MyView : ViewBase
    {
        // ─── ViewData ─────────────────────────────────────────
        public sealed class MyViewData : ViewData
        {
            public string Message { get; set; } = "";
        }

        // ─── Inspector フィールド ──────────────────────────────
        [SerializeField] private TMP_Text? _messageText;
        [SerializeField] private Button?   _closeButton;

        // ─── 初期化 ────────────────────────────────────────────
        protected override void OnInitialize()
        {
            if (Data is not MyViewData data) return;
            if (_messageText != null) _messageText.text = data.Message;
            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        }

        // ─── ボタン ────────────────────────────────────────────
        private void OnCloseClicked() => PopAsync().Forget();

        // ─── 後処理 ────────────────────────────────────────────
        protected override void OnRelease()
        {
            // イベント購読解除など
        }
    }
}
#nullable disable
```

**④ 呼び出し**

```csharp
var data = new MyView.MyViewData { Message = "Hello" };
ViewManager.PushViewAsync<MyView>(ViewKeys.MY_VIEW, data).Forget();

// 閉じる（View 内から）
PopAsync().Forget();

// 閉じる（外から）
ViewManager.PopViewAsync<MyView>().Forget();
```

---

## ViewManager API

```csharp
// 表示
ViewManager.PushViewAsync<T>(string key, ViewData? data)
ViewManager.PushView<T>(string key, ViewData? data)

// 非表示・破棄
ViewManager.PopViewAsync(ViewBase? view)
ViewManager.PopView(ViewBase? view)

// System View（フェード等、最前面に出す View）
ViewManager.PushSystemViewAsync<T>(string key, ViewData? data)
ViewManager.PopSystemViewAsync(ViewBase? view)

// 取得
ViewManager.GetView<T>()        // 型で検索
ViewManager.GetLatestView()     // 最前面の View

// 状態確認
ViewManager.IsBusy()            // アニメ中 or 処理中
```

---

## データの受け渡し

### Scene → 常駐 View

```csharp
// Scene 側
protected override ViewBase.ViewData? GetResidentViewData(ViewBase view)
{
    if (view is MyResidentView && _controller != null)
        return new MyResidentView.MyViewData { Controller = _controller };
    return base.GetResidentViewData(view);
}

// View 側
protected override void OnInitialize()
{
    if (Data is not MyViewData data) return;
    _controller = data.Controller;
}
```

### Controller → 動的 View（PushView 時）

```csharp
var data = new MyView.MyViewData { Message = "Game Over" };
ViewManager.PushViewAsync<MyView>(ViewKeys.MY_VIEW, data).Forget();
```

### シーン間のデータ受け渡し

セッションデータは `GameSettings` に static プロパティとして持つ。

```csharp
// Assets/Scripts/App/GameSettings.cs
public static class GameSettings
{
    public static int CurrentStageIndex { get; set; }
}
```

---

## UIAnimator

`UIAnimator` コンポーネントを GameObject にアタッチして演出を付ける。

| AnimType | 説明 |
|---|---|
| Bob | 上下にゆっくり往復（ボタンなど） |
| Slide | 左右にゆっくり往復（矢印など） |
| Flash | 点滅 |
| Punch | パンチ（一発） |
| Shake | 振動（一発） |
| Spin | スピン |
| Flip | フリップ |
| Stamp | スタンプ（表示時に） |

コードから呼ぶ場合：

```csharp
GetComponent<UIAnimator>().PlayAsync().Forget();
GetComponent<UIAnimator>().FireDirect();  // 即時再生（ループなし）
```

---

## ディレクトリ構成

```
Assets/
├── Resources/
│   ├── Prefabs/
│   │   ├── Views/      ← 動的 View の Prefab
│   │   ├── Node/       ← グリッド等の繰り返し要素
│   │   └── GameMain/   ← ゲーム内オブジェクト
│   └── StageConfigList.asset
├── Scripts/
│   ├── Sys/            ← フレームワーク（SceneBase / ViewBase / ViewManager など）
│   │   ├── Scene/
│   │   ├── View/
│   │   ├── Sound/
│   │   ├── UI/
│   │   └── Localization/
│   └── App/            ← ゲームロジック
│       ├── Boot/
│       ├── GameMain/
│       ├── StageSelect/
│       ├── Stamina/
│       └── View/       ← ViewKeys.cs など共通定義
└── Scenes/
    ├── BootScene.unity
    ├── GameScene.unity
    └── StageScene.unity
```
