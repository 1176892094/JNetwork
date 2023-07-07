using Mono.Cecil;

namespace JFramework.Editor
{
    public struct Const
    {
        public const string ASSEMBLY_NAME = "JFramework.Net";
        public const string GEN_NAMESPACE = "JFramework.Net";
        public const string GEN_NET_CODE = "NetCodeByJFramework";
        public const TypeAttributes ATTRIBUTES = TypeAttributes.Class |
                                                 TypeAttributes.AnsiClass |
                                                 TypeAttributes.Public | 
                                                 TypeAttributes.AutoClass | 
                                                 TypeAttributes.Abstract | 
                                                 TypeAttributes.Sealed | 
                                                 TypeAttributes.BeforeFieldInit;
    }
}