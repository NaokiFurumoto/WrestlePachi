#nullable enable
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 起動シーン。
    /// DontDestroyOnLoad な Manager をシーン上に配置して初期化し、
    /// 完了後に次のシーンへ遷移する。
    /// </summary>
    public sealed class BootScene : SceneBase
    {
        [SerializeField] private string _nextSceneName = "GameScene";

        protected override async UniTask OnInitialize()
        {
            // Manager の Awake が完了するまで1フレーム待つ
            await UniTask.Yield();
            SceneManager.Instance.TransitScene(_nextSceneName);
        }
    }
}
#nullable disable
