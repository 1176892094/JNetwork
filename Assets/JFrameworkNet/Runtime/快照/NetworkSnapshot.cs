using System.Collections.Generic;

namespace JFramework.Net
{
    internal static class NetworkSnapshot
    {
        public static readonly SortedList<double, SnapshotTime> snapshots = new SortedList<double, SnapshotTime>();

        /// <summary>
        /// 快照处理
        /// </summary>
        /// <param name="snapshot">新的快照</param>
        public static void OnTimeSnapshot(SnapshotTime snapshot)
        {
        }
    }
}