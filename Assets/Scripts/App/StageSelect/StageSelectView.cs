#nullable enable
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ステージ選択画面の常駐 View。
    /// グリッド生成・ヘッダーのスタミナ表示・戻るボタンを管理する。
    /// Prefab: Resources/Prefabs/Views/StageSelectView
    /// </summary>
    public sealed class StageSelectView : ViewBase
    {
        // ─── ViewData ────────────────────────────────────────────
        public sealed class StageSelectViewData : ViewData
        {
            public StageSelectController? Controller { get; set; }
        }

        // ─── Inspector フィールド ─────────────────────────────────
        [Header("グリッド")]
        [SerializeField] private Transform? _nodeContainer;

        [Header("ヘッダー")]
        [SerializeField] private TMP_Text? _staminaText;
        [SerializeField] private TMP_Text? _recoveryTimerText;
        [SerializeField] private Button?   _backButton;

        // ─── 内部状態 ─────────────────────────────────────────────
        private StageSelectController?         _controller;
        private readonly List<StageSelectNode> _nodes = new();

        // ─── 初期化 ──────────────────────────────────────────────
        protected override async UniTask OnInitializeAsync()
        {
            if (Data is not StageSelectViewData data) return;
            _controller = data.Controller;

            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);

            UpdateStaminaText();
            if (StaminaManager.isValid)
            {
                StaminaManager.Instance.OnChanged      += OnStaminaChanged;
                StaminaManager.Instance.OnRecoveryTick += UpdateRecoveryTimerText;
            }

            if (_controller != null)
                _controller.OnDetailClosed = ClearSelection;

            BuildNodes();
            await UniTask.Yield();
        }

        // ─── ノード生成 ───────────────────────────────────────────
        private void BuildNodes()
        {
            var list   = StageConfigList.Instance;
            var prefab = Resources.Load<GameObject>(ViewKeys.STAGE_SELECT_NODE);
            if (list == null || prefab == null || _nodeContainer == null || _controller == null) return;

            for (var i = 0; i < list.Count; i++)
            {
                var config = list.Get(i);
                var go     = Instantiate(prefab, _nodeContainer);
                var node   = go.GetComponent<StageSelectNode>();
                if (node == null) continue;

                var idx = i;
                node.Setup(i, config?.EnemySprite, _controller.GetCellState(i), OnNodeTapped);
                _nodes.Add(node);
            }
        }

        private void OnNodeTapped(int index)
        {
            SelectNode(index);
            _controller?.OnCellTapped(index);
        }

        private void SelectNode(int index)
        {
            for (var i = 0; i < _nodes.Count; i++)
                _nodes[i].SetSelected(i == index);
        }

        private void ClearSelection()
        {
            foreach (var n in _nodes)
                n.SetSelected(false);
        }

        // ─── スタミナ表示 ─────────────────────────────────────────
        private void OnStaminaChanged(int current, int max) => UpdateStaminaText();

        private void UpdateStaminaText()
        {
            if (_staminaText == null || !StaminaManager.isValid) return;
            _staminaText.text = $"{StaminaManager.Instance.Current}/{StaminaManager.Instance.Max}";
        }

        private void UpdateRecoveryTimerText(int remaining)
        {
            if (_recoveryTimerText == null) return;
            if (!StaminaManager.isValid || StaminaManager.Instance.Current >= StaminaManager.Instance.Max)
            {
                _recoveryTimerText.text = "";
                return;
            }
            _recoveryTimerText.text = $"{remaining / 60:00}:{remaining % 60:00}";
        }

        // ─── ボタン ───────────────────────────────────────────────
        private void OnBackClicked()
        {
            if (SceneManager.isValid)
                SceneManager.Instance.TransitScene("BootScene");
        }

        // ─── 破棄 ─────────────────────────────────────────────────
        protected override void OnRelease()
        {
            if (StaminaManager.isValid)
            {
                StaminaManager.Instance.OnChanged      -= OnStaminaChanged;
                StaminaManager.Instance.OnRecoveryTick -= UpdateRecoveryTimerText;
            }
        }
    }
}
#nullable disable
