#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// NEXT / NEXT NEXT ぷよを2Dワールド空間に表示するコンポーネント。
    /// BoardRoot/NextPuyo に配置し、PuyoBoard.OnNextQueueChanged を購読して更新される。
    /// </summary>
    public sealed class NextPuyoDisplay : MonoBehaviour
    {
        [Header("NEXT")]
        [SerializeField] private SpriteRenderer _next1Sub  = null!;
        [SerializeField] private SpriteRenderer _next1Main = null!;

        [Header("NEXT NEXT")]
        [SerializeField] private SpriteRenderer _next2Sub  = null!;
        [SerializeField] private SpriteRenderer _next2Main = null!;

        private Sprite[] _sprites = System.Array.Empty<Sprite>();

        /// <summary>
        /// PuyoBoard の ColorSprites を渡して初期化する。
        /// GameMainController.InitializeAsync() から呼ぶこと。
        /// </summary>
        public void Initialize(Sprite[] sprites)
        {
            _sprites = sprites;
        }

        /// <summary>
        /// PuyoBoard.OnNextQueueChanged に登録するコールバック。
        /// nextPairs[0]=NEXT, nextPairs[1]=NEXT NEXT。
        /// </summary>
        public void Refresh(IReadOnlyList<PuyoPairColors> nextPairs)
        {
            if (nextPairs.Count >= 1)
            {
                if (_next1Main != null) _next1Main.sprite = GetSprite(nextPairs[0].Main);
                if (_next1Sub  != null) _next1Sub.sprite  = GetSprite(nextPairs[0].Sub);
            }
            if (nextPairs.Count >= 2)
            {
                if (_next2Main != null) _next2Main.sprite = GetSprite(nextPairs[1].Main);
                if (_next2Sub  != null) _next2Sub.sprite  = GetSprite(nextPairs[1].Sub);
            }
        }

        private Sprite? GetSprite(PuyoColor color)
        {
            var idx = (int)color;
            return idx >= 0 && idx < _sprites.Length ? _sprites[idx] : null;
        }
    }
}
#nullable disable
