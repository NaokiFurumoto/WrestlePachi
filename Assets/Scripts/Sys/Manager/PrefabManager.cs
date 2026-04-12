using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// Prefab専用のリソース管理
    /// Resourcesフォルダからロード
    /// </summary>
    public class PrefabManager : BehaviourSingleton<PrefabManager>
    {
        private readonly Dictionary<string, GameObject> _cache = new();

        protected override void _SetInstance()
        {
            if (s_Instance == null)
                s_Instance = this;
            else if (s_Instance != this)
                Destroy(gameObject);
        }

        /// <summary>
        /// 非同期でPrefabロード
        /// </summary>
        public async UniTask<GameObject?> LoadAsync(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return null;

            // キャッシュがあればそれを返す
            if (_cache.TryGetValue(prefabPath, out var prefab))
                return prefab;

            // Resourcesからロード
            var request = Resources.LoadAsync<GameObject>(prefabPath);
            await request;

            if (request.asset is GameObject loadedPrefab)
            {
                _cache[prefabPath] = loadedPrefab;
                return loadedPrefab;
            }

            Debug.LogWarning($"Prefabロード失敗: {prefabPath}");
            return null;
        }

        /// <summary>
        /// キャッシュ解除
        /// </summary>
        public void Release(string prefabPath)
        {
            if (_cache.TryGetValue(prefabPath, out var prefab))
            {
                Resources.UnloadAsset(prefab);
                _cache.Remove(prefabPath);
            }
        }

        /// <summary>
        /// すべてのキャッシュ解除
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var prefab in _cache.Values)
            {
                Resources.UnloadAsset(prefab);
            }
            _cache.Clear();
        }
    }
}
