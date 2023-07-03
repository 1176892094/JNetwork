using System;
using JFramework.Udp;

namespace JFramework.Net
{
    public interface Transport
    {
        /// <summary>
        /// 地址
        /// </summary>
        Address Address { get; }

        /// <summary>
        /// 根据地址连接
        /// </summary>
        /// <param name="address">传入地址(IP + 端口号)</param>
        void ClientConnect(Address address);

        /// <summary>
        /// 根据Uri连接
        /// </summary>
        /// <param name="uri">传入Uri</param>
        void ClientConnect(Uri uri);

        /// <summary>
        /// 客户端向服务器传输信息
        /// </summary>
        /// <param name="segment">传入发送的数据</param>
        /// <param name="channel">传入通道</param>
        void ClientSend(ArraySegment<byte> segment, Channel channel);

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        void ClientDisconnect();

        /// <summary>
        /// 当服务器连接
        /// </summary>
        void ServerStart();

        /// <summary>
        /// 服务器传输信息给客户端
        /// </summary>
        void ServerSend(int clientId, ArraySegment<byte> segment, Channel channel);

        /// <summary>
        /// 服务器断开指定客户端连接
        /// </summary>
        /// <param name="clientId">传入要断开的客户端Id</param>
        void ServerDisconnect(int clientId);

        /// <summary>
        /// 当服务器停止
        /// </summary>
        void ServerStop();

        /// <summary>
        /// 客户端Update之前
        /// </summary>
        void ClientEarlyUpdate();

        /// <summary>
        /// 客户端Update之后
        /// </summary>
        void ClientAfterUpdate();

        /// <summary>
        /// 服务器Update之前
        /// </summary>
        void ServerEarlyUpdate();

        /// <summary>
        /// 服务器Update之后
        /// </summary>
        void ServerAfterUpdate();
    }
}