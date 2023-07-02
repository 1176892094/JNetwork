using System;

namespace Transport
{
    public static class Log
    {
        public static readonly Action<string> Info = Console.WriteLine;
        public static readonly Action<string> Warn = Console.WriteLine;
        public static readonly Action<string> Error = Console.Error.WriteLine;
    }
}