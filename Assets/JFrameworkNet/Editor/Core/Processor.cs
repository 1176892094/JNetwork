using System;
using JFramework.Net;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace JFramework.Editor
{
    internal class Processor
    {
        private readonly AssemblyDefinition assembly;
        
        /// <summary>
        /// NetworkWriter.Pop
        /// </summary>
        public readonly MethodReference PopWriterReference;
        
        /// <summary>
        /// NetworkWriter.Push
        /// </summary>
        public readonly MethodReference PushWriterReference;
        
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
        /// 初始化同步对象
        /// </summary>
        public readonly MethodReference InitSyncObjectReference;

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
        public readonly MethodReference generalSyncVarSetter;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarSetterGameObject
        /// </summary>
        public readonly MethodReference gameObjectSyncVarSetter;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarSetterNetworkObject
        /// </summary>
        public readonly MethodReference networkObjectSyncVarSetter;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarSetterNetworkBehaviour
        /// </summary>
        public readonly MethodReference networkBehaviourSyncVarSetter;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarGetterGeneral
        /// </summary>
        public readonly MethodReference generalSyncVarGetter;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarGetterGameObject
        /// </summary>
        public readonly MethodReference gameObjectSyncVarGetter;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarGetterNetworkObject
        /// </summary>
        public readonly MethodReference networkObjectSyncVarGetter;
        
        /// <summary>
        /// NetworkBehaviour.SyncVarGetterNetworkBehaviour
        /// </summary>
        public readonly MethodReference networkBehaviourSyncVarGetter;
        
        /// <summary>
        /// NetworkBehaviour.GetSyncVarGameObject
        /// </summary>
        public readonly MethodReference getSyncVarGameObjectReference;
        
        /// <summary>
        /// NetworkBehaviour.GetSyncVarNetworkObject
        /// </summary>
        public readonly MethodReference getSyncVarNetworkObjectReference;
        
        /// <summary>
        /// NetworkBehaviour.GetSyncVarNetworkBehaviour
        /// </summary>
        public readonly MethodReference getSyncVarNetworkBehaviourReference;
        
        /// <summary>
        /// NetworkUtilsRpc.RegisterServerRpc
        /// </summary>
        public readonly MethodReference registerServerRpcReference;
        
        /// <summary>
        /// NetworkUtilsRpc.RegisterClientRpc
        /// </summary>
        public readonly MethodReference registerClientRpcReference;
        
        /// <summary>
        /// Type.GetTypeFromHandle
        /// </summary>
        public readonly MethodReference getTypeFromHandleReference;
        
        
        /// <summary>
        /// InitializeOnLoadMethodAttribute
        /// </summary>
        public readonly TypeDefinition InitializeOnLoadMethodAttribute;
        public readonly TypeDefinition RuntimeInitializeOnLoadMethodAttribute;

        public TypeReference Import<T>() => Import(typeof(T));
        public TypeReference Import(Type t) => assembly.MainModule.ImportReference(t);

        public Processor(AssemblyDefinition assembly, Logger logger)
        {
            this.assembly = assembly;

            TypeReference ArraySegmentType = Import(typeof(ArraySegment<>));
            ArraySegmentConstructorReference = Resolvers.ResolveMethod(ArraySegmentType, assembly, logger, CONST.CONSTRUCTOR);
            
            TypeReference ActionType = Import(typeof(Action<,>));
            HookMethodReference = Resolvers.ResolveMethod(ActionType, assembly, logger, ".ctor");
            
            TypeReference NetworkClientType = Import(typeof(NetworkClient)); // 处理ClientRpc
            NetworkClientGetActive = Resolvers.ResolveMethod(NetworkClientType, assembly, logger, "get_isActive");
            TypeReference NetworkServerType = Import(typeof(NetworkServer)); // 处理ServerRpc
            NetworkServerGetActive = Resolvers.ResolveMethod(NetworkServerType, assembly, logger, "get_isActive");
            
            TypeReference readerExtensions = Import(typeof(StreamExtensions));
            readNetworkBehaviourGeneric = Resolvers.ResolveMethod(readerExtensions, assembly, logger, method => method.Name == nameof(StreamExtensions.ReadNetworkBehaviour) && method.HasGenericParameters);
            
          
            TypeReference NetworkBehaviourType = Import<NetworkBehaviour>();
            NetworkBehaviourDirtyReference = Resolvers.ResolveProperty(NetworkBehaviourType, assembly, "syncVarDirty");
            
            generalSyncVarSetter = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarSetterGeneral");
            gameObjectSyncVarSetter = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarSetterGameObject");
            networkObjectSyncVarSetter = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarSetterNetworkObject");
            networkBehaviourSyncVarSetter = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarSetterNetworkBehaviour");
            
            generalSyncVarGetter = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarGetterGeneral");
            gameObjectSyncVarGetter = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarGetterGameObject");
            networkObjectSyncVarGetter = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarGetterNetworkObject");
            networkBehaviourSyncVarGetter = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SyncVarGetterNetworkBehaviour");
            
            getSyncVarGameObjectReference = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "GetSyncVarGameObject");
            getSyncVarNetworkObjectReference = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "GetSyncVarNetworkObject");
            getSyncVarNetworkBehaviourReference = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "GetSyncVarNetworkBehaviour");
            
            sendServerRpcInternal = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SendServerRpcInternal");
            sendClientRpcInternal = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SendClientRpcInternal");
            sendTargetRpcInternal = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "SendTargetRpcInternal");
            
            InitSyncObjectReference = Resolvers.ResolveMethod(NetworkBehaviourType, assembly, logger, "InitSyncObject");
            
            TypeReference RemoteProcedureCallsType = Import(typeof(NetworkRpc));
            registerServerRpcReference = Resolvers.ResolveMethod(RemoteProcedureCallsType, assembly, logger, "RegisterServerRpc");
            registerClientRpcReference = Resolvers.ResolveMethod(RemoteProcedureCallsType, assembly, logger, "RegisterClientRpc");
            
            TypeReference RemoteCallDelegateType = Import<RpcDelegate>();
            RpcDelegateConstructor = Resolvers.ResolveMethod(RemoteCallDelegateType, assembly, logger, ".ctor");
            
            TypeReference ScriptableObjectType = Import<ScriptableObject>();
            ScriptableObjectCreateInstanceMethod = Resolvers.ResolveMethod(ScriptableObjectType, assembly, logger, method => method.Name == "CreateInstance" && method.HasGenericParameters);

            
            TypeReference unityDebugType = Import(typeof(Debug));
            logErrorReference = Resolvers.ResolveMethod(unityDebugType, assembly, logger, method => method.Name == "LogError" && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.FullName == typeof(object).FullName);
           
            TypeReference typeType = Import(typeof(Type));
            getTypeFromHandleReference = Resolvers.ResolveMethod(typeType, assembly, logger, "GetTypeFromHandle");
            TypeReference NetworkWriterPoolType = Import(typeof(NetworkWriter));
            PopWriterReference = Resolvers.ResolveMethod(NetworkWriterPoolType, assembly, logger, "Pop"); 
            PushWriterReference = Resolvers.ResolveMethod(NetworkWriterPoolType, assembly, logger, "Push");
         
            if (Helpers.IsEditorAssembly(assembly))
            {
                TypeReference initializeOnLoadMethodAttributeRef = Import(typeof(InitializeOnLoadMethodAttribute));
                InitializeOnLoadMethodAttribute = initializeOnLoadMethodAttributeRef.Resolve();
            }

            TypeReference runtimeInitializeOnLoadMethodAttributeRef = Import(typeof(RuntimeInitializeOnLoadMethodAttribute));
            RuntimeInitializeOnLoadMethodAttribute = runtimeInitializeOnLoadMethodAttributeRef.Resolve();
        }
    }
}