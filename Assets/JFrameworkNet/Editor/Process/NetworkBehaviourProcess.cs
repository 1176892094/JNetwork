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
        private readonly Process process;
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
        
        public NetworkBehaviourProcess(AssemblyDefinition assembly, Process process, SyncVarList syncVarList, Writers writers, Readers readers, Logger logger, TypeDefinition type)
        {
            this.type = type;
            this.logger = logger;
            this.writers = writers;
            this.readers = readers;
            this.assembly = assembly;
            this.process = process;
            this.syncVarList = syncVarList;
            serverVarProcess = new ServerVarProcess(assembly, process, syncVarList, logger);
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
            
            if (Command.failed)
            {
                return true;
            }
            
            InjectIntoStaticConstructor();
            
            GenerateSerialization();
        
            if (Command.failed)
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
                MethodDefinition versionMethod = new MethodDefinition(CONST.PROCESS_FUNC, MethodAttributes.Private, process.Import(typeof(void)));
                ILProcessor worker = versionMethod.Body.GetILProcessor();
                worker.Emit(OpCodes.Ret);
                td.Methods.Add(versionMethod);
            }
        }

        public static void WriteSetupLocals(ILProcessor worker, Process process)
        {
            worker.Body.InitLocals = true;
            worker.Body.Variables.Add(new VariableDefinition(process.Import<NetworkWriter>()));
        }

        public static void WriteGetWriter(ILProcessor worker, Process process)
        {
            worker.Emit(OpCodes.Call, process.PopWriterReference);
            worker.Emit(OpCodes.Stloc_0);
        }

        public static void WriteReturnWriter(ILProcessor worker, Process process)
        {
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Call, process.PushWriterReference);
        }


        public static void AddInvokeParameters(Process process, ICollection<ParameterDefinition> collection)
        {
            collection.Add(new ParameterDefinition("obj", ParameterAttributes.None, process.Import<NetworkBehaviour>()));
            collection.Add(new ParameterDefinition("reader", ParameterAttributes.None, process.Import<NetworkReader>()));
            collection.Add(new ParameterDefinition("target", ParameterAttributes.None, process.Import<ClientEntity>()));
        }
    }
}