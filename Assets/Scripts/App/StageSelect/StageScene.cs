#nullable enable
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// ステージ選択シーンのエントリーポイント。
    /// SceneBase を継承し、StageSelectController と常駐 View の初期化順を制御する。
    /// </summary>
    public sealed class StageScene : SceneBase
    {
        [Header("UI")]
        [SerializeField] private GameUIContents? _ui;

        [Header("Controller")]
        [SerializeField] private StageSelectController? _controller;

        // ─── エディタ直接再生用ブートストラップ ─────────────────
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

        // ─── SceneBase override ──────────────────────────────────

        protected override Transform? GetUIParent()         => _ui != null ? _ui.DynamicViewRoot : base.GetUIParent();
        protected override Transform? GetResidentUIParent() => _ui != null ? _ui.ResidentRoot    : base.GetResidentUIParent();

        protected override async UniTask OnInitialize()
        {
            _controller?.Initialize(ViewMng);
            await UniTask.Yield();
        }

        protected override ViewBase.ViewData? GetResidentViewData(ViewBase view)
        {
            if (view is StageSelectView && _controller != null)
                return new StageSelectView.StageSelectViewData { Controller = _controller };

            return base.GetResidentViewData(view);
        }
    }
}
#nullable disable
