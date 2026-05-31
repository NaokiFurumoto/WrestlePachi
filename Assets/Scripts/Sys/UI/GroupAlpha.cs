#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace GameSys
{
    /// <summary>
    /// 子要素の表示アルファをまとめて制御する
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/Group Alpha")]
    public class GroupAlpha : MonoBehaviour
    {
        [SerializeField, Range( 0f, 1f )]
        private     float       alpha               = 1f;

        [SerializeField]
        private     bool        includeInactive     = true;

        [SerializeField]
        private     bool        applyEveryFrame     = false;

        private     SpriteRenderer[]    m_SpriteRenderers   = System.Array.Empty<SpriteRenderer>();
        private     Graphic[]           m_Graphics          = System.Array.Empty<Graphic>();
        private     float               m_AppliedAlpha      = -1f;
        private     bool                m_IsDirty           = true;

        public float Alpha
        {
            get => alpha;
            set
            {
                alpha = Mathf.Clamp01( value );
                ApplyAlpha();
            }
        }

        public bool IncludeInactive
        {
            get => includeInactive;
            set
            {
                if( includeInactive == value )
                {
                    return;
                }

                includeInactive = value;
                RefreshTargets();
                ApplyAlpha();
            }
        }

        public bool ApplyEveryFrame
        {
            get => applyEveryFrame;
            set => applyEveryFrame = value;
        }

        private void Awake()
        {
            RefreshTargets();
            ApplyAlpha();
        }

        private void OnEnable()
        {
            RefreshTargets();
            ApplyAlpha();
        }

        private void Update()
        {
            if( applyEveryFrame || m_IsDirty || Mathf.Approximately( m_AppliedAlpha, alpha ) == false )
            {
                ApplyAlpha();
            }
        }

        private void OnTransformChildrenChanged()
        {
            RefreshTargets();
            ApplyAlpha();
        }

        private void OnValidate()
        {
            alpha = Mathf.Clamp01( alpha );
            RefreshTargets();
            ApplyAlpha();
        }

        /// <summary>
        /// 子要素の描画コンポーネントを再収集
        /// </summary>
        public void RefreshTargets()
        {
            m_SpriteRenderers = GetComponentsInChildren<SpriteRenderer>( includeInactive );
            m_Graphics = GetComponentsInChildren<Graphic>( includeInactive );
            m_IsDirty = false;
        }

        /// <summary>
        /// UnityEventやTweenから呼びやすいアルファ設定
        /// </summary>
        public void SetAlpha( float value )
        {
            Alpha = value;
        }

        /// <summary>
        /// 現在の対象へアルファを即時反映
        /// </summary>
        public void ApplyAlpha()
        {
            alpha = Mathf.Clamp01( alpha );

            if( m_IsDirty )
            {
                RefreshTargets();
            }

            for( int i = 0; i < m_SpriteRenderers.Length; ++i )
            {
                var sr = m_SpriteRenderers[i];
                if( sr == null )
                {
                    m_IsDirty = true;
                    continue;
                }

                var color = sr.color;
                if( Mathf.Approximately( color.a, alpha ) )
                {
                    continue;
                }

                color.a = alpha;
                sr.color = color;
            }

            for( int i = 0; i < m_Graphics.Length; ++i )
            {
                var graphic = m_Graphics[i];
                if( graphic == null )
                {
                    m_IsDirty = true;
                    continue;
                }

                var color = graphic.color;
                if( Mathf.Approximately( color.a, alpha ) )
                {
                    continue;
                }

                color.a = alpha;
                graphic.color = color;
            }

            m_AppliedAlpha = alpha;
        }
    }
}

#nullable disable
