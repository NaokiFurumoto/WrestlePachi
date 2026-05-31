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

        [Header("Controller")]
        [SerializeField] private GameMainController _controller;

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
            if (!PrefabManager.isValid)
            {
                var go = new GameObject("[PrefabManager]");
                go.AddComponent<PrefabManager>();
            }

            await Intialize(null);
            _OnEndSceneFadeOut();
        }

        // ─── SceneBase override ──────────────────────────────────

        /// <summary>
        /// シーン初期化。
        /// 呼び出し順：Game2DContents → GameMainController → View（SceneBase側で開く）
        /// </summary>
        protected override async UniTask OnInitialize()
        {
            // 1. 2D配下コンポーネントを初期化（PuyoBoard・PachinkoZone等）
            _contents.Initialize();

            // 2. ゲーム進行コントローラーを初期化
            //    ViewMng は SceneBase が生成済みなのでここで渡せる
            await _controller.InitializeAsync(_contents, ViewMng);
        }

        protected override ViewBase.ViewData GetResidentViewData(ViewBase view)
        {
            if (view is PuyoInputView)
            {
                return new PuyoInputView.InputViewData { Controller = _controller };
            }

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
            _controller.StartGame();
        }
    }
}
