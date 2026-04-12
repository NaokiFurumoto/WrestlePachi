#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// Easing
    /// https://easings.net/ja
    /// </summary>
    static public class EasingUtil
    {
        public static float EaseInOutQuart( float rate )
        {
            return rate < 0.5f ? 8 * rate * rate * rate * rate : 1 - Mathf.Pow( -2 * rate + 2, 4 ) * 0.5f;
        }
    }
}

#nullable disable
