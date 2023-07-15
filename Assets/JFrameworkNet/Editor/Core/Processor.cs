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
        
        public readonly MethodReference ActionDoubleReference;
        
        public readonly MethodReference GetWriterReference;
        public readonly MethodReference ReturnWriterReference;
        
        public readonly MethodReference NetworkEntityDirtyReference;
        public readonly MethodReference RemoteCallDelegateConstructor;
        
        public readonly MethodReference InitSyncObjectReference;

        public readonly MethodReference logErrorReference;
        public readonly MethodReference NetworkClientGetActive;
        public readonly MethodReference NetworkServerGetActive;
        
        public readonly MethodReference ArraySegmentConstructorReference;
        public readonly MethodReference ScriptableObjectCreateInstanceMethod;
        public readonly MethodReference readNetworkBehaviourGeneric;
        public readonly MethodReference sendServerRpcInternal;
        public readonly MethodReference sendTargetRpcInternal;
        public readonly MethodReference sendClientRpcInternal;
        
        public readonly MethodReference getSyncVarGameObjectReference;
        public readonly MethodReference getSyncVarNetworkIdentityReference;
        public readonly MethodReference getSyncVarNetworkBehaviourReference;
        
        public readonly MethodReference generalSyncVarSetter;
        public readonly MethodReference gameObjectSyncVarSetter;
        public readonly MethodReference networkObjectSyncVarSetter;
        public readonly MethodReference networkEntitySyncVarSetter;
        
        public readonly MethodReference generalSyncVarGetter;
        public readonly MethodReference gameObjectSyncVarGetter;
        public readonly MethodReference networkObjectSyncVarGetter;
        public readonly MethodReference networkEntitySyncVarGetter;
        
        public readonly MethodReference registerServerRpcReference;
        public readonly MethodReference registerClientRpcReference;
        public readonly MethodReference getTypeFromHandleReference;
        
        
        public readonly TypeDefinition initializeOnLoadMethodAttribute;
        public readonly TypeDefinition runtimeInitializeOnLoadMethodAttribute;

        public TypeReference Import<T>() => Import(typeof(T));
        public TypeReference Import(Type t) => assembly.MainModule.ImportReference(t);

        public Processor(AssemblyDefinition assembly, Logger logger)
        {
            this.assembly = assembly;

            TypeReference ArraySegmentType = Import(typeof(ArraySegment<>));
            ArraySegmentConstructorReference = Resolvers.ResolveMethod(ArraySegmentType, assembly, logger, CONST.CONSTRUCTOR);
            
            TypeReference ActionType = Import(typeof(Action<,>));
            ActionDoubleReference = Resolvers.ResolveMethod(ActionType, assembly, logger, ".ctor");
            
            TypeReference NetworkClientType = Import(typeof(NetworkClient)); // 处理ClientRpc
            NetworkClientGetActive = Resolvers.ResolveMethod(NetworkClientType, assembly, logger, "get_isActive");
            TypeReference NetworkServerType = Import(typeof(NetworkServer)); // 处理ServerRpc
            NetworkServerGetActive = Resolvers.ResolveMethod(NetworkServerType, assembly, logger, "get_isActive");
            
            TypeReference readerExtensions = Import(typeof(StreamExtensions));
            readNetworkBehaviourGeneric = Resolvers.ResolveMethod(readerExtensions, assembly, logger, method => method.Name == nameof(StreamExtensions.ReadNetworkBehaviour) && method.HasGenericParameters);
            
          
            TypeReference NetworkEntityType = Import<NetworkEntity>();
            NetworkEntityDirtyReference = Resolvers.ResolveProperty(NetworkEntityType, assembly, "serverVarDirty");
            
            generalSyncVarSetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GeneralSyncVarSetter");
            generalSyncVarGetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GeneralSyncVarGetter");
            gameObjectSyncVarSetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GameObjectSyncVarSetter");
            gameObjectSyncVarGetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GameObjectSyncVarGetter");
            networkObjectSyncVarSetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "NetworkObjectSyncVarSetter");
            networkObjectSyncVarGetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "NetworkObjectSyncVarGetter");
            networkEntitySyncVarSetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "NetworkEntitySyncVarSetter");
            networkEntitySyncVarGetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "NetworkEntitySyncVarGetter");
            
            getSyncVarGameObjectReference = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GetGameObjectSyncVar");
            getSyncVarNetworkIdentityReference = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GetNetworkObjectSyncVar");
            getSyncVarNetworkBehaviourReference = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GetNetworkEntitySyncVar");
            
            sendServerRpcInternal = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "SendServerRpcInternal");
            sendClientRpcInternal = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "SendClientRpcInternal");
            sendTargetRpcInternal = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "SendTargetRpcInternal");
            
            InitSyncObjectReference = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "InitSyncObject");
            
            TypeReference RemoteProcedureCallsType = Import(typeof(NetworkRpc));
            registerServerRpcReference = Resolvers.ResolveMethod(RemoteProcedureCallsType, assembly, logger, "RegisterServerRpc");
            registerClientRpcReference = Resolvers.ResolveMethod(RemoteProcedureCallsType, assembly, logger, "RegisterClientRpc");
            
            TypeReference RemoteCallDelegateType = Import<RpcDelegate>();
            RemoteCallDelegateConstructor = Resolvers.ResolveMethod(RemoteCallDelegateType, assembly, logger, ".ctor");
            
            TypeReference ScriptableObjectType = Import<ScriptableObject>();
            ScriptableObjectCreateInstanceMethod = Resolvers.ResolveMethod(ScriptableObjectType, assembly, logger, method => method.Name == "CreateInstance" && method.HasGenericParameters);

            
            TypeReference unityDebugType = Import(typeof(Debug));
            logErrorReference = Resolvers.ResolveMethod(unityDebugType, assembly, logger, method => method.Name == "LogError" && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.FullName == typeof(object).FullName);
           
            TypeReference typeType = Import(typeof(Type));
            getTypeFromHandleReference = Resolvers.ResolveMethod(typeType, assembly, logger, "GetTypeFromHandle");
            TypeReference NetworkWriterPoolType = Import(typeof(NetworkWriter));
            GetWriterReference = Resolvers.ResolveMethod(NetworkWriterPoolType, assembly, logger, "Pop"); 
            ReturnWriterReference = Resolvers.ResolveMethod(NetworkWriterPoolType, assembly, logger, "Push");
         
            if (Helpers.IsEditorAssembly(assembly))
            {
                TypeReference initializeOnLoadMethodAttributeRef = Import(typeof(InitializeOnLoadMethodAttribute));
                initializeOnLoadMethodAttribute = initializeOnLoadMethodAttributeRef.Resolve();
            }

            TypeReference runtimeInitializeOnLoadMethodAttributeRef = Import(typeof(RuntimeInitializeOnLoadMethodAttribute));
            runtimeInitializeOnLoadMethodAttribute = runtimeInitializeOnLoadMethodAttributeRef.Resolve();
        }
    }
}