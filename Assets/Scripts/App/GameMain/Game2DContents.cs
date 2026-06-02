using App.Puyo;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace App
{
    /// <summary>
    /// 2D配下のオブジェクト参照を一元管理するクラス。
    /// 2D関連コンポーネントの参照保持と初期化順の制御を担う。
    /// ロジックは持たず、GameMainController に参照を提供する。
    /// </summary>
    public class Game2DContents : MonoBehaviour
    {
        // ─── Core ────────────────────────────────────────────────
        [Header("Core")]
        [SerializeField] private Camera    _mainCamera;
        [SerializeField] private Light2D   _mainLight;
        [SerializeField] private Transform _effectRoot;

        // ─── Puyo ────────────────────────────────────────────────
        [Header("Puyo")]
        [SerializeField] private PuyoBoard        _puyoBoard;
        [SerializeField] private NextPuyoDisplay  _nextPuyoDisplay;

        // ─── Pachinko ────────────────────────────────────────────
        [Header("Pachinko")]
        [SerializeField] private BallLauncher        _ballLauncher;
        [SerializeField] private PachinkoController  _pachinkoController;
        [SerializeField] private HoldDisplay         _holdDisplay;

        // ─── プロパティ ───────────────────────────────────────────
        public Camera        MainCamera    => _mainCamera;
        public Light2D       MainLight     => _mainLight;
        public Transform     EffectRoot    => _effectRoot;
        public PuyoBoard        PuyoBoard        => _puyoBoard;
        public NextPuyoDisplay  NextPuyoDisplay  => _nextPuyoDisplay;
        public BallLauncher       BallLauncher       => _ballLauncher;
        public PachinkoController PachinkoController => _pachinkoController;
        public HoldDisplay        HoldDisplay        => _holdDisplay;

        // ─── 初期化 ──────────────────────────────────────────────

        /// <summary>
        /// 2D配下コンポーネントを順番に初期化する。
        /// GameMainScene.OnInitialize() から呼ぶこと。
        /// </summary>
        public void Initialize()
        {
            _pachinkoController.Initialize();
        }
    }
}
