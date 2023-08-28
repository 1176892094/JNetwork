using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        private Dictionary<FieldDefinition, FieldDefinition> syncVarIds = new Dictionary<FieldDefinition, FieldDefinition>();
        private List<FieldDefinition> syncVars = new List<FieldDefinition>();
        private readonly Models models;
        private readonly Logger logger;
        private readonly Writers writers;
        private readonly Readers readers;
        private readonly SyncVarAccess access;
        private readonly TypeDefinition type;
        private readonly TypeDefinition generate;
        private readonly SyncVarProcess process;
        private readonly AssemblyDefinition assembly;
        private readonly List<MethodDefinition> serverRpcList = new List<MethodDefinition>();
        private readonly List<MethodDefinition> serverRpcFuncList = new List<MethodDefinition>();
        private readonly List<MethodDefinition> clientRpcList = new List<MethodDefinition>();
        private readonly List<MethodDefinition> clientRpcFuncList = new List<MethodDefinition>();
        private readonly List<MethodDefinition> targetRpcList = new List<MethodDefinition>();
        private readonly List<MethodDefinition> targetRpcFuncList = new List<MethodDefinition>();

        public NetworkBehaviourProcess(AssemblyDefinition assembly,SyncVarAccess access, Models models, Writers writers, Readers readers, Logger logger, TypeDefinition type)
        {
            generate = type;
            this.type = type;
            this.models = models;
            this.access = access;
            this.logger = logger;
            this.writers = writers;
            this.readers = readers;
            this.assembly = assembly;
            process = new SyncVarProcess(assembly,access, models, logger);
        }

        public bool Process(ref bool failed)
        {
            if (type.GetMethod(CONST.GEN_FUNC) != null)
            {
                return false;
            }

            MarkAsProcessed(type);

            (syncVars, syncVarIds) = process.ProcessSyncVars(type, ref failed);

            ProcessRpcMethods(ref failed);

            if (failed)
            {
                return true;
            }

            InjectStaticConstructor(ref failed);

            GenerateSerialize(ref failed);

            if (failed)
            {
                return true;
            }

            GenerateDeserialize(ref failed);
            return true;
        }

        private void MarkAsProcessed(TypeDefinition td)
        {
            var versionMethod = new MethodDefinition(CONST.GEN_FUNC, MethodAttributes.Private, models.Import(typeof(void)));
            var worker = versionMethod.Body.GetILProcessor();
            worker.Emit(OpCodes.Ret);
            td.Methods.Add(versionMethod);
        }

        public static void WriteInitLocals(ILProcessor worker, Models models)
        {
            worker.Body.InitLocals = true;
            worker.Body.Variables.Add(new VariableDefinition(models.Import<NetworkWriter>()));
        }

        public static void WritePopWriter(ILProcessor worker, Models models)
        {
            worker.Emit(OpCodes.Call, models.PopWriterRef);
            worker.Emit(OpCodes.Stloc_0);
        }

        public static void WritePushWriter(ILProcessor worker, Models models)
        {
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Call, models.PushWriterRef);
        }

        public static void AddInvokeParameters(Models models, ICollection<ParameterDefinition> collection)
        {
            collection.Add(new ParameterDefinition("obj", ParameterAttributes.None, models.Import<NetworkBehaviour>()));
            collection.Add(new ParameterDefinition("reader", ParameterAttributes.None, models.Import<NetworkReader>()));
            collection.Add(new ParameterDefinition("target", ParameterAttributes.None, models.Import<UdpClient>()));
        }
    }
}