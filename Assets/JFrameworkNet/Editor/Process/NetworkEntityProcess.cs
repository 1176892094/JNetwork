using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    public enum RemoteType
    {
        ServerRpc,
        ClientRpc,
        TargetRpc
    }

    internal class NetworkEntityProcess
    {
        private Writers writers;
        private Readers readers;
        private Processor processor;
        private ServerVarList serverVars;
        private ServerVarProcess serverVarProcess;
        private AssemblyDefinition assembly;
        private Logger logger;
        private TypeDefinition type;
        private List<FieldDefinition> syncVars = new List<FieldDefinition>();
        private Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds = new Dictionary<FieldDefinition, FieldDefinition>();
        private List<FieldDefinition> syncObjects = new List<FieldDefinition>();

        public struct ServerRpcResult
        {
            public MethodDefinition method;
            public ServerRpcResult(MethodDefinition method) => this.method = method;
        }

        public struct ClientRpcResult
        {
            public MethodDefinition method;
            public ClientRpcResult(MethodDefinition method) => this.method = method;
        }
        
        public NetworkEntityProcess(AssemblyDefinition assembly, Processor processor, ServerVarList serverVars, Writers writers, Readers readers, Logger logger, TypeDefinition type)
        {
            this.type = type;
            this.logger = logger;
            this.writers = writers;
            this.readers = readers;
            this.assembly = assembly;
            this.processor = processor;
            this.serverVars = serverVars;
            serverVarProcess = new ServerVarProcess(assembly, processor, serverVars, logger);
        }

        public bool Process(ref bool isFailed)
        {
            if (WasProcessed(type))
            {
                return false;
            }

            MarkAsProcessed(type);
            
            (syncVars, syncVarNetIds) = serverVarProcess.ProcessSyncVars(type, ref isFailed);
            
            return true;
        }

        private static bool WasProcessed(TypeDefinition td)
        {
            return td.GetMethod(CONST.PROCESS_FUNCTION) != null;
        }

        private void MarkAsProcessed(TypeDefinition td)
        {
            if (!WasProcessed(td))
            {
                MethodDefinition versionMethod = new MethodDefinition(CONST.PROCESS_FUNCTION, MethodAttributes.Private, processor.Import(typeof(void)));
                ILProcessor worker = versionMethod.Body.GetILProcessor();
                worker.Emit(OpCodes.Ret);
                td.Methods.Add(versionMethod);
            }
        }
        
        public static void WriteClientActiveCheck(ILProcessor worker, Processor processor, string mdName, Instruction label,string error)
        {
            worker.Emit(OpCodes.Call, processor.NetworkClientGetActive);
            worker.Emit(OpCodes.Brtrue, label);
            worker.Emit(OpCodes.Ldstr, $"{error} {mdName} called on server.");
            worker.Emit(OpCodes.Call, processor.logErrorReference);
            worker.Emit(OpCodes.Ret);
            worker.Append(label);
        }

        public static bool ReadArguments(MethodDefinition method, Readers readers, Logger logger, ILProcessor worker, RemoteType callType, ref bool WeavingFailed)
        {
            bool skipFirst = callType == RemoteType.TargetRpc && TargetRpcProcess.HasNetworkConnectionParameter(method);
            int argNum = 1;
            foreach (ParameterDefinition param in method.Parameters)
            {
                if (argNum == 1 && skipFirst)
                {
                    argNum += 1;
                    continue;
                }
                
                if (IsSenderConnection(param, callType))
                {
                    argNum += 1;
                    continue;
                }
                
                MethodReference readFunc = readers.GetReadFunc(param.ParameterType, ref WeavingFailed);

                if (readFunc == null)
                {
                    logger.Error($"{method.Name} has invalid parameter {param}.  Unsupported type {param.ParameterType},  use a supported Mirror type instead", method);
                    WeavingFailed = true;
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
        
        public static bool IsSenderConnection(ParameterDefinition param, RemoteType callType)
        {
            if (callType != RemoteType.ServerRpc)
            {
                return false;
            }

            TypeReference type = param.ParameterType;
            return type.Is<NetworkClientEntity>() || type.Resolve().IsDerivedFrom<NetworkClientEntity>();
        }
        
        public static void AddInvokeParameters(Processor processor, ICollection<ParameterDefinition> collection)
        {
            collection.Add(new ParameterDefinition("obj", ParameterAttributes.None, processor.Import<NetworkEntity>()));
            collection.Add(new ParameterDefinition("reader", ParameterAttributes.None, processor.Import<NetworkReader>()));
            collection.Add(new ParameterDefinition("target", ParameterAttributes.None, processor.Import<NetworkClientEntity>()));
        }
    }
}