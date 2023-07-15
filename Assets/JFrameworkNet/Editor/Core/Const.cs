using Mono.Cecil;

namespace JFramework.Editor
{
    internal struct CONST
    {
        /// <summary>
        /// 程序集名称
        /// </summary>
        public const string ASSEMBLY_NAME = "JFramework.Net";
        
        /// <summary>
        /// 命名空间
        /// </summary>
        public const string GEN_NAMESPACE = "JFramework.Net";
        
        /// <summary>
        /// 生成脚本名称
        /// </summary>
        public const string GEN_NET_CODE = "NetworkGenerator";
        
        /// <summary>
        /// 处理方法名称
        /// </summary>
        public const string PROCESS_FUNC = "JFrameworkProcessor";
        
        /// <summary>
        /// 调用Rpc取代方法的方法
        /// </summary>
        public const string INVOKE_RPC = "InvokeRpcMethod";
        
        /// <summary>
        /// Rpc的取代方法
        /// </summary>
        public const string USER_RPC = "RpcMethod";
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public const string CONSTRUCTOR = ".ctor";
        
        /// <summary>
        /// 网络变量绑定的方法
        /// </summary>
        public const string VALUE_CHANGED = "method";
        
        /// <summary>
        /// 序列化网络变量
        /// </summary>
        public const string SERIAL_METHOD = "SerializeSyncVars";
        
        /// <summary>
        /// 反序列化网络变量
        /// </summary>
        public const string DE_SERIAL_METHOD = "DeserializeSyncVars";

        /// <summary>
        /// 单个网络对象携带的网络变量极限
        /// </summary>
        public const int SERVER_VAR_LIMIT = 64;

        public const MethodAttributes SERVER_VALUE = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        public const MethodAttributes SERIAL_ATTRS = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
        public const MethodAttributes METHOD_RPC = MethodAttributes.Family | MethodAttributes.Static | MethodAttributes.HideBySig;
        public const MethodAttributes METHOD_ATTRS = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
        public const MethodAttributes STATIC_CCTOR = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static;
        public const TypeAttributes ATTRIBUTES = TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
    }
}