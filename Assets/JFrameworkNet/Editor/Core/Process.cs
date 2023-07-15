using System;
using JFramework.Net;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace JFramework.Editor
{
    internal class Process
    {
        /// <summary>
        /// 注入的指定程序集
        /// </summary>
        private readonly AssemblyDefinition assembly;

        /// <summary>
        /// 当网络变量值改变时调用的方法
        /// </summary>
        public readonly MethodReference HookMethodReference;
        
        /// <summary>
        /// 网络行为被标记改变
        /// </summary>
        public readonly MethodReference NetworkBehaviourDirtyReference;
        
        /// <summary>
        /// Rpc委托的构造函数
        /// </summary>
        public readonly MethodReference RpcDelegateConstructor;

        /// <summary>
        /// 日志出现错误
        /// </summary>
        public readonly MethodReference logErrorReference;
        
        /// <summary>
        /// 获取NetworkClient.isActive
        /// </summary>
        public readonly MethodReference NetworkClientGetActive;
        
        /// <summary>
        /// 获取NetworkServer.isActive
        /// </summary>
        public readonly MethodReference NetworkServerGetActive;
        
        /// <summary>
        /// 对ArraySegment的构造函数的注入
        /// </summary>
        public readonly MethodReference ArraySegmentConstructorReference;
        
        /// <summary>
        /// 创建SO方法
        /// </summary>
        public readonly MethodReference ScriptableObjectCreateInstanceMethod;
        
        /// <summary>
        /// 读取泛型的 NetworkBehaviour
        /// </summary>
        public readonly MethodReference readNetworkBehaviourGeneric;
        
        /// <summary>
        /// NetworkBehaviour.SendServerRpcInternal
        /// </summary>
        public readonly MethodReference sendServerRpcInternal;
        
        /// <summary>
        /// NetworkBehaviour.SendTargetRpcInternal
        /// </summary>
        public readonly MethodReference sendTargetRpcInternal;
        
        /// <summary>
        /// NetworkBehaviour.SendClientRpcInternal
        /// </summary>
        public readonly MethodReference sendClientRpcInternal;

        /// <summary>
        /// NetworkBehaviour.SyncVarSetterGeneral
        /// </summary>
        public readonly MethodReference syncVarSetterGeneral;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarSetterGameObject
        /// </summary>
        public readonly MethodReference syncVarSetterGameObject;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarSetterNetworkObject
        /// </summary>
        public readonly MethodReference syncVarSetterNetworkObject;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarSetterNetworkBehaviour
        /// </summary>
        public readonly MethodReference syncVarSetterNetworkBehaviour;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarGetterGeneral
        /// </summary>
        public readonly MethodReference syncVarGetterGeneral;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarGetterGameObject
        /// </summary>
        public readonly MethodReference syncVarGetterGameObject;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarGetterNetworkObject
        /// </summary>
        public readonly MethodReference syncVarGetterNetworkObject;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarGetterNetworkBehaviour
        /// </summary>
        public readonly MethodReference syncVarGetterNetworkBehaviour;
        
        /// <summary>
        /// NetworkBehaviour.GetSyncVarGameObject
        /// </summary>
        public readonly MethodReference getSyncVarGameObject;
        
        /// <summary>
        /// NetworkBehaviour.GetSyncVarNetworkObject
        /// </summary>
        public readonly MethodReference getSyncVarNetworkObject;
        
        /// <summary>
        /// NetworkBehaviour.GetSyncVarNetworkBehaviour
        /// </summary>
        public readonly MethodReference getSyncVarNetworkBehaviour;
        
        /// <summary>
        /// NetworkUtilsRpc.RegisterServerRpc
        /// </summary>
        public readonly MethodReference registerServerRpcReference;
        
        /// <summary>
        /// NetworkUtilsRpc.RegisterClientRpc
        /// </summary>
        public readonly MethodReference registerClientRpcReference;
        
        /// <summary>
        /// NetworkWriter.Pop
        /// </summary>
        public readonly MethodReference PopWriterReference;
        
        /// <summary>
        /// NetworkWriter.Push
        /// </summary>
        public readonly MethodReference PushWriterReference;
        
        /// <summary>
        /// Type.GetTypeFromHandle
        /// </summary>
        public readonly MethodReference getTypeFromHandleReference;
        
        /// <summary>
        /// InitializeOnLoadMethodAttribute
        /// </summary>
        public readonly TypeDefinition InitializeOnLoadMethodAttribute;
        
        /// <summary>
        /// RuntimeInitializeOnLoadMethodAttribute
        /// </summary>
        public readonly TypeDefinition RuntimeInitializeOnLoadMethodAttribute;

        public TypeReference Import<T>() => Import(typeof(T));
        public TypeReference Import(Type t) => assembly.MainModule.ImportReference(t);

        public Process(AssemblyDefinition assembly, Logger logger)
        {
            this.assembly = assembly;

            TypeReference ArraySegmentType = Import(typeof(ArraySegment<>));
            ArraySegmentConstructorReference = Utils.ResolveMethod(ArraySegmentType, assembly, logger, CONST.CTOR);
            
            TypeReference ActionType = Import(typeof(Action<,>));
            HookMethodReference = Utils.ResolveMethod(ActionType, assembly, logger, ".ctor");
            
            TypeReference NetworkClientType = Import(typeof(NetworkClient));
            NetworkClientGetActive = Utils.ResolveMethod(NetworkClientType, assembly, logger, "get_isActive");
            TypeReference NetworkServerType = Import(typeof(NetworkServer));
            NetworkServerGetActive = Utils.ResolveMethod(NetworkServerType, assembly, logger, "get_isActive");
            
            TypeReference readerExtensions = Import(typeof(StreamExtensions));
            readNetworkBehaviourGeneric = Utils.ResolveMethod(readerExtensions, assembly, logger, method => method.Name == nameof(StreamExtensions.ReadNetworkBehaviour) && method.HasGenericParameters);

            TypeReference NetworkBehaviourType = Import<NetworkBehaviour>();
            NetworkBehaviourDirtyReference = Utils.ResolveProperty(NetworkBehaviourType, assembly, "syncVarDirty");
            
            syncVarSetterGeneral = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarSetterGeneral");
            syncVarSetterGameObject = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarSetterGameObject");
            syncVarSetterNetworkObject = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarSetterNetworkObject");
            syncVarSetterNetworkBehaviour = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarSetterNetworkBehaviour");
            
            syncVarGetterGeneral = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarGetterGeneral");
            syncVarGetterGameObject = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarGetterGameObject");
            syncVarGetterNetworkObject = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarGetterNetworkObject");
            syncVarGetterNetworkBehaviour = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarGetterNetworkBehaviour");
            
            getSyncVarGameObject = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "GetSyncVarGameObject");
            getSyncVarNetworkObject = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "GetSyncVarNetworkObject");
            getSyncVarNetworkBehaviour = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "GetSyncVarNetworkBehaviour");
            
            sendServerRpcInternal = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SendServerRpcInternal");
            sendClientRpcInternal = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SendClientRpcInternal");
            sendTargetRpcInternal = Utils.ResolveMethod(NetworkBehaviourType, assembly, logger, "SendTargetRpcInternal");

            TypeReference RegisterRpcType = Import(typeof(NetworkRpc));
            registerServerRpcReference = Utils.ResolveMethod(RegisterRpcType, assembly, logger, "RegisterServerRpc");
            registerClientRpcReference = Utils.ResolveMethod(RegisterRpcType, assembly, logger, "RegisterClientRpc");
            
            TypeReference RemoteCallDelegateType = Import<RpcDelegate>();
            RpcDelegateConstructor = Utils.ResolveMethod(RemoteCallDelegateType, assembly, logger, ".ctor");
            
            TypeReference ScriptableObjectType = Import<ScriptableObject>();
            ScriptableObjectCreateInstanceMethod = Utils.ResolveMethod(ScriptableObjectType, assembly, logger, method => method.Name == "CreateInstance" && method.HasGenericParameters);

            
            TypeReference DebugType = Import(typeof(Debug));
            logErrorReference = Utils.ResolveMethod(DebugType, assembly, logger, method => method.Name == "LogError" && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.FullName == typeof(object).FullName);
           
            TypeReference TypeRef = Import(typeof(Type));
            getTypeFromHandleReference = Utils.ResolveMethod(TypeRef, assembly, logger, "GetTypeFromHandle");
            
            TypeReference NetworkWriterType = Import(typeof(NetworkWriter));
            PopWriterReference = Utils.ResolveMethod(NetworkWriterType, assembly, logger, "Pop"); 
            PushWriterReference = Utils.ResolveMethod(NetworkWriterType, assembly, logger, "Push");
         
            if (Helpers.IsEditorAssembly(assembly))
            {
                TypeReference InitializeOnLoadMethodAttributeRef = Import(typeof(InitializeOnLoadMethodAttribute));
                InitializeOnLoadMethodAttribute = InitializeOnLoadMethodAttributeRef.Resolve();
            }

            TypeReference RuntimeInitializeOnLoadMethodAttributeRef = Import(typeof(RuntimeInitializeOnLoadMethodAttribute));
            RuntimeInitializeOnLoadMethodAttribute = RuntimeInitializeOnLoadMethodAttributeRef.Resolve();
        }
    }
}