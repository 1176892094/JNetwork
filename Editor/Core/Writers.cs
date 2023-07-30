using System;
using System.Collections.Generic;
using System.Reflection;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;
using Object = UnityEngine.Object;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace JFramework.Editor
{
    internal class Writers
    {
        private readonly Dictionary<TypeReference, MethodReference> writeFuncList =
            new Dictionary<TypeReference, MethodReference>(new Comparer());

        private readonly Models models;
        private readonly Logger logger;
        private readonly TypeDefinition generate;
        private readonly AssemblyDefinition assembly;

        public Writers(AssemblyDefinition assembly, Models models, TypeDefinition generate, Logger logger)
        {
            this.assembly = assembly;
            this.models = models;
            this.generate = generate;
            this.logger = logger;
        }

        public void Register(TypeReference dataType, MethodReference md)
        {
            TypeReference imported = assembly.MainModule.ImportReference(dataType);
            writeFuncList[imported] = md;
        }

        private void RegisterWriteFunc(TypeReference tr, MethodDefinition func)
        {
            Register(tr, func);
            generate.Methods.Add(func);
        }

        public MethodReference GetWriteFunc(TypeReference variable, ref bool failed)
        {
            if (writeFuncList.TryGetValue(variable, out MethodReference func))
            {
                return func;
            }

            TypeReference importedVariable = assembly.MainModule.ImportReference(variable);
            return GenerateWriter(importedVariable, ref failed);
        }

        private MethodReference GenerateWriter(TypeReference variableRef, ref bool failed)
        {
            if (variableRef.IsArray)
            {
                if (variableRef.IsMultidimensionalArray())
                {
                    logger.Error($"无法为多维数组 {variableRef.Name} 生成 Writer", variableRef);
                }

                TypeReference elementType = variableRef.GetElementType();
                return GenerateCollectionWriter(variableRef, elementType, nameof(StreamExtensions.WriteArray),
                    ref failed);
            }

            if (variableRef.IsByReference)
            {
                logger.Error($"无法为反射 {variableRef.Name} 生成 Writer", variableRef);
            }

            if (variableRef.Resolve()?.IsEnum ?? false)
            {
                return GenerateEnumWriteFunc(variableRef, ref failed);
            }

            if (variableRef.Is(typeof(ArraySegment<>)))
            {
                GenericInstanceType genericInstance = (GenericInstanceType)variableRef;
                TypeReference elementType = genericInstance.GenericArguments[0];

                return GenerateCollectionWriter(variableRef, elementType, nameof(StreamExtensions.WriteArraySegment),
                    ref failed);
            }

            if (variableRef.Is(typeof(List<>)))
            {
                GenericInstanceType genericInstance = (GenericInstanceType)variableRef;
                TypeReference elementType = genericInstance.GenericArguments[0];

                return GenerateCollectionWriter(variableRef, elementType, nameof(StreamExtensions.WriteList),
                    ref failed);
            }

            if (variableRef.IsDerivedFrom<NetworkBehaviour>() || variableRef.Is<NetworkBehaviour>())
            {
                return GetNetworkBehaviourWriter(variableRef);
            }

            TypeDefinition variableDefinition = variableRef.Resolve();
            if (variableDefinition == null)
            {
                logger.Error($"无法为Null {variableRef.Name} 生成 Writer", variableRef);
                return null;
            }

            if (variableDefinition.IsDerivedFrom<Component>())
            {
                logger.Error($"无法为组件 {variableRef.Name} 生成 Writer", variableRef);
                return null;
            }

            if (variableRef.Is<Object>())
            {
                logger.Error($"无法为对象 {variableRef.Name} 生成 Writer", variableRef);
                return null;
            }

            if (variableRef.Is<ScriptableObject>())
            {
                logger.Error($"无法为可视化脚本 {variableRef.Name} 生成 Writer", variableRef);
                return null;
            }

            if (variableDefinition.HasGenericParameters)
            {
                logger.Error($"无法为通用变量 {variableRef.Name} 生成 Writer", variableRef);
                return null;
            }

            if (variableDefinition.IsInterface)
            {
                logger.Error($"无法为接口 {variableRef.Name} 生成 Writer", variableRef);
                return null;
            }

            if (variableDefinition.IsAbstract)
            {
                logger.Error($"无法为抽象类 {variableRef.Name} 生成 Writer", variableRef);
                return null;
            }

            return GenerateClassOrStructWriterFunction(variableRef, ref failed);
        }

        private MethodReference GetNetworkBehaviourWriter(TypeReference variableReference)
        {
            if (writeFuncList.TryGetValue(models.Import<NetworkBehaviour>(), out MethodReference func))
            {
                Register(variableReference, func);
                return func;
            }

            throw new MissingMethodException($"无法从 NetworkBehaviour 获取 Writer");
        }

        private MethodDefinition GenerateEnumWriteFunc(TypeReference variable, ref bool failed)
        {
            MethodDefinition writerFunc = GenerateWriterFunc(variable);

            ILProcessor worker = writerFunc.Body.GetILProcessor();

            MethodReference underlyingWriter = GetWriteFunc(variable.Resolve().GetEnumUnderlyingType(), ref failed);

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, underlyingWriter);

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private MethodDefinition GenerateWriterFunc(TypeReference variable)
        {
            string functionName = $"Write{NetworkMessage.GetHashByName(variable.FullName)}";
            MethodDefinition writerFunc =
                new MethodDefinition(functionName, CONST.RAW_ATTRS, models.Import(typeof(void)));
            writerFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None,
                models.Import<NetworkWriter>()));
            writerFunc.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, variable));
            writerFunc.Body.InitLocals = true;

            RegisterWriteFunc(variable, writerFunc);
            return writerFunc;
        }

        private MethodDefinition GenerateClassOrStructWriterFunction(TypeReference variable, ref bool failed)
        {
            MethodDefinition writerFunc = GenerateWriterFunc(variable);

            ILProcessor worker = writerFunc.Body.GetILProcessor();

            if (!variable.Resolve().IsValueType)
            {
                WriteNullCheck(worker, ref failed);
            }

            if (!WriteAllFields(variable, worker, ref failed))
            {
                return null;
            }

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private void WriteNullCheck(ILProcessor worker, ref bool failed)
        {
            Instruction labelNotNull = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Brtrue, labelNotNull);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_0);
            worker.Emit(OpCodes.Call, GetWriteFunc(models.Import<bool>(), ref failed));
            worker.Emit(OpCodes.Ret);
            worker.Append(labelNotNull);

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_1);
            worker.Emit(OpCodes.Call, GetWriteFunc(models.Import<bool>(), ref failed));
        }

        private bool WriteAllFields(TypeReference variable, ILProcessor worker, ref bool failed)
        {
            foreach (FieldDefinition field in variable.FindAllPublicFields())
            {
                MethodReference writeFunc = GetWriteFunc(field.FieldType, ref failed);
                if (writeFunc == null)
                {
                    return false;
                }

                FieldReference fieldRef = assembly.MainModule.ImportReference(field);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldfld, fieldRef);
                worker.Emit(OpCodes.Call, writeFunc);
            }

            return true;
        }

        private MethodDefinition GenerateCollectionWriter(TypeReference variable, TypeReference elementType,
            string writerFunction, ref bool failed)
        {
            MethodDefinition writerFunc = GenerateWriterFunc(variable);
            MethodReference elementWriteFunc = GetWriteFunc(elementType, ref failed);

            if (elementWriteFunc == null)
            {
                logger.Error($"无法为 {variable} 生成 Writer", variable);
                failed = true;
                return writerFunc;
            }

            ModuleDefinition module = assembly.MainModule;
            TypeReference readerExtensions = module.ImportReference(typeof(StreamExtensions));
            MethodReference collectionWriter = Utils.ResolveMethod(readerExtensions, assembly, logger, writerFunction,ref failed);

            GenericInstanceMethod methodRef = new GenericInstanceMethod(collectionWriter);
            methodRef.GenericArguments.Add(elementType);

            ILProcessor worker = writerFunc.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, methodRef);
            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        internal void InitializeWriters(ILProcessor worker)
        {
            ModuleDefinition module = assembly.MainModule;
            TypeReference genericWriterClassRef = module.ImportReference(typeof(Writer<>));
            FieldInfo fieldInfo = typeof(Writer<>).GetField(nameof(Writer<object>.write));
            FieldReference fieldRef = module.ImportReference(fieldInfo);
            TypeReference networkWriterRef = module.ImportReference(typeof(NetworkWriter));
            TypeReference actionRef = module.ImportReference(typeof(Action<,>));
            MethodReference actionConstructorRef = module.ImportReference(typeof(Action<,>).GetConstructors()[0]);

            foreach (var (type, method) in writeFuncList)
            {
                worker.Emit(OpCodes.Ldnull);
                worker.Emit(OpCodes.Ldftn, method);
                GenericInstanceType actionGenericInstance = actionRef.MakeGenericInstanceType(networkWriterRef, type);
                MethodReference actionRefInstance =
                    actionConstructorRef.MakeHostInstanceGeneric(assembly.MainModule, actionGenericInstance);
                worker.Emit(OpCodes.Newobj, actionRefInstance);
                GenericInstanceType genericInstance = genericWriterClassRef.MakeGenericInstanceType(type);
                FieldReference specializedField = fieldRef.SpecializeField(assembly.MainModule, genericInstance);
                worker.Emit(OpCodes.Stsfld, specializedField);
            }
        }
    }
}