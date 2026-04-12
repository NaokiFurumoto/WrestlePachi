#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// 汎用Pool
    /// </summary>
    public class Pool<T> where T : class, new()
    {
        private     int             m_Size;
        private     List<T>         m_List;
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Pool( int size )
        {
            m_Size = size;
            m_List = new List<T>( size );
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize( System.Func<int, T>? func )
        {
            if( func != null )
            {
                for( int i = 0; i < m_Size; ++i )
                {
                    m_List.Add( func( i ) );
                }
            }
            else
            {
                for( int i = 0; i < m_Size; ++i )
                {
                    m_List.Add( new T() );
                }
            }
        }

        /// <summary>
        /// 取得
        /// </summary>
        public T? Get()
        {
            if( m_List.Count == 0 )
            {
                return null;
            }
            
            var result = m_List[0];
            m_List.RemoveAt( 0 );
            return result;
        }
        
        public void Return( T elm )
        {
            if( m_List.Count >= m_Size )
            {
                return;
            }

            if( m_List.Contains( elm ) )
            {
                return;
            }
            
            m_List.Add( elm );
        }
    }
}

#nullable disable
