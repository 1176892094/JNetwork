// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-04  23:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;

namespace JFramework.Net
{
    internal enum SyncMode : byte
    {
        Server,
        Client
    }

    internal enum DebugMode : byte
    {
        Enable,
        Disable,
    }

    internal enum InvokeMode : byte
    {
        ServerRpc,
        ClientRpc,
    }

    [Flags]
    internal enum ObjectMode : byte
    {
        None = 0,
        Owner = 1 << 0,
        Client = 1 << 1,
        Server = 1 << 2,
    }

    internal enum SpawnMode : byte
    {
        Asset,
        Pool,
    }

    public enum Include : byte
    {
        Total,
        Other,
    }

    public enum EntryMode : byte
    {
        None = 0,
        Host = 1,
        Server = 2,
        Client = 3,
    }

    public enum StateMode : byte
    {
        Connect = 0,
        Connected = 1,
        Disconnect = 2,
    }
}