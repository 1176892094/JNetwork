using Mono.Cecil;

namespace JFramework.Editor
{
    internal struct Const
    {
        public const string ASSEMBLY_NAME = "JFramework.Net";
        public const string GEN_NAMESPACE = "JFramework.Net";
        public const string GEN_NET_CODE = "NetworkGenerator";
        public const string CONSTRUCTOR = ".ctor";
        public const MethodAttributes METHOD_ATTRS = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
        public const TypeAttributes ATTRIBUTES = TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
    }
}