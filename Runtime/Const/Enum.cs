// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-04  23:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

namespace JFramework.Net
{
    internal enum InvokeMode : byte
    {
        ServerRpc,
        ClientRpc,
    }

    internal enum SyncMode : byte
    {
        Server,
        Client
    }

    public enum EntryMode : byte
    {
        None = 0,
        Host = 1,
        Server = 2,
        Client = 3,
    }

    internal enum DebugMode : byte
    {
        Enable,
        Disable,
    }

    public enum StateMode : byte
    {
        Connect = 0,
        Connected = 1,
        Disconnect = 2,
    }
}