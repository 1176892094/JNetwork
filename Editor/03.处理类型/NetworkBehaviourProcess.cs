// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-06-06  05:06
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        private Dictionary<FieldDefinition, FieldDefinition> syncVarIds = new Dictionary<FieldDefinition, FieldDefinition>();
        private List<FieldDefinition> syncVars = new List<FieldDefinition>();
        private readonly Models models;
        private readonly Logger logger;
        private readonly Writer writers;
        private readonly Reader readers;
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

        public NetworkBehaviourProcess(AssemblyDefinition assembly, SyncVarAccess access, Models models, Writer writers, Reader readers, Logger logger, TypeDefinition type)
        {
            generate = type;
            this.type = type;
            this.models = models;
            this.access = access;
            this.logger = logger;
            this.writers = writers;
            this.readers = readers;
            this.assembly = assembly;
            process = new SyncVarProcess(assembly, access, models, logger);
        }

        public bool Process(ref bool failed)
        {
            if (type.GetMethod(Const.GEN_FUNC) != null)
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
            var versionMethod = new MethodDefinition(Const.GEN_FUNC, MethodAttributes.Private, models.Import(typeof(void)));
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
            collection.Add(new ParameterDefinition("target", ParameterAttributes.None, models.Import<NetworkClient>()));
        }
    }

    internal partial class NetworkBehaviourProcess
    {
        /// <summary>
        /// 处理Rpc方法
        /// </summary>
        private void ProcessRpcMethods(ref bool failed)
        {
            var names = new HashSet<string>();
            var methods = new List<MethodDefinition>(generate.Methods);

            foreach (var md in methods)
            {
                foreach (var ca in md.CustomAttributes)
                {
                    if (ca.AttributeType.Is<ServerRpcAttribute>())
                    {
                        ProcessServerRpc(names, md, ca, ref failed);
                        break;
                    }

                    if (ca.AttributeType.Is<TargetRpcAttribute>())
                    {
                        ProcessTargetRpc(names, md, ca, ref failed);
                        break;
                    }

                    if (ca.AttributeType.Is<ClientRpcAttribute>())
                    {
                        ProcessClientRpc(names, md, ca, ref failed);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 处理ClientRpc
        /// </summary>
        /// <param name="names"></param>
        /// <param name="md"></param>
        /// <param name="rpc"></param>
        /// <param name="failed"></param>
        private void ProcessClientRpc(HashSet<string> names, MethodDefinition md, CustomAttribute rpc, ref bool failed)
        {
            if (md.IsAbstract)
            {
                logger.Error("ClientRpc不能作用在抽象方法中。", md);
                failed = true;
                return;
            }

            if (!IsValidMethod(md, InvokeMode.ClientRpc, ref failed))
            {
                return;
            }

            names.Add(md.Name);
            clientRpcList.Add(md);
            var func = NetworkDelegateProcess.ProcessClientRpcInvoke(models, writers, logger, generate, md, rpc, ref failed);
            if (func == null) return;
            var rpcFunc = NetworkDelegateProcess.ProcessClientRpc(models, readers, logger, generate, md, func, ref failed);
            if (rpcFunc != null)
            {
                clientRpcFuncList.Add(rpcFunc);
            }
        }

        /// <summary>
        /// 处理ServerRpc
        /// </summary>
        /// <param name="names"></param>
        /// <param name="md"></param>
        /// <param name="rpc"></param>
        /// <param name="failed"></param>
        private void ProcessServerRpc(HashSet<string> names, MethodDefinition md, CustomAttribute rpc, ref bool failed)
        {
            if (md.IsAbstract)
            {
                logger.Error("ServerRpc不能作用在抽象方法中。", md);
                failed = true;
                return;
            }

            if (!IsValidMethod(md, InvokeMode.ServerRpc, ref failed))
            {
                return;
            }

            names.Add(md.Name);
            serverRpcList.Add(md);
            var func = NetworkDelegateProcess.ProcessServerRpcInvoke(models, writers, logger, generate, md, rpc, ref failed);
            if (func == null) return;
            var rpcFunc = NetworkDelegateProcess.ProcessServerRpc(models, readers, logger, generate, md, func, ref failed);
            if (rpcFunc != null)
            {
                serverRpcFuncList.Add(rpcFunc);
            }
        }

        /// <summary>
        /// 处理TargetRpc
        /// </summary>
        /// <param name="names"></param>
        /// <param name="md"></param>
        /// <param name="rpc"></param>
        /// <param name="failed"></param>
        private void ProcessTargetRpc(HashSet<string> names, MethodDefinition md, CustomAttribute rpc, ref bool failed)
        {
            if (md.IsAbstract)
            {
                logger.Error("TargetRpc不能作用在抽象方法中。", md);
                failed = true;
                return;
            }

            if (!IsValidMethod(md, InvokeMode.TargetRpc, ref failed))
            {
                return;
            }

            names.Add(md.Name);
            targetRpcList.Add(md);
            var func = NetworkDelegateProcess.ProcessTargetRpcInvoke(models, writers, logger, generate, md, rpc, ref failed);
            var rpcFunc = NetworkDelegateProcess.ProcessTargetRpc(models, readers, logger, generate, md, func, ref failed);
            if (rpcFunc != null)
            {
                targetRpcFuncList.Add(rpcFunc);
            }
        }

        /// <summary>
        /// 判断是否为非静态方法
        /// </summary>
        /// <param name="md"></param>
        /// <param name="rpcType"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidMethod(MethodDefinition md, InvokeMode rpcType, ref bool failed)
        {
            if (md.IsStatic)
            {
                logger.Error($"{md.Name} 方法不能是静态的。", md);
                failed = true;
                return false;
            }

            return IsValidFunc(md, ref failed) && IsValidParams(md, rpcType, ref failed);
        }

        /// <summary>
        /// 判断是否为有效Rpc
        /// </summary>
        /// <param name="mr"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidFunc(MethodReference mr, ref bool failed)
        {
            if (!mr.ReturnType.Is(typeof(void)))
            {
                logger.Error($"{mr.Name} 方法不能有返回值。", mr);
                failed = true;
                return false;
            }

            if (mr.HasGenericParameters)
            {
                logger.Error($"{mr.Name} 方法不能有泛型参数。", mr);
                failed = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断Rpc携带的参数
        /// </summary>
        /// <param name="mr"></param>
        /// <param name="rpcType"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidParams(MethodReference mr, InvokeMode rpcType, ref bool failed)
        {
            for (int i = 0; i < mr.Parameters.Count; ++i)
            {
                ParameterDefinition param = mr.Parameters[i];
                if (!IsValidParam(mr, param, rpcType, i == 0, ref failed))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断Rpc是否为有效参数
        /// </summary>
        /// <param name="method"></param>
        /// <param name="param"></param>
        /// <param name="rpcType"></param>
        /// <param name="firstParam"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidParam(MethodReference method, ParameterDefinition param, InvokeMode rpcType, bool firstParam, ref bool failed)
        {
            if (param.ParameterType.IsGenericParameter)
            {
                logger.Error($"{method.Name} 方法不能有泛型参数。", method);
                failed = true;
                return false;
            }

            bool connection = param.ParameterType.Is<NetworkClient>();
            bool sendTarget = NetworkDelegateProcess.IsNetworkClient(param, rpcType);

            if (param.IsOut)
            {
                logger.Error($"{method.Name} 方法不能携带 out 关键字。", method);
                failed = true;
                return false;
            }

            if (!sendTarget && connection && !(rpcType == InvokeMode.TargetRpc && firstParam))
            {
                logger.Error($"{method.Name} 方法无效的参数 {param}，不能传递网络连接。", method);
                failed = true;
                return false;
            }

            if (param.IsOptional && !sendTarget)
            {
                logger.Error($"{method.Name} 方法不能有可选参数。", method);
                failed = true;
                return false;
            }

            return true;
        }
    }

    internal partial class NetworkBehaviourProcess
    {
        /// <summary>
        /// 注入静态构造函数
        /// </summary>
        private void InjectStaticConstructor(ref bool failed)
        {
            if (serverRpcList.Count == 0 && clientRpcList.Count == 0 && targetRpcList.Count == 0) return;
            MethodDefinition cctor = generate.GetMethod(".cctor");
            if (cctor != null)
            {
                if (!RemoveFinalRetInstruction(cctor))
                {
                    logger.Error($"{generate.Name} 无效的静态构造函数。", cctor);
                    failed = true;
                    return;
                }
            }
            else
            {
                cctor = new MethodDefinition(".cctor", Const.CTOR_ATTRS, models.Import(typeof(void)));
            }

            ILProcessor worker = cctor.Body.GetILProcessor();
            for (int i = 0; i < serverRpcList.Count; ++i)
            {
                GenerateServerRpcDelegate(worker, models.registerServerRpcRef, serverRpcFuncList[i], serverRpcList[i].FullName);
            }

            for (int i = 0; i < clientRpcList.Count; ++i)
            {
                GenerateClientRpcDelegate(worker, models.registerClientRpcRef, clientRpcFuncList[i], clientRpcList[i].FullName);
            }

            for (int i = 0; i < targetRpcList.Count; ++i)
            {
                GenerateClientRpcDelegate(worker, models.registerClientRpcRef, targetRpcFuncList[i], targetRpcList[i].FullName);
            }

            worker.Append(worker.Create(OpCodes.Ret));
            generate.Methods.Add(cctor);
            generate.Attributes &= ~TypeAttributes.BeforeFieldInit;
        }

        /// <summary>
        /// 判断自身静态构造函数是否被创建
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        private static bool RemoveFinalRetInstruction(MethodDefinition md)
        {
            if (md.Body.Instructions.Count != 0)
            {
                Instruction retInstr = md.Body.Instructions[^1];
                if (retInstr.OpCode == OpCodes.Ret)
                {
                    md.Body.Instructions.RemoveAt(md.Body.Instructions.Count - 1);
                    return true;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 在静态构造函数中注入ClientRpc委托
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="mr"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        private void GenerateClientRpcDelegate(ILProcessor worker, MethodReference mr, MethodDefinition md, string func)
        {
            worker.Emit(OpCodes.Ldtoken, generate);
            worker.Emit(OpCodes.Call, models.getTypeFromHandleRef);
            worker.Emit(OpCodes.Ldstr, func);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, md);
            worker.Emit(OpCodes.Newobj, models.RpcDelegateRef);
            worker.Emit(OpCodes.Call, mr);
        }

        /// <summary>
        /// 在静态构造函数中注入ServerRpc委托
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="mr"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        private void GenerateServerRpcDelegate(ILProcessor worker, MethodReference mr, MethodDefinition md, string func)
        {
            worker.Emit(OpCodes.Ldtoken, generate);
            worker.Emit(OpCodes.Call, models.getTypeFromHandleRef);
            worker.Emit(OpCodes.Ldstr, func);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, md);
            worker.Emit(OpCodes.Newobj, models.RpcDelegateRef);
            worker.Emit(OpCodes.Call, mr);
        }
    }

    internal partial class NetworkBehaviourProcess
    {
        /// <summary>
        /// 生成SyncVar的序列化方法
        /// </summary>
        private void GenerateSerialize(ref bool failed)
        {
            if (generate.GetMethod(Const.SER_METHOD) != null) return;
            if (syncVars.Count == 0) return;
            var serialize = new MethodDefinition(Const.SER_METHOD, Const.SER_ATTRS, models.Import(typeof(void)));
            serialize.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, models.Import<NetworkWriter>()));
            serialize.Parameters.Add(new ParameterDefinition("start", ParameterAttributes.None, models.Import<bool>()));
            var worker = serialize.Body.GetILProcessor();

            serialize.Body.InitLocals = true;
            var baseSerialize = Helper.TryResolveMethodInParents(generate.BaseType, assembly, Const.SER_METHOD);
            if (baseSerialize != null)
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_2);
                worker.Emit(OpCodes.Call, baseSerialize);
            }

            Instruction isStart = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_2);
            worker.Emit(OpCodes.Brfalse, isStart);
            foreach (var syncVarDef in syncVars)
            {
                FieldReference syncVar = syncVarDef;
                if (generate.HasGenericParameters)
                {
                    syncVar = syncVarDef.MakeHostInstanceGeneric();
                }

                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, syncVar);
                var writeFunc =
                    writers.GetFunction(
                        syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() ? models.Import<NetworkBehaviour>() : syncVar.FieldType,
                        ref failed);

                if (writeFunc != null)
                {
                    worker.Emit(OpCodes.Call, writeFunc);
                }
                else
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型", syncVar);
                    failed = true;
                    return;
                }
            }

            worker.Emit(OpCodes.Ret);
            worker.Append(isStart);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, models.NetworkBehaviourDirtyRef);
            var writeUint64Func = writers.GetFunction(models.Import<ulong>(), ref failed);
            worker.Emit(OpCodes.Call, writeUint64Func);
            int dirty = access.GetSyncVar(generate.BaseType.FullName);
            foreach (var syncVarDef in syncVars)
            {
                FieldReference syncVar = syncVarDef;
                if (generate.HasGenericParameters)
                {
                    syncVar = syncVarDef.MakeHostInstanceGeneric();
                }

                var varLabel = worker.Create(OpCodes.Nop);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Call, models.NetworkBehaviourDirtyRef);
                worker.Emit(OpCodes.Ldc_I8, 1L << dirty);
                worker.Emit(OpCodes.And);
                worker.Emit(OpCodes.Brfalse, varLabel);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, syncVar);

                var writeFunc =
                    writers.GetFunction(
                        syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() ? models.Import<NetworkBehaviour>() : syncVar.FieldType,
                        ref failed);

                if (writeFunc != null)
                {
                    worker.Emit(OpCodes.Call, writeFunc);
                }
                else
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型", syncVar);
                    failed = true;
                    return;
                }

                worker.Append(varLabel);
                dirty += 1;
            }

            worker.Emit(OpCodes.Ret);
            generate.Methods.Add(serialize);
        }

        /// <summary>
        /// 生成SyncVar的反序列化方法
        /// </summary>
        private void GenerateDeserialize(ref bool failed)
        {
            if (generate.GetMethod(Const.DES_METHOD) != null) return;
            if (syncVars.Count == 0) return;
            var serialize = new MethodDefinition(Const.DES_METHOD, Const.SER_ATTRS, models.Import(typeof(void)));
            serialize.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, models.Import<NetworkReader>()));
            serialize.Parameters.Add(new ParameterDefinition("start", ParameterAttributes.None, models.Import<bool>()));
            var worker = serialize.Body.GetILProcessor();

            serialize.Body.InitLocals = true;
            var dirtyBitsLocal = new VariableDefinition(models.Import<long>());
            serialize.Body.Variables.Add(dirtyBitsLocal);

            var baseDeserialize = Helper.TryResolveMethodInParents(generate.BaseType, assembly, Const.DES_METHOD);
            if (baseDeserialize != null)
            {
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldarg_1));
                worker.Append(worker.Create(OpCodes.Ldarg_2));
                worker.Append(worker.Create(OpCodes.Call, baseDeserialize));
            }

            var isStart = worker.Create(OpCodes.Nop);

            worker.Append(worker.Create(OpCodes.Ldarg_2));
            worker.Append(worker.Create(OpCodes.Brfalse, isStart));

            foreach (var syncVar in syncVars)
            {
                DeserializeField(syncVar, worker, ref failed);
            }

            worker.Append(worker.Create(OpCodes.Ret));
            worker.Append(isStart);
            worker.Append(worker.Create(OpCodes.Ldarg_1));
            worker.Append(worker.Create(OpCodes.Call, readers.GetFunction(models.Import<ulong>(), ref failed)));
            worker.Append(worker.Create(OpCodes.Stloc_0));

            int dirtyBits = access.GetSyncVar(generate.BaseType.FullName);
            foreach (var syncVar in syncVars)
            {
                var varLabel = worker.Create(OpCodes.Nop);
                worker.Append(worker.Create(OpCodes.Ldloc_0));
                worker.Append(worker.Create(OpCodes.Ldc_I8, 1L << dirtyBits));
                worker.Append(worker.Create(OpCodes.And));
                worker.Append(worker.Create(OpCodes.Brfalse, varLabel));

                DeserializeField(syncVar, worker, ref failed);

                worker.Append(varLabel);
                dirtyBits += 1;
            }

            worker.Append(worker.Create(OpCodes.Ret));
            generate.Methods.Add(serialize);
        }

        /// <summary>
        /// 反序列化字段
        /// </summary>
        /// <param name="syncVar"></param>
        /// <param name="worker"></param>
        /// <param name="failed"></param>
        private void DeserializeField(FieldDefinition syncVar, ILProcessor worker, ref bool failed)
        {
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldflda, generate.HasGenericParameters ? syncVar.MakeHostInstanceGeneric() : syncVar);

            var hookMethod = process.GetHookMethod(generate, syncVar, ref failed);
            if (hookMethod != null)
            {
                process.GenerateNewActionFromHookMethod(syncVar, worker, hookMethod);
            }
            else
            {
                worker.Emit(OpCodes.Ldnull);
            }

            if (syncVar.FieldType.Is<GameObject>())
            {
                var objectId = syncVarIds[syncVar];
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, objectId);
                worker.Emit(OpCodes.Call, models.syncVarGetterGameObject);
            }
            else if (syncVar.FieldType.Is<NetworkObject>())
            {
                var objectId = syncVarIds[syncVar];
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, objectId);
                worker.Emit(OpCodes.Call, models.syncVarGetterNetworkObject);
            }
            else if (syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() || syncVar.FieldType.Is<NetworkBehaviour>())
            {
                var objectId = syncVarIds[syncVar];
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, objectId);
                var getFunc = models.syncVarGetterNetworkBehaviour.MakeGeneric(assembly.MainModule, syncVar.FieldType);
                worker.Emit(OpCodes.Call, getFunc);
            }
            else
            {
                var readFunc = readers.GetFunction(syncVar.FieldType, ref failed);
                if (readFunc == null)
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型。", syncVar);
                    failed = true;
                    return;
                }

                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Call, readFunc);
                MethodReference generic = models.syncVarGetterGeneral.MakeGeneric(assembly.MainModule, syncVar.FieldType);
                worker.Emit(OpCodes.Call, generic);
            }
        }
    }
}