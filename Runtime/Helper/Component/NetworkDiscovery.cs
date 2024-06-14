// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-06-10  03:06
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace JFramework.Net
{
    public partial class NetworkDiscovery : MonoBehaviour
    {
        /// <summary>
        /// 版本
        /// </summary>
        [SerializeField] private int version;

        /// <summary>
        /// 远端服务器地址
        /// </summary>
        [SerializeField] private string address = "";

        /// <summary>
        /// 服务器广播端口
        /// </summary>
        [SerializeField] private ushort port = 47777;

        /// <summary>
        /// 广播间隔
        /// </summary>
        [SerializeField, Range(1, 10)] private long duration = 1;

        /// <summary>
        /// 主机
        /// </summary>
        private UdpClient udpServer;

        /// <summary>
        /// 客户端
        /// </summary>
        private UdpClient udpClient;

        /// <summary>
        /// 远端连接点
        /// </summary>
        private IPEndPoint remotePoint;

        /// <summary>
        /// 寻找到的响应服务器
        /// </summary>
        public event Action<Uri, IPEndPoint> OnServerResponse;

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
            udpServer = new UdpClient(port)
            {
                EnableBroadcast = true,
                MulticastLoopback = false
            };
#if UNITY_ANDROID
            BeginMulticastLock();
#endif
            ServerReceive();
        }

        /// <summary>
        /// 客户端开启网络发现
        /// </summary>
        public void ClientBroadcast()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogError("网络发现不支持WebGL");
                return;
            }

            StopDiscovery();
            udpClient = new UdpClient(0)
            {
                EnableBroadcast = true,
                MulticastLoopback = false
            };
            ClientReceive();
            InvokeRepeating(nameof(ClientSend), 0, duration);
        }

        /// <summary>
        /// 异步服务器监听
        /// </summary>
        private async void ServerReceive()
        {
            while (udpServer != null)
            {
                try
                {
                    var result = await udpServer.ReceiveAsync();
                    using var reader = NetworkReader.Pop(result.Buffer);
                    if (version != reader.ReadLong())
                    {
                        Debug.LogError("接收到的消息版本不同！");
                        return;
                    }

                    var request = reader.Invoke<RequestMessage>();
                    ServerSend(request, result.RemoteEndPoint);
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
        /// 异步客户端监听
        /// </summary>
        private async void ClientReceive()
        {
            while (udpClient != null)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync();
                    using var reader = NetworkReader.Pop(result.Buffer);
                    if (version != reader.ReadLong())
                    {
                        Debug.LogError("接收到的消息版本不同息！");
                        return;
                    }

                    var response = reader.Invoke<ResponseMessage>();
                    var builder = new UriBuilder(response.uri)
                    {
                        Host = remotePoint.Address.ToString()
                    };
                    response.uri = builder.Uri;
                    OnServerResponse?.Invoke(response.uri, result.RemoteEndPoint);
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

        private void ServerSend(RequestMessage request, IPEndPoint endPoint)
        {
            try
            {
                using var writer = NetworkWriter.Pop();
                writer.WriteLong(version);
                var builder = new UriBuilder
                {
                    Scheme = "https",
                    Host = Dns.GetHostName(),
                    Port = NetworkManager.Transport.port
                };
                writer.Invoke(new ResponseMessage(builder.Uri));
                ArraySegment<byte> segment = writer;
                udpServer.Send(segment.Array, segment.Count, endPoint);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// 客户端向服务器发送请求
        /// </summary>
        private void ClientSend()
        {
            try
            {
                if (NetworkManager.Client.isConnected)
                {
                    StopDiscovery();
                    return;
                }

                var endPoint = new IPEndPoint(IPAddress.Broadcast, port);

                if (!string.IsNullOrWhiteSpace(address))
                {
                    endPoint = new IPEndPoint(IPAddress.Parse(address), port);
                }

                using var writer = NetworkWriter.Pop();
                writer.WriteLong(version);
                writer.Write(new RequestMessage());
                ArraySegment<byte> segment = writer;
                udpClient.Send(segment.Array, segment.Count, endPoint);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
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
            udpClient?.Close();
            udpServer = null;
            udpClient = null;
            CancelInvoke();
        }

        private void OnDestroy()
        {
            StopDiscovery();
        }
    }
#if UNITY_ANDROID
    public partial class NetworkDiscovery
    {
        /// <summary>
        /// 是否启用多播
        /// </summary>
        private bool multicast;

        /// <summary>
        /// 多播
        /// </summary>
        private AndroidJavaObject multicastLock;

        /// <summary>
        /// 开启多播锁
        /// </summary>
        private void BeginMulticastLock()
        {
            if (multicast) return;
            if (Application.platform == RuntimePlatform.Android)
            {
                using var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
                using var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
                multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "lock");
                multicastLock.Call("acquire");
                multicast = true;
            }
        }

        /// <summary>
        /// 结束多播锁
        /// </summary>
        private void EndMulticastLock()
        {
            if (!multicast) return;
            multicastLock?.Call("release");
            multicast = false;
        }
    }
#endif
}