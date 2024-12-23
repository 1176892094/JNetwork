// *********************************************************************************
// # Project: JFramework
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2024-11-29 13:11:20
// # Recently: 2024-12-22 21:12:51
// # Copyright: 2024, 云谷千羽
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using UnityEngine;

namespace JFramework.Net
{
    public abstract class Transport : MonoBehaviour
    {
        /// <summary>
        /// 地址
        /// </summary>
        public string address = "localhost";

        /// <summary>
        /// 端口
        /// </summary>
        public ushort port = 20974;

        /// <summary>
        /// 客户端连接事件
        /// </summary>
        public Action OnClientConnect;

        /// <summary>
        /// 客户端断开事件
        /// </summary>
        public Action OnClientDisconnect;

        /// <summary>
        /// 客户端错误事件
        /// </summary>
        public Action<int, string> OnClientError;

        /// <summary>
        /// 客户端接收事件
        /// </summary>
        public Action<ArraySegment<byte>, int> OnClientReceive;

        /// <summary>
        /// 客户端连接到服务器的事件
        /// </summary>
        public Action<int> OnServerConnect;

        /// <summary>
        /// 客户端从服务器断开的事件
        /// </summary>
        public Action<int> OnServerDisconnect;

        /// <summary>
        /// 服务器错误事件
        /// </summary>
        public Action<int, int, string> OnServerError;

        /// <summary>
        /// 服务器接收客户端消息的事件
        /// </summary>
        public Action<int, ArraySegment<byte>, int> OnServerReceive;

        /// <summary>
        /// 获取最大网络消息大小
        /// </summary>
        /// <param name="channel">传输通道</param>
        /// <returns></returns>
        public abstract int MessageSize(int channel);

        /// <summary>
        /// 服务器传输信息给客户端
        /// </summary>
        public abstract void SendToClient(int clientId, ArraySegment<byte> segment, int channel = Channel.Reliable);

        /// <summary>
        /// 客户端向服务器传输信息
        /// </summary>
        /// <param name="segment">传入发送的数据</param>
        /// <param name="channel">传入通道</param>
        public abstract void SendToServer(ArraySegment<byte> segment, int channel = Channel.Reliable);

        /// <summary>
        /// 当服务器连接
        /// </summary>
        public abstract void StartServer();

        /// <summary>
        /// 当服务器停止
        /// </summary>
        public abstract void StopServer();

        /// <summary>
        /// 服务器断开指定客户端连接
        /// </summary>
        /// <param name="clientId">传入要断开的客户端Id</param>
        public abstract void StopClient(int clientId);

        /// <summary>
        /// 根据地址和端口进行连接
        /// </summary>
        public abstract void StartClient();

        /// <summary>
        /// 根据Uri连接
        /// </summary>
        /// <param name="uri">传入Uri</param>
        public abstract void StartClient(Uri uri);

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        public abstract void StopClient();

        /// <summary>
        /// 客户端Update之前
        /// </summary>
        public abstract void ClientEarlyUpdate();

        /// <summary>
        /// 客户端Update之后
        /// </summary>
        public abstract void ClientAfterUpdate();

        /// <summary>
        /// 服务器Update之前
        /// </summary>
        public abstract void ServerEarlyUpdate();

        /// <summary>
        /// 服务器Update之后
        /// </summary>
        public abstract void ServerAfterUpdate();
    }
}