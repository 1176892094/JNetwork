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
        private readonly Model model;
        private readonly TypeDefinition type;
        private readonly SyncVarProcess syncVarProcess;
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
        
        public NetworkBehaviourProcess(AssemblyDefinition assembly, Model model, Writers writers, Readers readers, Logger logger, TypeDefinition type)
        {
            this.type = type;
            this.logger = logger;
            this.writers = writers;
            this.readers = readers;
            this.assembly = assembly;
            this.model = model;
            syncVarProcess = new SyncVarProcess(assembly, model, logger);
            generateCode = this.type;
        }

        public bool Process()
        {
            if (WasProcessed(type))
            {
                return false;
            }
            
            MarkAsProcessed(type);
            
            (syncVars, syncVarNetIds) = syncVarProcess.ProcessSyncVars(type);
           
            ProcessRpcMethods();
            
            if (Editor.Process.failed)
            {
                return true;
            }
            
            InjectStaticConstructor();
            
            GenerateSerialize();
        
            if (Editor.Process.failed)
            {
                return true;
            }

            GenerateDeserialize();
            return true;
        }

        private static bool WasProcessed(TypeDefinition td)
        {
            return td.GetMethod(CONST.GEN_FUNC) != null;
        }

        private void MarkAsProcessed(TypeDefinition td)
        {
            if (!WasProcessed(td))
            {
                MethodDefinition versionMethod = new MethodDefinition(CONST.GEN_FUNC, MethodAttributes.Private, model.Import(typeof(void)));
                ILProcessor worker = versionMethod.Body.GetILProcessor();
                worker.Emit(OpCodes.Ret);
                td.Methods.Add(versionMethod);
            }
        }

        public static void WriteSetupLocals(ILProcessor worker, Model model)
        {
            worker.Body.InitLocals = true;
            worker.Body.Variables.Add(new VariableDefinition(model.Import<NetworkWriter>()));
        }

        public static void WriteGetWriter(ILProcessor worker, Model model)
        {
            worker.Emit(OpCodes.Call, model.PopWriterReference);
            worker.Emit(OpCodes.Stloc_0);
        }

        public static void WriteReturnWriter(ILProcessor worker, Model model)
        {
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Call, model.PushWriterReference);
        }


        public static void AddInvokeParameters(Model model, ICollection<ParameterDefinition> collection)
        {
            collection.Add(new ParameterDefinition("obj", ParameterAttributes.None, model.Import<NetworkBehaviour>()));
            collection.Add(new ParameterDefinition("reader", ParameterAttributes.None, model.Import<NetworkReader>()));
            collection.Add(new ParameterDefinition("target", ParameterAttributes.None, model.Import<ClientEntity>()));
        }
    }
}