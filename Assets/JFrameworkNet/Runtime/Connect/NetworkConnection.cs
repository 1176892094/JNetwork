using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JFramework.Udp;

namespace JFramework.Net
{
    public abstract class NetworkConnection
    {
        private readonly Dictionary<Channel, Batch> batches = new Dictionary<Channel, Batch>();
        public readonly int connectionId;
        public bool isAuthority;
        public bool isReady;

        internal NetworkConnection()
        {
        }

        internal NetworkConnection(int connectionId)
        {
            this.connectionId = connectionId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send<T>(T message, Channel channel = Channel.Reliable) where T : struct, NetworkMessage
        {
            using var writer = NetworkWriterPool.Pop();
            MessageUtils.Writer(message, writer);
            Send(writer.ToArraySegment(), channel);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Send(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            GetBatch(channel).Enqueue(segment, NetworkTime.localTime);
        }
        
        protected Batch GetBatch(Channel channel)
        {
            if (!batches.TryGetValue(channel, out var batch))
            {
                int threshold = Transport.Instance.GetBatchThreshold();
                batch = new Batch(threshold);
                batches[channel] = batch;
            }

            return batch;
        }
    }
}