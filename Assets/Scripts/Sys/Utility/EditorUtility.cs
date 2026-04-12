#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

namespace GameSys
{
    #if UNITY_EDITOR
    
    public class EditorExtention
    {
        public static string DrawStringArrayPopup( string current, string[] tbl )
        {
            int idx = 0;
            
            for( int i = 0; i < tbl.Length; ++i )
            {
                if( current == tbl[i] )
                {
                    idx = i;
                    break;
                }
            }
            
            idx = EditorGUILayout.Popup( idx, tbl );
            
            if( idx == -1 || tbl.Length <= idx )
            {
                return string.Empty;
            }
            
            return tbl.SafeGetAt( idx, string.Empty );
        }
    }
    
    #endif //UNITY_EDITOR
}

#nullable disable
