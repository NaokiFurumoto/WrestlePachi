using UnityEngine;

namespace App
{
    /// <summary>
    /// ゲームオーバー時のオーバーレイ表示を担当する HUD。
    /// GameMainController から OnGUI を分離する。
    /// GameMainScene.OnInitialize() から Initialize() を呼んで使う。
    /// </summary>
    public sealed class GameOverHUD : MonoBehaviour
    {
        private GameMainController _controller;

        public void Initialize(GameMainController controller)
        {
            _controller = controller;
        }

        private void OnGUI()
        {
            if (!_controller.IsGameOver) return;

            var cx = Screen.width  * 0.5f;
            var cy = Screen.height * 0.5f;

            GUI.Label(
                new Rect(cx - 200, cy - 60, 400, 60),
                "GAME OVER",
                new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter });

            GUI.Label(
                new Rect(cx - 200, cy + 10, 400, 40),
                "[ R ] でリスタート",
                new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter });
        }
    }
}
