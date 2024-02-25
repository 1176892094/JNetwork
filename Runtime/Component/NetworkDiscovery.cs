using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Random = UnityEngine.Random;

// ReSharper disable All

namespace JFramework.Net
{
    [DisallowMultipleComponent]
    public sealed class NetworkDiscovery : MonoBehaviour
    {
        /// <summary>
        /// 服务器请求
        /// </summary>
        public struct ServerRequest : Message
        {
        }

        /// <summary>
        /// 服务器响应
        /// </summary>
        public struct ServerResponse : Message
        {
            public Uri uri;
            public long serverId;
            public IPEndPoint endPoint { get; set; }
        }

        /// <summary>
        /// 传输组件
        /// </summary>
        [SerializeField] private Transport transport;

        /// <summary>
        /// 远端服务器地址
        /// </summary>
        [SerializeField] private string broadcastAddress = "";

        /// <summary>
        /// 服务器广播端口
        /// </summary>
        [SerializeField] private ushort broadcastPort = 47777;

        /// <summary>
        /// 广播间隔
        /// </summary>
        [SerializeField, Range(1, 60)] private float broadcastRate = 3;

        /// <summary>
        /// 秘密握手
        /// </summary>
        private long handshakeRequest;

        /// <summary>
        /// 服务器Id
        /// </summary>
        private long serverId;

        /// <summary>
        /// 主机
        /// </summary>
        private UdpClient udpServer;

        /// <summary>
        /// 客户端
        /// </summary>
        private UdpClient udpClient;

        /// <summary>
        /// 寻找到的响应服务器
        /// </summary>
        public event Action<ServerResponse> OnServerFound;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (handshakeRequest != 0) return;
            handshakeRequest = RandomLong();
            UnityEditor.Undo.RecordObject(this, "Set secret handshake");
        }
#endif

        public void Start()
        {
            serverId = RandomLong();
            if (transport == null)
            {
                transport = NetworkManager.Transport;
            }
#if UNITY_SERVER
            AdvertiseServer();
#endif
        }

        private void OnApplicationQuit()
        {
            StopDiscovery();
        }

        /// <summary>
        /// 获取随机值
        /// </summary>
        /// <returns></returns>
        private static long RandomLong()
        {
            int value1 = Random.Range(int.MinValue, int.MaxValue);
            int value2 = Random.Range(int.MinValue, int.MaxValue);
            return value1 + ((long)value2 << 32);
        }

        /// <summary>
        /// 关闭广播
        /// </summary>
        public void StopDiscovery()
        {
#if UNITY_ANDROID
            EndMulticastLock();
#endif
            udpServer?.Close();
            udpServer = null;
            udpClient?.Close();
            udpClient = null;
            CancelInvoke();
        }

        /// <summary>
        /// 服务器广播
        /// </summary>
        public void ServerBroadcast()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogError("网络发现不支持WebGL");
                return;
            }

            StopDiscovery();
            udpServer = new UdpClient(broadcastPort)
            {
                EnableBroadcast = true,
                MulticastLoopback = false
            };

            ServerListenAsync();
        }

        /// <summary>
        /// 异步服务器监听
        /// </summary>
        private async void ServerListenAsync()
        {
#if UNITY_ANDROID
            BeginMulticastLock();
#endif
            while (udpServer != null)
            {
                try
                {
                    var result = await udpServer.ReceiveAsync();
                    using var reader = NetworkReader.Pop(result.Buffer);
                    var handshake = reader.ReadLong();
                    if (handshakeRequest != handshake)
                    {
                        Debug.LogError("无效的握手请求！");
                        return;
                    }

                    ProcessClientRequest(reader.Read<ServerRequest>(), result.RemoteEndPoint);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// 处理客户端请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="endpoint"></param>
        private void ProcessClientRequest(ServerRequest request, IPEndPoint endpoint)
        {
            var response = ProcessRequest(request, endpoint);
            if (response.serverId == 0) return;
            using var writer = NetworkWriter.Pop();
            try
            {
                writer.WriteLong(handshakeRequest);
                writer.Write(response);
                var segment = writer.ToArraySegment();
                udpServer.Send(segment.Array, segment.Count, endpoint);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
        {
            try
            {
                return new ServerResponse
                {
                    serverId = serverId,
                    uri = transport.GetServerUri()
                };
            }
            catch (Exception)
            {
                Debug.LogError($"传输组件 {transport} 不支持网络发现。");
                throw;
            }
        }

        /// <summary>
        /// 客户端开启网络发现
        /// </summary>
        public void StartDiscovery()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogError("网络发现不支持WebGL");
                return;
            }

            StopDiscovery();

            try
            {
                udpClient = new UdpClient(0)
                {
                    EnableBroadcast = true,
                    MulticastLoopback = false
                };
            }
            catch (Exception)
            {
                StopDiscovery();
                throw;
            }

            ClientListenAsync();
            InvokeRepeating(nameof(BroadcastDiscoveryRequest), 0, broadcastRate);
        }

        /// <summary>
        /// 异步客户端监听
        /// </summary>
        private async void ClientListenAsync()
        {
            while (udpClient != null)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync();
                    using var reader = NetworkReader.Pop(result.Buffer);
                    if (reader.ReadLong() != handshakeRequest) return;
                    ProcessResponse(reader.Read<ServerResponse>(), result.RemoteEndPoint);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// 处理响应
        /// </summary>
        /// <param name="response"></param>
        /// <param name="endpoint"></param>
        private void ProcessResponse(ServerResponse response, IPEndPoint endpoint)
        {
            response.endPoint = endpoint;
            var builder = new UriBuilder(response.uri)
            {
                Host = response.endPoint.Address.ToString()
            };
            response.uri = builder.Uri;
            OnServerFound?.Invoke(response);
        }

        /// <summary>
        /// 广播发现请求
        /// </summary>
        private void BroadcastDiscoveryRequest()
        {
            if (udpClient == null) return;

            if (NetworkManager.Client.isAuthority)
            {
                StopDiscovery();
                return;
            }

            var endPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

            if (!string.IsNullOrWhiteSpace(broadcastAddress))
            {
                try
                {
                    endPoint = new IPEndPoint(IPAddress.Parse(broadcastAddress), broadcastPort);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            using var writer = NetworkWriter.Pop();
            writer.WriteLong(handshakeRequest);
            try
            {
                var request = new ServerRequest();
                writer.Write(request);
                var segment = writer.ToArraySegment();
                udpClient.SendAsync(segment.Array, segment.Count, endPoint);
            }
            catch (Exception)
            {
                // ignored
            }
        }

#if UNITY_ANDROID
        /// <summary>
        /// 多播锁
        /// </summary>
        private AndroidJavaObject multicastLock;
        
        /// <summary>
        /// 是否启用多播锁
        /// </summary>
        private bool hasMulticastLock;

        /// <summary>
        /// 开启多播锁
        /// </summary>
        private void BeginMulticastLock()
        {
            if (hasMulticastLock) return;
            if (Application.platform == RuntimePlatform.Android)
            {
                using (AndroidJavaObject activity =
 new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
                    {
                        multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "lock");
                        multicastLock.Call("acquire");
                        hasMulticastLock = true;
                    }
                }
			}

        }

        /// <summary>
        /// 结束多播锁
        /// </summary>
        private void EndMulticastLock()
        {

            if (!hasMulticastLock) return;
            multicastLock?.Call("release");
            hasMulticastLock = false;
        }
#endif
    }
}