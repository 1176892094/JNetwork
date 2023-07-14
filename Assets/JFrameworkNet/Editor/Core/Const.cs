using Mono.Cecil;

namespace JFramework.Editor
{
    internal struct CONST
    {
        public const string ASSEMBLY_NAME = "JFramework.Net";
        public const string GEN_NAMESPACE = "JFramework.Net";
        public const string GEN_NET_CODE = "NetworkGenerator";
        public const string PROCESS_FUNCTION = "JFrameworkProcessor";
        public const string VALUE_CHANGED = "onValueChanged";
        public const string INVOKE_RPC = "InvokeUserCode_";
        public const string USER_RPC = "UserCode_";
        public const string CONSTRUCTOR = ".ctor";
        public const string SERIAL_METHOD = "SerializeSyncVars";
        public const string DE_SERIAL_METHOD = "DeserializeSyncVars";

        public const int SERVER_VAR_LIMIT = 64;

        public const MethodAttributes METHOD_RPC = MethodAttributes.Family | MethodAttributes.Static | MethodAttributes.HideBySig;
        public const MethodAttributes METHOD_ATTRS = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
        public const MethodAttributes STATIC_CCTOR = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static;
        public const TypeAttributes ATTRIBUTES = TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
    }
}