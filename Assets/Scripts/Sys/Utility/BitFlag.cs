#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace GameSys
{
    /// <summary>
    /// ビットフラグ
    /// https://qiita.com/old_friend/items/0dcdcc5f2ac69d564937
    /// Enumをフラグ化することで、複数状態を1つのintで管理できる
    ///配列やListで管理するより軽量で高速
    /// </summary>
    public class BitFlag<T> where T : Enum
    {
        private     int         m_Flag          = 0;
        private     bool        m_IsDisable     = false;
        
        public BitFlag()
        {
            if( Enum.GetUnderlyingType( typeof( T ) ) == typeof( int ) == false )
            {
                m_IsDisable = true;
                DebugLog.LogError( "BitFlag generic type is not int" );
            }
        }
        
        public void Set( T val )
        {
            if( m_IsDisable )
            {
                return;
            }
            
            int idx = _ConvertInt( ref val );
            m_Flag |= 1 << idx;
        }
        
        public void Reset( T val )
        {
            if( m_IsDisable )
            {
                return;
            }
            
            int idx = _ConvertInt( ref val );
            m_Flag &= ~(1 << idx);
        }
        
        public bool Check( T val )
        {
            if( m_IsDisable )
            {
                return false;
            }
            
            int idx = _ConvertInt( ref val );
            return ( m_Flag & (1 << idx) ) > 0;
        }
        
        public void Clear()
        {
            m_Flag = 0;
        }
        
        private int _ConvertInt( ref T val )
        {
            return Unsafe.As<T, int>( ref val );
        }
    }
}
#nullable disable
