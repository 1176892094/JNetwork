using System;

namespace JFramework.Net
{
    [Serializable]
    public struct NetworkRoom
    {
        public string roomId;
        public string roomName;
        public string roomData;
        public int maxCount;
        public int clientId;
        public bool isPublic;
        public int[] clients;
    }
}