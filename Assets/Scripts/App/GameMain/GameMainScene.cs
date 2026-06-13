using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// メインゲームシーンのエントリーポイント。
    /// SceneBase を継承し、2Dコンテンツと GameMainController の
    /// 初期化順を制御する。
    /// </summary>
    public sealed class GameMainScene : SceneBase
    {
        // ─── Inspector 設定 ──────────────────────────────────────
        [Header("2D")]
        [SerializeField] private Game2DContents _contents;

        [Header("UI")]
        [SerializeField] private GameUIContents _ui;

        [Header("Controller")]
        [SerializeField] private GameMainController  _controller;
        [SerializeField] private KeyboardInputBridge _keyboardBridge;
        [SerializeField] private GameOverHUD         _gameOverHUD;

        // ─── エディタ直接再生用ブートストラップ ─────────────────
        // SceneManager 経由でシーン遷移しない場合（エディタで直接 Play など）
        // Intialize → StartGame を自分で呼ぶ。
        private void Start()
        {
            if (!SceneManager.isValid)
                BootstrapAsync().Forget();
        }

        private async UniTaskVoid BootstrapAsync()
        {
            // エディタ直接再生時、シーンに配置されていない Manager を自動生成する
            _AutoCreate<SceneFade>("[SceneFade]");
            _AutoCreate<PrefabManager>("[PrefabManager]");
            _AutoCreate<SoundManager>("[SoundManager]");
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

        // ─── SceneBase override ──────────────────────────────────

        protected override Transform? GetUIParent()         => _ui != null ? _ui.DynamicViewRoot : base.GetUIParent();
        protected override Transform? GetResidentUIParent() => _ui != null ? _ui.ResidentRoot    : base.GetResidentUIParent();

        /// <summary>
        /// シーン初期化。
        /// 呼び出し順：Game2DContents → GameMainController → View（SceneBase側で開く）
        /// </summary>
        protected override async UniTask OnInitialize()
        {
            // 1. 2D配下コンポーネントを初期化（PuyoBoard・PachinkoZone等）
            _contents.Initialize();

            // 2. ゲーム進行コントローラーを初期化（UI 参照を一緒に渡す）
            await _controller.InitializeAsync(_contents, ViewMng);

            // 3. コントローラー参照を各コンポーネントに注入
            _keyboardBridge.Initialize(_controller);
            _gameOverHUD.Initialize(_controller);
        }

        protected override ViewBase.ViewData GetResidentViewData(ViewBase view)
        {
            if (view is PuyoInputView)
                return new PuyoInputView.InputViewData { Controller = _controller };

            return base.GetResidentViewData(view);
        }

        /// <summary>
        /// シーン破棄時のクリーンアップ。
        /// </summary>
        protected override void OnRelease()
        {
            _controller.Dispose();
        }

        /// <summary>
        /// フェードアウト完了後（画面が見え始めたタイミング）に呼ばれる。
        /// ゲーム開始演出などをここで起動する。
        /// </summary>
        protected override void _OnEndSceneFadeOut()
        {
            if (StaminaManager.isValid)
                StaminaManager.Instance.Consume(1);

            _controller.StartGame();
        }
    }
}
