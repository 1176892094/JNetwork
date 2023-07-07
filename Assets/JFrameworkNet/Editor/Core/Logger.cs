using Mono.Cecil;

namespace JFramework.Editor
{
    internal interface Logger
    {
        void Warn(string message, MemberReference member = null);
        void Error(string message, MemberReference member = null);
    }
}