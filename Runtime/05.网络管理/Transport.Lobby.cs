// *********************************************************************************
// # Project: JFramework
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2024-12-22 18:12:38
// # Recently: 2024-12-22 21:12:47
// # Copyright: 2024, 云谷千羽
// # Description: This is an automatically generated comment.
// *********************************************************************************

namespace JFramework.Net
{
    public abstract class Lobby : Transport
    {
        /// <summary>
        /// 大厅传输器
        /// </summary>
        public Transport transport;

        /// <summary>
        /// 房间公开
        /// </summary>
        public bool isPublic = true;

        /// <summary>
        /// 房间名称
        /// </summary>
        public string roomName;

        /// <summary>
        /// 房间数据
        /// </summary>
        public string roomData;

        /// <summary>
        /// 房间在大厅服务器中的Id
        /// </summary>
        public string serverId;

        /// <summary>
        /// 大厅服务器校验码
        /// </summary>
        public string serverKey = "Secret Key";

        /// <summary>
        /// 开启大厅
        /// </summary>
        public abstract void StartLobby();

        /// <summary>
        /// 停止大厅
        /// </summary>
        public abstract void StopLobby();

        /// <summary>
        /// 更新大厅
        /// </summary>
        public abstract void UpdateLobby();

        /// <summary>
        /// 更新房间
        /// </summary>
        public abstract void UpdateRoom();
    }
}