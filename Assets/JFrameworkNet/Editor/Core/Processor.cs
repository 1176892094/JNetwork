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

        public Processor(AssemblyDefinition assembly, Logger logger, ref bool isFailed)
        {
            this.assembly = assembly;

            TypeReference ArraySegmentType = Import(typeof(ArraySegment<>));
            ArraySegmentConstructorReference = Resolvers.ResolveMethod(ArraySegmentType, assembly, logger, CONST.CONSTRUCTOR, ref isFailed);
            
            TypeReference ActionType = Import(typeof(Action<,>));
            ActionDoubleReference = Resolvers.ResolveMethod(ActionType, assembly, logger, ".ctor", ref isFailed);
            
            TypeReference NetworkClientType = Import(typeof(NetworkClient)); // 处理ClientRpc
            NetworkClientGetActive = Resolvers.ResolveMethod(NetworkClientType, assembly, logger, "get_isActive", ref isFailed);
            TypeReference NetworkServerType = Import(typeof(NetworkServer)); // 处理ServerRpc
            NetworkServerGetActive = Resolvers.ResolveMethod(NetworkServerType, assembly, logger, "get_isActive", ref isFailed);
            
            TypeReference readerExtensions = Import(typeof(StreamExtensions));
            readNetworkBehaviourGeneric = Resolvers.ResolveMethod(readerExtensions, assembly, logger, method => method.Name == nameof(StreamExtensions.ReadNetworkBehaviour) && method.HasGenericParameters, ref isFailed);
            
          
            TypeReference NetworkEntityType = Import<NetworkEntity>();
            NetworkEntityDirtyReference = Resolvers.ResolveProperty(NetworkEntityType, assembly, "serverVarDirty");
            
            generalSyncVarSetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GeneralServerVarSetter", ref isFailed);
            generalSyncVarGetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GeneralServerVarGetter", ref isFailed);
            gameObjectSyncVarSetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GameObjectServerVarSetter", ref isFailed);
            gameObjectSyncVarGetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GameObjectServerVarGetter", ref isFailed);
            networkObjectSyncVarSetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "NetworkObjectServerVarSetter", ref isFailed);
            networkObjectSyncVarGetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "NetworkObjectServerVarGetter", ref isFailed);
            networkEntitySyncVarSetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "NetworkEntityServerVarSetter", ref isFailed);
            networkEntitySyncVarGetter = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "NetworkEntityServerVarGetter", ref isFailed);
            
            getSyncVarGameObjectReference = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GetGameObjectServerVar", ref isFailed);
            getSyncVarNetworkIdentityReference = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GetNetworkObjectServerVar", ref isFailed);
            getSyncVarNetworkBehaviourReference = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "GetNetworkEntityServerVar", ref isFailed);
            
            sendServerRpcInternal = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "SendServerRpcInternal", ref isFailed);
            sendClientRpcInternal = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "SendClientRpcInternal", ref isFailed);
            sendTargetRpcInternal = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "SendTargetRpcInternal", ref isFailed);
            
            InitSyncObjectReference = Resolvers.ResolveMethod(NetworkEntityType, assembly, logger, "InitSyncObject", ref isFailed);
            
            TypeReference RemoteProcedureCallsType = Import(typeof(NetworkRpc));
            registerServerRpcReference = Resolvers.ResolveMethod(RemoteProcedureCallsType, assembly, logger, "RegisterServerRpc", ref isFailed);
            registerClientRpcReference = Resolvers.ResolveMethod(RemoteProcedureCallsType, assembly, logger, "RegisterClientRpc", ref isFailed);
            
            TypeReference RemoteCallDelegateType = Import<RpcDelegate>();
            RemoteCallDelegateConstructor = Resolvers.ResolveMethod(RemoteCallDelegateType, assembly, logger, ".ctor", ref isFailed);
            
            TypeReference ScriptableObjectType = Import<ScriptableObject>();
            ScriptableObjectCreateInstanceMethod = Resolvers.ResolveMethod(ScriptableObjectType, assembly, logger, method => method.Name == "CreateInstance" && method.HasGenericParameters, ref isFailed);

            
            TypeReference unityDebugType = Import(typeof(Debug));
            logErrorReference = Resolvers.ResolveMethod(unityDebugType, assembly, logger, method => method.Name == "LogError" && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.FullName == typeof(object).FullName,ref isFailed);
           
            TypeReference typeType = Import(typeof(Type));
            getTypeFromHandleReference = Resolvers.ResolveMethod(typeType, assembly, logger, "GetTypeFromHandle", ref isFailed);
            TypeReference NetworkWriterPoolType = Import(typeof(NetworkWriter));
            GetWriterReference = Resolvers.ResolveMethod(NetworkWriterPoolType, assembly, logger, "Pop", ref isFailed); 
            ReturnWriterReference = Resolvers.ResolveMethod(NetworkWriterPoolType, assembly, logger, "Push", ref isFailed);
         
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