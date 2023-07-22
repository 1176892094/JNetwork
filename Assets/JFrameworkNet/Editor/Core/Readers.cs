using System;
using System.Collections.Generic;
using System.Reflection;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;
using Object = UnityEngine.Object;
using Component = UnityEngine.Component;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace JFramework.Editor
{
    internal class Readers
    {
        private readonly Dictionary<TypeReference, MethodReference> readFuncList = new Dictionary<TypeReference, MethodReference>(new Comparer());
        private readonly AssemblyDefinition assembly;
        private readonly Logger logger;
        private readonly Process process;
        private readonly TypeDefinition generate;

        public Readers(AssemblyDefinition assembly, Process process, TypeDefinition generate, Logger logger)
        {
            this.assembly = assembly;
            this.process = process;
            this.generate = generate;
            this.logger = logger;
        }

        internal void Register(TypeReference dataType, MethodReference methodReference)
        {
            TypeReference imported = assembly.MainModule.ImportReference(dataType);
            readFuncList[imported] = methodReference;
        }

        private void RegisterReadFunc(TypeReference typeReference, MethodDefinition newReaderFunc)
        {
            Register(typeReference, newReaderFunc);
            generate.Methods.Add(newReaderFunc);
        }
        
        public MethodReference GetReadFunc(TypeReference variable)
        {
            if (readFuncList.TryGetValue(variable, out MethodReference foundFunc))
            {
                return foundFunc;
            }

            TypeReference importedVariable = assembly.MainModule.ImportReference(variable);
            return GenerateReader(importedVariable);
        }

        private MethodReference GenerateReader(TypeReference variableReference)
        {
            if (variableReference.IsArray)
            {
                if (variableReference.IsMultidimensionalArray())
                {
                    logger.Error($"无法为多维数组 {variableReference.Name} 生成 Reader", variableReference);
                    Command.failed = true;
                    return null;
                }

                return GenerateReadCollection(variableReference, variableReference.GetElementType(), nameof(StreamExtensions.ReadArray));
            }
            
            TypeDefinition variableDefinition = variableReference.Resolve();
            if (variableDefinition == null)
            {
                logger.Error($"无法为Null {variableReference.Name} 生成 Reader", variableReference); 
                Command.failed = true;
                return null;
            }
            
            if (variableReference.IsByReference)
            {
                logger.Error($"无法为反射 {variableReference.Name} 生成 Reader", variableReference);
                Command.failed = true;
                return null;
            }
            
            if (variableDefinition.IsEnum)
            {
                return GenerateEnumReadFunc(variableReference);
            }
            
            if (variableDefinition.Is(typeof(ArraySegment<>)))
            {
                return GenerateArraySegmentReadFunc(variableReference);
            }
            
            if (variableDefinition.Is(typeof(List<>)))
            {
                GenericInstanceType genericInstance = (GenericInstanceType)variableReference;
                TypeReference elementType = genericInstance.GenericArguments[0];

                return GenerateReadCollection(variableReference, elementType, nameof(StreamExtensions.ReadList));
            }
            
            if (variableReference.IsDerivedFrom<NetworkBehaviour>() || variableReference.Is<NetworkBehaviour>())
            {
                return GetNetworkBehaviourReader(variableReference);
            }
            
            if (variableDefinition.IsDerivedFrom<Component>())
            {
                logger.Error($"无法为组件 {variableReference.Name} 生成 Reader", variableReference);
                Command.failed = true;
                return null;
            }
            
            if (variableReference.Is<Object>())
            {
                logger.Error($"无法为对象 {variableReference.Name} 生成 Reader", variableReference);
                Command.failed = true;
                return null;
            }
            
            if (variableReference.Is<ScriptableObject>())
            {
                logger.Error($"无法为可视化脚本 {variableReference.Name} 生成 Reader", variableReference);
                Command.failed = true;
                return null;
            }
            
            if (variableDefinition.HasGenericParameters)
            {
                logger.Error($"无法为通用变量 {variableReference.Name} 生成 Reader", variableReference);
                Command.failed = true;
                return null;
            }
            
            if (variableDefinition.IsInterface)
            {
                logger.Error($"无法为接口 {variableReference.Name} 生成 Reader", variableReference);
                Command.failed = true;
                return null;
            }
            
            if (variableDefinition.IsAbstract)
            { 
                logger.Error($"无法为抽象类 {variableReference.Name} 生成 Reader", variableReference);
                Command.failed = true;
                return null;
            }

            return GenerateClassOrStructReadFunction(variableReference);
        }

        private MethodDefinition GenerateReadCollection(TypeReference variable, TypeReference elementType, string readerFunction)
        {
            MethodDefinition readerFunc = GenerateReaderFunction(variable);
            GetReadFunc(elementType);
            ModuleDefinition module = assembly.MainModule;
            TypeReference readerExtensions = module.ImportReference(typeof(StreamExtensions));
            MethodReference listReader = Utils.ResolveMethod(readerExtensions, assembly, logger, readerFunction);
            GenericInstanceMethod methodRef = new GenericInstanceMethod(listReader);
            methodRef.GenericArguments.Add(elementType);
            ILProcessor worker = readerFunc.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, methodRef);
            worker.Emit(OpCodes.Ret);
            return readerFunc;
        }

        private MethodDefinition GenerateReaderFunction(TypeReference variable)
        {
            string functionName = $"Read{NetworkEvent.GetHashByName(variable.FullName)}";
            MethodDefinition readerFunc = new MethodDefinition(functionName, CONST.RAW_ATTRS, variable);
            readerFunc.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, process.Import<NetworkReader>()));
            readerFunc.Body.InitLocals = true;
            RegisterReadFunc(variable, readerFunc);
            return readerFunc;
        }

        private MethodDefinition GenerateEnumReadFunc(TypeReference variable)
        {
            MethodDefinition readerFunc = GenerateReaderFunction(variable);
            ILProcessor worker = readerFunc.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            TypeReference underlyingType = variable.Resolve().GetEnumUnderlyingType();
            MethodReference underlyingFunc = GetReadFunc(underlyingType);
            worker.Emit(OpCodes.Call, underlyingFunc);
            worker.Emit(OpCodes.Ret);
            return readerFunc;
        }

        private MethodDefinition GenerateArraySegmentReadFunc(TypeReference variable)
        {
            GenericInstanceType genericInstance = (GenericInstanceType)variable;
            TypeReference elementType = genericInstance.GenericArguments[0];
            MethodDefinition readerFunc = GenerateReaderFunction(variable);
            ILProcessor worker = readerFunc.Body.GetILProcessor();
            ArrayType arrayType = new ArrayType(elementType);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, GetReadFunc(arrayType));
            worker.Emit(OpCodes.Newobj, process.ArraySegmentReference.MakeHostInstanceGeneric(assembly.MainModule, genericInstance));
            worker.Emit(OpCodes.Ret);
            return readerFunc;
        }

        private MethodReference GetNetworkBehaviourReader(TypeReference variableReference)
        {
            MethodReference generic = process.readNetworkBehaviourGeneric;
            MethodReference readFunc = generic.MakeGeneric(assembly.MainModule, variableReference);
            Register(variableReference, readFunc);
            return readFunc;
        }

        private MethodDefinition GenerateClassOrStructReadFunction(TypeReference variable)
        {
            MethodDefinition readerFunc = GenerateReaderFunction(variable);
            
            readerFunc.Body.Variables.Add(new VariableDefinition(variable));

            ILProcessor worker = readerFunc.Body.GetILProcessor();

            TypeDefinition td = variable.Resolve();

            if (!td.IsValueType)
            {
                GenerateNullCheck(worker);
            }

            CreateNew(variable, worker, td);
            ReadAllFields(variable, worker);

            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ret);
            return readerFunc;
        }

        private void GenerateNullCheck(ILProcessor worker)
        {
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, GetReadFunc(process.Import<bool>()));
            Instruction labelEmptyArray = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Brtrue, labelEmptyArray);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ret);
            worker.Append(labelEmptyArray);
        }

        private void CreateNew(TypeReference variable, ILProcessor worker, TypeDefinition td)
        {
            if (variable.IsValueType)
            {
                worker.Emit(OpCodes.Ldloca, 0);
                worker.Emit(OpCodes.Initobj, variable);
            }
            else if (td.IsDerivedFrom<ScriptableObject>())
            {
                GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(process.ScriptableObjectCreateInstanceMethod);
                genericInstanceMethod.GenericArguments.Add(variable);
                worker.Emit(OpCodes.Call, genericInstanceMethod);
                worker.Emit(OpCodes.Stloc_0);
            }
            else
            {
                MethodDefinition ctor = Utils.ResolveDefaultPublicCtor(variable);
                if (ctor == null)
                {
                    logger.Error($"{variable.Name} 不能被反序列化，因为它没有默认的构造函数", variable);
                    Command.failed = true;
                    return;
                }

                MethodReference ctorRef = assembly.MainModule.ImportReference(ctor);

                worker.Emit(OpCodes.Newobj, ctorRef);
                worker.Emit(OpCodes.Stloc_0);
            }
        }

        private void ReadAllFields(TypeReference variable, ILProcessor worker)
        {
            foreach (FieldDefinition field in variable.FindAllPublicFields())
            {
                OpCode opcode = variable.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc;
                worker.Emit(opcode, 0);
                MethodReference readFunc = GetReadFunc(field.FieldType);
                if (readFunc != null)
                {
                    worker.Emit(OpCodes.Ldarg_0);
                    worker.Emit(OpCodes.Call, readFunc);
                }
                else
                {
                    logger.Error($"{field.Name} 有不受支持的类型", field);
                    Command.failed = true;
                }
                FieldReference fieldRef = assembly.MainModule.ImportReference(field);

                worker.Emit(OpCodes.Stfld, fieldRef);
            }
        }

        internal void InitializeReaders(ILProcessor worker)
        {
            ModuleDefinition module = assembly.MainModule;
            TypeReference genericReaderClassRef = module.ImportReference(typeof(Reader<>));
            FieldInfo fieldInfo = typeof(Reader<>).GetField(nameof(Reader<object>.read));
            FieldReference fieldRef = module.ImportReference(fieldInfo);
            TypeReference networkReaderRef = module.ImportReference(typeof(NetworkReader));
            TypeReference funcRef = module.ImportReference(typeof(Func<,>));
            MethodReference funcConstructorRef = module.ImportReference(typeof(Func<,>).GetConstructors()[0]);
            foreach (var (type, method) in readFuncList)
            {
                worker.Emit(OpCodes.Ldnull);
                worker.Emit(OpCodes.Ldftn, method);
                GenericInstanceType funcGenericInstance = funcRef.MakeGenericInstanceType(networkReaderRef, type);
                MethodReference funcConstructorInstance = funcConstructorRef.MakeHostInstanceGeneric(assembly.MainModule, funcGenericInstance);
                worker.Emit(OpCodes.Newobj, funcConstructorInstance);
                GenericInstanceType genericInstance = genericReaderClassRef.MakeGenericInstanceType(type);
                FieldReference specializedField = fieldRef.SpecializeField(assembly.MainModule, genericInstance);
                worker.Emit(OpCodes.Stsfld, specializedField);
            }
        }
    }
}