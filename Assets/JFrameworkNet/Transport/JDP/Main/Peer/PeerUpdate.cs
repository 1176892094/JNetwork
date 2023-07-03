using System;
using System.Net.Sockets;
// ReSharper disable All

namespace JFNet.JDP
{
    internal sealed partial class Peer
    {
        public void EarlyUpdate()
        {
            uint time = (uint)watch.ElapsedMilliseconds;

            try
            {
                switch (state)
                {
                    case State.Connected:
                        EarlyUpdateConnected(time);
                        break;
                    case State.Authority:
                        EarlyUpdateAuthority(time);
                        break;
                }
            }
            catch (SocketException exception)
            {
                Log.Error($"Disconnecting because {exception}.");
                Disconnect();
            }
            catch (ObjectDisposedException exception)
            {
                Log.Error($"Disconnecting because {exception}.");
                Disconnect();
            }
            catch (Exception exception)
            {
                Log.Error($"Disconnecting because {exception}.");
                Disconnect();
            }
        }
        
        public void AfterUpdate()
        {
            try
            {
                if (state == State.Authority)
                {
                    jdp.Update(watch.ElapsedMilliseconds);
                }
            }
            catch (SocketException exception)
            {
                Log.Error($"Disconnecting because {exception}.");
                Disconnect();
            }
            catch (ObjectDisposedException exception)
            {
                Log.Error($"Disconnecting because {exception}.");
                Disconnect();
            }
            catch (Exception exception)
            {
                Log.Error($"Disconnecting because {exception}.");
                Disconnect();
            }
        }

        private void EarlyUpdateConnected(uint time)
        {
            OnEarlyUpdate(time);
            if (TryReceive(out var header, out var segment))
            {
                switch (header)
                {
                    case Header.Handshake:
                        if (segment.Count != 4)
                        {
                            Log.Error($"Received invalid handshake message with size {segment.Count} != 4.");
                            Disconnect();
                            return;
                        }

                        Buffer.BlockCopy(segment.Array, segment.Offset, receiveCookie, 0, 4);
                        var prettyCookie = BitConverter.ToUInt32(segment.Array, segment.Offset);
                        Log.Info($"Received handshake with cookie = {prettyCookie}");
                        state = State.Authority;
                        peerData.onAuthority?.Invoke();
                        break;
                    case Header.Disconnect:
                        Log.Error($"Received invalid header {header} while Connected. Disconnecting the connection.");
                        Disconnect();
                        break;
                }
            }
        }

        private void EarlyUpdateAuthority(uint time)
        {
            OnEarlyUpdate(time);
            while (TryReceive(out var header, out var segment))
            {
                switch (header)
                {
                    case Header.Handshake:
                        Log.Warn($"Received invalid header {header} while Authenticated.");
                        Disconnect();
                        break;
                    case Header.Message:
                        if (segment.Count > 0)
                        {
                            peerData.onReceive?.Invoke(segment, Channel.Reliable);
                        }
                        else
                        {
                            Log.Error("Received empty Data message while Authenticated.");
                            Disconnect();
                        }

                        break;
                    case Header.Disconnect:
                        Log.Info($"Received disconnect message");
                        Disconnect();
                        break;
                }
            }
        }
        
        private void OnEarlyUpdate(uint time)
        {
            if (time >= lastReceiveTime + timeout)
            {
                Log.Error($"Connection timeout after not receiving any message for {timeout}ms.");
                Disconnect();
            }

            if (jdp.state == -1)
            {
                Log.Error($"Deadlink detected: a message was retransmitted {jdp.deadLink} times without acknowledge.");
                Disconnect();
            }

            if (time >= lastPingTime + Utils.PING_INTERVAL)
            {
                SendReliable(Header.Ping, default);
                lastPingTime = time;
            }

            if (jdp.GetBufferQueueCount() >= Utils.QUEUE_DISCONNECTED_THRESHOLD)
            {
                Log.Error($"Disconnecting connection because it can't process data fast enough.");
                jdp.sendQueue.Clear();
                Disconnect();
            }
        }
    }
}