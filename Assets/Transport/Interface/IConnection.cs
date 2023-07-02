using System;

namespace Transport
{
    public interface IConnection
    {
        void Connect(IConfig config);

        void Disconnect();

        void Send(ArraySegment<byte> segment);

        bool Receive(out ArraySegment<byte> segment);
    }
}