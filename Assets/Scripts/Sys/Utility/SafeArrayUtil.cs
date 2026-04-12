#nullable enable
using System;
using System.Collections.Generic;

namespace GameSys
{

    public static class SafeArrayUtil
    {
        /// <summary>
        /// 配列範囲内
        /// </summary>
        public static bool IsRangeIn<T>( this IList<T>? self, int idx )
        {
            return self != null && idx >= 0 && idx < self.Count;
        }
        
        /// <summary>
        /// 要素取得
        /// 取得できない場合はデフォルト値
        /// </summary>
        public static T SafeGetAt<T>( this IList<T>? self, int idx, T defaultValue )
        {
            return ( self != null && idx >= 0 && idx < self.Count ) ? self[idx] : defaultValue;
        }
        
        // 取得できない場合に処理
        public static T SafeGetAtWithFunc<T>( this IList<T>? self, int idx, Func<T> defaultFunc )
        {
            return ( self != null && idx >= 0 && idx < self.Count ) ? self[idx] : defaultFunc();
        }

        public static T? SafeGetAtNullable<T>( this IList<T>? self, int idx ) where T : class
        {
            return ( self != null && idx >= 0 && idx < self.Count ) ? self[idx] : null;
        }
        
        public static T? SafeGetAtNullableElem<T>( this IList<T?>? self, int idx ) where T : class
        {
            return ( self != null && idx >= 0 && idx < self.Count ) ? self[idx] : null;
        }
    }
}

#nullable disable
