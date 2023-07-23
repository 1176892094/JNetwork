using System.Collections.Generic;

namespace JFramework.Net
{
    public class NetworkTransform : NetworkBehaviour
    {
        private readonly SortedList<double, SnapshotTransform> clientSnapshots = new SortedList<double, SnapshotTransform>();
        private readonly SortedList<double, SnapshotTransform> serverSnapshots = new SortedList<double, SnapshotTransform>();
    }
}