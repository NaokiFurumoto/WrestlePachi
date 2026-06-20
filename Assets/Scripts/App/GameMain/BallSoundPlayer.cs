#nullable enable
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// パチンコ玉に付けるサウンドプレイヤー。
    /// 釘・壁との衝突時に AppSound.PlayBallNailHit() を呼ぶ。
    /// AudioSource の管理は AppSound 側で完結している。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BallSoundPlayer : MonoBehaviour
    {
        [SerializeField, Min(0f)]       private float _minImpulse  = 0.8f;
        [SerializeField, Min(0f)]       private float _cooldownSec = 0.06f;
        [SerializeField, Range(0f, 1f)] private float _volume      = 0.3f;

        private float _lastPlayTime = float.NegativeInfinity;

        private void OnCollisionEnter2D(Collision2D col)
        {
            if( col.relativeVelocity.magnitude < _minImpulse ) return;
            if( Time.time - _lastPlayTime < _cooldownSec ) return;

            _lastPlayTime = Time.time;
            AppSound.PlayBallNailHit( _volume );
        }
    }
}
#nullable disable
