using System.IO;
using System.Runtime.CompilerServices;

namespace JFramework.Net
{
    public static class MessageId<T> where T : struct, NetworkMessage
    {
        public static readonly ushort Id = (ushort)typeof(T).FullName.GetStableHashCode();
    }

    public static class MessageUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Writer<T>(T message, NetworkWriter writer) where T : struct, NetworkMessage
        {
            writer.WriteUShort(MessageId<T>.Id);
            writer.Write(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Reader(NetworkReader reader, out ushort messageId)
        {
            try
            {
                messageId = reader.ReadUShort();
                return true;
            }
            catch (EndOfStreamException)
            {
                messageId = 0;
                return false;
            }
        }
    }
}