using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        private readonly Logger logger;
        private readonly Writers writers;
        private readonly Readers readers;
        private readonly Processor processor;
        private readonly SyncVarList syncVarList;
        private readonly TypeDefinition type;
        private readonly ServerVarProcess serverVarProcess;
        private readonly AssemblyDefinition assembly;
        private readonly List<ServerRpcResult> serverRpcList = new List<ServerRpcResult>();
        private readonly List<MethodDefinition> serverRpcFuncList = new List<MethodDefinition>();
        private readonly List<ClientRpcResult> clientRpcList = new List<ClientRpcResult>();
        private readonly List<MethodDefinition> clientRpcFuncList = new List<MethodDefinition>();
        private readonly List<MethodDefinition> targetRpcList = new List<MethodDefinition>();
        private readonly List<MethodDefinition> targetRpcFuncList = new List<MethodDefinition>();
        private List<FieldDefinition> syncVars = new List<FieldDefinition>();
        private Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds = new Dictionary<FieldDefinition, FieldDefinition>();

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
            generateCode = this.type;
        }

        public bool Process()
        {
            if (WasProcessed(type))
            {
                return false;
            }
            
            MarkAsProcessed(type);
            
            (syncVars, syncVarNetIds) = serverVarProcess.ProcessSyncVars(type);
           
            ProcessMethods();
            
            if (Injection.failed)
            {
                return true;
            }
            
            InjectIntoStaticConstructor();
            
            GenerateSerialization();
        
            if (Injection.failed)
            {
                return true;
            }

            GenerateDeserialization();
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

        public static void NetworkClientActive(ILProcessor worker, Processor processor, string mdName, Instruction label, string error)
        {
            worker.Emit(OpCodes.Call, processor.NetworkClientGetActive);
            worker.Emit(OpCodes.Brtrue, label);
            worker.Emit(OpCodes.Ldstr, $"{error} 方法 {mdName} 远程调用服务器异常。");
            worker.Emit(OpCodes.Call, processor.logErrorReference);
            worker.Emit(OpCodes.Ret);
            worker.Append(label);
        }

        public static void NetworkServerActive(ILProcessor worker, Processor processor, string mdName, Instruction label, string error)
        {
            worker.Emit(OpCodes.Call, processor.NetworkServerGetActive);
            worker.Emit(OpCodes.Brtrue, label);

            worker.Emit(OpCodes.Ldstr, $"{error} 方法 {mdName} 远程调用客户端异常。");
            worker.Emit(OpCodes.Call, processor.logErrorReference);
            worker.Emit(OpCodes.Ret);
            worker.Append(label);
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
            collection.Add(new ParameterDefinition("target", ParameterAttributes.None, processor.Import<ClientEntity>()));
        }
    }
}