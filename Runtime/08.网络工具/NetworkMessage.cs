// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-08-27  00:08
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

namespace JFramework.Net
{
    internal static class Message<T> where T : struct, IMessage
    {
        public static readonly ushort Id = (ushort)NetworkManager.GetStableId(typeof(T).FullName);
    }
}