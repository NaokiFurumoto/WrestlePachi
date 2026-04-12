#nullable enable
using System;

namespace GameSys
{
    /// <summary>
    /// 時間計算汎用
    /// </summary>
    public static class TimeUtility
    {
        const   int     HOURS_MIN       = 1;
        const   int     HOURS_MAX       = 24;
        const   int     MINUTES_MIN     = 1;
        const   int     MINUTES_MAX     = 60;
        const   int     DAYS_MAX        = 1;
        
        const   long    UTC_TO_LOCAL    = 60 * 60 * 9;
        const   long    ONE_DAY_SECOND  = 24 * 60 * 60;
        
        private static readonly     DateTime    UNIX_EPOCH      = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        /// <summary>
        /// 現在のUnixTime
        /// </summary>
        public static long GetUnixTime()
        {
            return (long)( DateTime.UtcNow - UNIX_EPOCH ).TotalSeconds;
        }

        /// <summary>
        /// UnixTimeからDateTime 
        /// </summary>
        public static DateTime GetDateTimeFromUnixTime( long unixTime )
        {
            return UNIX_EPOCH.AddSeconds( unixTime );
        }
        
        /// <summary>
        /// UnixTimeをDateTimeに変換 
        /// </summary>
        public static DateTime GetLocalDateTimeFromUnixTime( long unixTime )
        {
            return UNIX_EPOCH.AddSeconds( unixTime + UTC_TO_LOCAL );
        }
        
        /// <summary>
        /// DateTimeからUnixTime 
        /// </summary>
        public static long GetUnixTimeFromDateTime( DateTime dateTime )
        {
            return (long)( dateTime - UNIX_EPOCH ).TotalSeconds;
        }
    }
}

#nullable disable
