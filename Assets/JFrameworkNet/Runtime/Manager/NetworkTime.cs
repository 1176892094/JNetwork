using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkTime
    {
        public static double localTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Time.unscaledTimeAsDouble;
        }
    }
}