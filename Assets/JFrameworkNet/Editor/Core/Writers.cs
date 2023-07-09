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
        private readonly Dictionary<TypeReference, MethodReference> writeFuncList = new Dictionary<TypeReference, MethodReference>(new Comparer());
        private readonly AssemblyDefinition assembly;
        private readonly Logger logger;
        private readonly Processor processor;
        private readonly TypeDefinition generate;

        public Writers(AssemblyDefinition assembly, Processor processor, TypeDefinition generate, Logger logger)
        {
            this.assembly = assembly;
            this.processor = processor;
            this.generate = generate;
            this.logger = logger;
        }

        public void Register(TypeReference dataType, MethodReference methodReference)
        {
            TypeReference imported = assembly.MainModule.ImportReference(dataType);
            writeFuncList[imported] = methodReference;
        }

        private void RegisterWriteFunc(TypeReference typeReference, MethodDefinition newWriterFunc)
        {
            Register(typeReference, newWriterFunc);
            generate.Methods.Add(newWriterFunc);
        }
        
        public MethodReference GetWriteFunc(TypeReference variable, ref bool isFailed)
        {
            if (writeFuncList.TryGetValue(variable, out MethodReference foundFunc))
            {
                return foundFunc;
            }
            
            TypeReference importedVariable = assembly.MainModule.ImportReference(variable);
            return GenerateWriter(importedVariable, ref isFailed);
        }

        private MethodReference GenerateWriter(TypeReference variableReference, ref bool isFailed)
        {
            if (variableReference.IsArray)
            {
                if (variableReference.IsMultidimensionalArray())
                {
                    logger.Error($"无法为多维数组 {variableReference.Name} 生成 Writer", variableReference);
                }
                TypeReference elementType = variableReference.GetElementType();
                return GenerateCollectionWriter(variableReference, elementType, nameof(StreamExtensions.WriteArray), ref isFailed);
            }
            
            if (variableReference.IsByReference)
            {
                logger.Error($"无法为反射 {variableReference.Name} 生成 Writer", variableReference);
            }

            if (variableReference.Resolve()?.IsEnum ?? false)
            {
                return GenerateEnumWriteFunc(variableReference, ref isFailed);
            }
            
            if (variableReference.Is(typeof(ArraySegment<>)))
            {
                GenericInstanceType genericInstance = (GenericInstanceType)variableReference;
                TypeReference elementType = genericInstance.GenericArguments[0];

                return GenerateCollectionWriter(variableReference, elementType, nameof(StreamExtensions.WriteArraySegment), ref isFailed);
            }
            if (variableReference.Is(typeof(List<>)))
            {
                GenericInstanceType genericInstance = (GenericInstanceType)variableReference;
                TypeReference elementType = genericInstance.GenericArguments[0];

                return GenerateCollectionWriter(variableReference, elementType, nameof(StreamExtensions.WriteList), ref isFailed);
            }
            
            if (variableReference.IsDerivedFrom<NetworkEntity>() || variableReference.Is<NetworkEntity>())
            {
                return GetNetworkBehaviourWriter(variableReference);
            }
            
            TypeDefinition variableDefinition = variableReference.Resolve();
            if (variableDefinition == null)
            {
                logger.Error($"无法为Null {variableReference.Name} 生成 Writer", variableReference);
                return null;
            }
            if (variableDefinition.IsDerivedFrom<Component>())
            {
                logger.Error($"无法为组件 {variableReference.Name} 生成 Writer", variableReference);
                return null;
            }
            if (variableReference.Is<Object>())
            {
                logger.Error($"无法为对象 {variableReference.Name} 生成 Writer", variableReference);
                return null;
            }
            if (variableReference.Is<ScriptableObject>())
            {
                logger.Error($"无法为可视化脚本 {variableReference.Name} 生成 Writer", variableReference);
                return null;
            }
            if (variableDefinition.HasGenericParameters)
            {
                logger.Error($"无法为通用变量 {variableReference.Name} 生成 Writer", variableReference);
                return null;
            }
            if (variableDefinition.IsInterface)
            {
                logger.Error($"无法为接口 {variableReference.Name} 生成 Writer", variableReference);
                return null;
            }
            if (variableDefinition.IsAbstract)
            {
                logger.Error($"无法为抽象类 {variableReference.Name} 生成 Writer", variableReference);
                return null;
            }
            
            return GenerateClassOrStructWriterFunction(variableReference, ref isFailed);
        }

        private MethodReference GetNetworkBehaviourWriter(TypeReference variableReference)
        {
            if (writeFuncList.TryGetValue(processor.Import<NetworkEntity>(), out MethodReference func))
            {
                Register(variableReference, func);
                return func;
            }
            throw new MissingMethodException($"无法从 NetworkEntity 获取 Writer");
        }

        private MethodDefinition GenerateEnumWriteFunc(TypeReference variable, ref bool isFailed)
        {
            MethodDefinition writerFunc = GenerateWriterFunc(variable);

            ILProcessor worker = writerFunc.Body.GetILProcessor();

            MethodReference underlyingWriter = GetWriteFunc(variable.Resolve().GetEnumUnderlyingType(), ref isFailed);

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, underlyingWriter);

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private MethodDefinition GenerateWriterFunc(TypeReference variable)
        {
            string functionName = $"Write{Math.Abs(variable.FullName.GetHashCode())}";
            MethodDefinition writerFunc = new MethodDefinition(functionName, Const.METHOD_ATTRS, processor.Import(typeof(void)));
            writerFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, processor.Import<NetworkWriter>()));
            writerFunc.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, variable));
            writerFunc.Body.InitLocals = true;

            RegisterWriteFunc(variable, writerFunc);
            return writerFunc;
        }

        private MethodDefinition GenerateClassOrStructWriterFunction(TypeReference variable, ref bool isFailed)
        {
            MethodDefinition writerFunc = GenerateWriterFunc(variable);

            ILProcessor worker = writerFunc.Body.GetILProcessor();

            if (!variable.Resolve().IsValueType)
            {
                WriteNullCheck(worker, ref isFailed);
            }

            if (!WriteAllFields(variable, worker, ref isFailed))
            {
                return null;
            }

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private void WriteNullCheck(ILProcessor worker, ref bool isFailed)
        {
            Instruction labelNotNull = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Brtrue, labelNotNull);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_0);
            worker.Emit(OpCodes.Call, GetWriteFunc(processor.Import<bool>(), ref isFailed));
            worker.Emit(OpCodes.Ret);
            worker.Append(labelNotNull);
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_1);
            worker.Emit(OpCodes.Call, GetWriteFunc(processor.Import<bool>(), ref isFailed));
        }
        
        private bool WriteAllFields(TypeReference variable, ILProcessor worker, ref bool isFailed)
        {
            foreach (FieldDefinition field in variable.FindAllPublicFields())
            {
                MethodReference writeFunc = GetWriteFunc(field.FieldType, ref isFailed);
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

        private MethodDefinition GenerateCollectionWriter(TypeReference variable, TypeReference elementType, string writerFunction, ref bool isFailed)
        {
            MethodDefinition writerFunc = GenerateWriterFunc(variable);
            MethodReference elementWriteFunc = GetWriteFunc(elementType, ref isFailed);

            if (elementWriteFunc == null)
            {
                logger.Error($"无法为 {variable} 生成 Writer", variable);
                isFailed = true;
                return writerFunc;
            }

            ModuleDefinition module = assembly.MainModule;
            TypeReference readerExtensions = module.ImportReference(typeof(StreamExtensions));
            MethodReference collectionWriter = Resolvers.ResolveMethod(readerExtensions, assembly, logger, writerFunction, ref isFailed);

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

            foreach (var (type,method) in writeFuncList)
            {
                worker.Emit(OpCodes.Ldnull);
                worker.Emit(OpCodes.Ldftn, method);
                GenericInstanceType actionGenericInstance = actionRef.MakeGenericInstanceType(networkWriterRef, type);
                MethodReference actionRefInstance = actionConstructorRef.MakeHostInstanceGeneric(assembly.MainModule, actionGenericInstance);
                worker.Emit(OpCodes.Newobj, actionRefInstance);
                GenericInstanceType genericInstance = genericWriterClassRef.MakeGenericInstanceType(type);
                FieldReference specializedField = fieldRef.SpecializeField(assembly.MainModule, genericInstance);
                worker.Emit(OpCodes.Stsfld, specializedField);
            }
        }
    }
}