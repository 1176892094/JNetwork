using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        private readonly Writers writers;
        private readonly Readers readers;
        private readonly Processor processor;
        private readonly SyncVarList syncVarList;
        private readonly ServerVarProcess serverVarProcess;
        private readonly AssemblyDefinition assembly;
        private readonly Logger logger;
        private readonly TypeDefinition type;
        private List<FieldDefinition> syncVars = new List<FieldDefinition>();
        private Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds = new Dictionary<FieldDefinition, FieldDefinition>();
        private readonly List<ServerRpcResult> serverRpcList = new List<ServerRpcResult>();
        private readonly List<MethodDefinition> serverRpcFuncList = new List<MethodDefinition>();
        private readonly List<ClientRpcResult> clientRpcList = new List<ClientRpcResult>();
        private readonly List<MethodDefinition> clientRpcFuncList = new List<MethodDefinition>();
        private readonly List<MethodDefinition> targetRpcList = new List<MethodDefinition>();
        private readonly List<MethodDefinition> targetRpcFuncList = new List<MethodDefinition>();

        private readonly TypeDefinition generateCode;

        public struct ServerRpcResult
        {
            public readonly MethodDefinition method;
            public ServerRpcResult(MethodDefinition method) => this.method = method;
        }

        public struct ClientRpcResult
        {
            public readonly MethodDefinition method;
            public ClientRpcResult(MethodDefinition method) => this.method = method;
        }
        
        public NetworkBehaviourProcess(AssemblyDefinition assembly, Processor processor, SyncVarList syncVars, Writers writers, Readers readers, Logger logger, TypeDefinition type)
        {
            this.type = type;
            this.logger = logger;
            this.writers = writers;
            this.readers = readers;
            this.assembly = assembly;
            this.processor = processor;
            this.syncVarList = syncVars;
            serverVarProcess = new ServerVarProcess(assembly, processor, syncVars, logger);
        }

        public bool Process(ref bool isFailed)
        {
            if (WasProcessed(type))
            {
                return false;
            }

            MarkAsProcessed(type);
            
            (syncVars, syncVarNetIds) = serverVarProcess.ProcessSyncVars(type);
            
            ProcessMethods();
            
            if (isFailed)
            {
                return true;
            }
            
            InjectIntoStaticConstructor(ref isFailed);
            
            GenerateSerialization(ref isFailed);
            if (isFailed)
            {
                return true;
            }
            
            GenerateDeserialization(ref isFailed);
            return true;
        }

        private static bool WasProcessed(TypeDefinition td)
        {
            return td.GetMethod(CONST.PROCESS_FUNC) != null;
        }

        private void MarkAsProcessed(TypeDefinition td)
        {
            if (!WasProcessed(td))
            {
                MethodDefinition versionMethod = new MethodDefinition(CONST.PROCESS_FUNC, MethodAttributes.Private, processor.Import(typeof(void)));
                ILProcessor worker = versionMethod.Body.GetILProcessor();
                worker.Emit(OpCodes.Ret);
                td.Methods.Add(versionMethod);
            }
        }

        public static void WriteClientActiveCheck(ILProcessor worker, Processor processor, string mdName, Instruction label, string error)
        {
            worker.Emit(OpCodes.Call, processor.NetworkClientGetActive);
            worker.Emit(OpCodes.Brtrue, label);
            worker.Emit(OpCodes.Ldstr, $"{error} 方法 {mdName} 远程调用服务器异常.");
            worker.Emit(OpCodes.Call, processor.logErrorReference);
            worker.Emit(OpCodes.Ret);
            worker.Append(label);
        }

        public static void WriteServerActiveCheck(ILProcessor worker, Processor processor, string mdName, Instruction label, string error)
        {
            worker.Emit(OpCodes.Call, processor.NetworkServerGetActive);
            worker.Emit(OpCodes.Brtrue, label);

            worker.Emit(OpCodes.Ldstr, $"{error} {mdName} called on client.");
            worker.Emit(OpCodes.Call, processor.logErrorReference);
            worker.Emit(OpCodes.Ret);
            worker.Append(label);
        }


        public static bool WriteArguments(ILProcessor worker, Writers writers, Logger logger, MethodDefinition method, RpcType rpcType)
        {
            bool skipFirst = rpcType == RpcType.TargetRpc && TargetRpcProcess.HasNetworkConnectionParameter(method);
            
            int argNum = 1;
            foreach (ParameterDefinition param in method.Parameters)
            {
                if (argNum == 1 && skipFirst)
                {
                    argNum += 1;
                    continue;
                }
                if (IsSenderConnection(param, rpcType))
                {
                    argNum += 1;
                    continue;
                }

                MethodReference writeFunc = writers.GetWriteFunc(param.ParameterType);
                if (writeFunc == null)
                {
                    logger.Error($"{method.Name} 有无效的参数 {param}。不支持类型 {param.ParameterType}。", method);
                    Editor.Injection.failed = true;
                    return false;
                }
                
                worker.Emit(OpCodes.Ldloc_0);
                worker.Emit(OpCodes.Ldarg, argNum);
                worker.Emit(OpCodes.Call, writeFunc);
                argNum += 1;
            }
            return true;
        }
        
        public static bool ReadArguments(MethodDefinition method, Readers readers, Logger logger, ILProcessor worker, RpcType rpcType)
        {
            bool skipFirst = rpcType == RpcType.TargetRpc && TargetRpcProcess.HasNetworkConnectionParameter(method);
            int argNum = 1;
            foreach (ParameterDefinition param in method.Parameters)
            {
                if (argNum == 1 && skipFirst)
                {
                    argNum += 1;
                    continue;
                }
                
                if (IsSenderConnection(param, rpcType))
                {
                    argNum += 1;
                    continue;
                }
                
                MethodReference readFunc = readers.GetReadFunc(param.ParameterType);

                if (readFunc == null)
                {
                    logger.Error($"{method.Name} 有无效的参数 {param}。不支持类型 {param.ParameterType}。", method);
                    Editor.Injection.failed = true;
                    return false;
                }

                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Call, readFunc);
   
                if (param.ParameterType.Is<float>())
                {
                    worker.Emit(OpCodes.Conv_R4);
                }
                else if (param.ParameterType.Is<double>())
                {
                    worker.Emit(OpCodes.Conv_R8);
                }
            }

            return true;
        }

        public static bool IsSenderConnection(ParameterDefinition param, RpcType rpcType)
        {
            if (rpcType != RpcType.ServerRpc)
            {
                return false;
            }

            TypeReference type = param.ParameterType;
            return type.Is<NetworkClientEntity>() || type.Resolve().IsDerivedFrom<NetworkClientEntity>();
        }
        
        public static void WriteSetupLocals(ILProcessor worker, Processor processor)
        {
            worker.Body.InitLocals = true;
            worker.Body.Variables.Add(new VariableDefinition(processor.Import<NetworkWriter>()));
        }

        public static void WriteGetWriter(ILProcessor worker, Processor processor)
        {
            worker.Emit(OpCodes.Call, processor.PopWriterReference);
            worker.Emit(OpCodes.Stloc_0);
        }

        public static void WriteReturnWriter(ILProcessor worker, Processor processor)
        {
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Call, processor.PushWriterReference);
        }


        public static void AddInvokeParameters(Processor processor, ICollection<ParameterDefinition> collection)
        {
            collection.Add(new ParameterDefinition("obj", ParameterAttributes.None, processor.Import<NetworkBehaviour>()));
            collection.Add(new ParameterDefinition("reader", ParameterAttributes.None, processor.Import<NetworkReader>()));
            collection.Add(new ParameterDefinition("target", ParameterAttributes.None, processor.Import<NetworkClientEntity>()));
        }
    }
}