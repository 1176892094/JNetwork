using System.Collections.Generic;

namespace JFramework.Net
{
    internal static class NetworkSnapshot
    {
        public static readonly SortedList<double, TimeSnapshot> snapshots = new SortedList<double, TimeSnapshot>();

        /// <summary>
        /// 快照处理
        /// </summary>
        /// <param name="snapshot">新的快照</param>
        public static void OnTimeSnapshot(TimeSnapshot snapshot)
        {
        }
    }
}