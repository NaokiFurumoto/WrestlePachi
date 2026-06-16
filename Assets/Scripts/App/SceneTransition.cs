#nullable enable
using GameSys;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace App
{
    /// <summary>シーン遷移を一元管理する静的クラス。</summary>
    public static class SceneTransition
    {
        public static void RestartGame()
        {
            var sceneName = UnitySceneManager.GetActiveScene().name;
            if (SceneManager.isValid)
                SceneManager.Instance.TransitScene(sceneName);
            else
                UnitySceneManager.LoadScene(sceneName);
        }
    }
}
#nullable disable
