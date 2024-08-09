// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  02:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;

namespace JFramework.Net
{
    public delegate void InvokeDelegate(NetworkBehaviour behaviour, NetworkReader reader, NetworkClient client);

    internal delegate void MessageDelegate(NetworkClient client, NetworkReader reader, byte channel);

    public static class Channel
    {
        public const byte Reliable = 1;
        public const byte Unreliable = 2;
    }
    
    internal static class Message<T> where T : struct, Message
    {
        public static readonly ushort Id = (ushort)NetworkUtility.GetStableId(typeof(T).FullName);
    }

    public static class Reader<T>
    {
        public static Func<NetworkReader, T> read;
    }

    public static class Writer<T>
    {
        public static Action<NetworkWriter, T> write;
    }
}