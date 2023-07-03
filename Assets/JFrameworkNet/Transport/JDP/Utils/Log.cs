using System;

namespace JFramework.Udp
{
    public static class Log
    {
        public static Action<string> Info = Console.WriteLine;
        public static Action<string> Warn = Console.WriteLine;
        public static Action<string> Error = Console.Error.WriteLine;
    }
}