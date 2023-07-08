using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public abstract class Connection
    {
        private readonly Dictionary<Channel, NetworkSend> batches = new Dictionary<Channel, NetworkSend>();
        public readonly HashSet<NetworkObject> objects = new HashSet<NetworkObject>();
        public readonly int clientId;
        public bool isReady;
        public bool isLocal;
        public bool isAuthority;
        public double timestamp;

        internal Connection()
        {
        }

        internal Connection(int clientId) => this.clientId = clientId;

        internal virtual void Update()
        {
            foreach (var (channels, batch) in batches)
            {
                using var writer = NetworkWriterPool.Pop();
                while (batch.WriteDequeue(writer))
                {
                    var segment = writer.ToArraySegment();
                    if (PacketValidate(segment, channels))
                    {
                        Debug.Log(batch);
                        SendToTransport(segment, channels);
                        writer.position = 0;
                    }
                }
            }
        }

        private static bool PacketValidate(ArraySegment<byte> segment, Channel channel)
        {
            int maxPacketSize = Transport.current.GetMaxPacketSize(channel);
            if (segment.Count > maxPacketSize)
            {
                Debug.LogError($"Cannot send packet larger than {maxPacketSize} bytes, was {segment.Count} bytes");
                return false;
            }

            if (segment.Count == 0)
            {
                Debug.LogError("Cannot send zero bytes");
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send<T>(T message, Channel channel = Channel.Reliable) where T : struct, NetworkMessage
        {
            using var writer = NetworkWriterPool.Pop();
            NetworkUtils.WriteMessage(writer,message);
            Send(writer.ToArraySegment(), channel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Send(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            GetBatch(channel).WriteEnqueue(segment, NetworkTime.localTime);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable);

        protected NetworkSend GetBatch(Channel channel)
        {
            if (!batches.TryGetValue(channel, out var batch))
            {
                int threshold = Transport.current.GetBatchThreshold();
                batch = new NetworkSend(threshold);
                batches[channel] = batch;
            }

            return batch;
        }
        
        public abstract void Disconnect();
    }
}