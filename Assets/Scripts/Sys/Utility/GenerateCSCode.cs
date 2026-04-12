#nullable enable
using System.Runtime.CompilerServices;
using Cysharp.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace GameSys
{
    /// <summary>
    /// コード自動生成の共通部分
    /// </summary>
    public class GenerateCSCode
    {
        private     Utf8ValueStringBuilder      m_Sb    = ZString.CreateUtf8StringBuilder();
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GenerateCSCode( [CallerFilePath] string srcFileName = "" )
        {
            m_Sb.AppendLine( "#nullable enable" );
            m_Sb.AppendLine( "// ======================================" );
            m_Sb.AppendLine( "// このファイルは自動生成ファイルです" );
            m_Sb.AppendLine( "// 手動で変更しないようにしてください" );
            m_Sb.AppendLine( $"// 生成箇所:{System.IO.Path.GetFileName(srcFileName)}" );
            m_Sb.AppendLine( "// ======================================" );
            m_Sb.AppendLine( "" );
        }
        
        /// <summary>
        /// ファイル出力
        /// </summary>
        /// <param name="fileName">拡張子含まないファイル名</param>
        public void Generate( string fileName )
        {
            m_Sb.AppendLine( "#nullable disable" );
            
            var pathSb = ZString.CreateStringBuilder();
            pathSb.Append( Application.dataPath );
            pathSb.Append( "/Scripts/App/Generated/" );
            pathSb.Append( fileName );
            pathSb.Append( ".cs");
            
            System.IO.File.WriteAllText( pathSb.ToString(), m_Sb.ToString(), System.Text.Encoding.UTF8 );
            AssetDatabase.Refresh();
        }

        public void Append( string str )
        {
            m_Sb.Append( str );
        }

        public void AppendLine( string str )
        {
            m_Sb.AppendLine( str );
        }
    }
}
#endif //UNITY_EDITOR

#nullable disable
