using System;
using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;
using Object = UnityEngine.Object;
using Component = UnityEngine.Component;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace JFramework.Editor
{
    internal class NetworkReaderProcess
    {
        private readonly Dictionary<TypeReference, MethodReference> funcs = new Dictionary<TypeReference, MethodReference>(new Comparer());
        private readonly Models models;
        private readonly Logger logger;
        private readonly TypeDefinition generate;
        private readonly AssemblyDefinition assembly;

        public NetworkReaderProcess(AssemblyDefinition assembly, Models models, TypeDefinition generate, Logger logger)
        {
            this.logger = logger;
            this.models = models;
            this.assembly = assembly;
            this.generate = generate;
        }

        internal void Register(TypeReference tr, MethodReference md)
        {
            var imported = assembly.MainModule.ImportReference(tr);
            funcs[imported] = md;
        }

        public MethodReference GetFunction(TypeReference tr, ref bool failed)
        {
            if (funcs.TryGetValue(tr, out var mr))
            {
                return mr;
            }

            var reference = assembly.MainModule.ImportReference(tr);
            return GenerateReader(reference, ref failed);
        }

        private MethodReference GenerateReader(TypeReference tr, ref bool failed)
        {
            if (tr.IsArray)
            {
                if (tr.IsMultidimensionalArray())
                {
                    logger.Error($"无法为多维数组 {tr.Name} 生成 Reader", tr);
                    failed = true;
                    return null;
                }

                return GenerateCollection(tr, tr.GetElementType(), nameof(StreamExtensions.ReadArray), ref failed);
            }

            var td = tr.Resolve();
            if (td == null)
            {
                logger.Error($"无法为Null {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (tr.IsByReference)
            {
                logger.Error($"无法为反射 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (td.IsEnum)
            {
                return GenerateEnum(tr, ref failed);
            }

            if (td.Is(typeof(ArraySegment<>)))
            {
                return GenerateArraySegment(tr, ref failed);
            }

            if (td.Is(typeof(List<>)))
            {
                var genericInstance = (GenericInstanceType)tr;
                var elementType = genericInstance.GenericArguments[0];
                return GenerateCollection(tr, elementType, nameof(StreamExtensions.ReadList), ref failed);
            }

            if (tr.IsDerivedFrom<NetworkBehaviour>() || tr.Is<NetworkBehaviour>())
            {
                return GetNetworkBehaviour(tr);
            }

            if (td.IsDerivedFrom<Component>())
            {
                logger.Error($"无法为组件 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (tr.Is<Object>())
            {
                logger.Error($"无法为对象 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (tr.Is<ScriptableObject>())
            {
                logger.Error($"无法为可视化脚本 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (td.HasGenericParameters)
            {
                logger.Error($"无法为通用变量 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (td.IsInterface)
            {
                logger.Error($"无法为接口 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (td.IsAbstract)
            {
                logger.Error($"无法为抽象或泛型 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            return GenerateClassOrStruct(tr, ref failed);
        }

        private MethodReference GetNetworkBehaviour(TypeReference tr)
        {
            var generic = models.ReadNetworkBehaviourGeneric;
            var mr = generic.MakeGeneric(assembly.MainModule, tr);
            Register(tr, mr);
            return mr;
        }

        private MethodDefinition GenerateEnum(TypeReference tr, ref bool failed)
        {
            var md = GenerateFunction(tr);
            var worker = md.Body.GetILProcessor();
            var mr = GetFunction(tr.Resolve().GetEnumUnderlyingType(), ref failed);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, mr);
            worker.Emit(OpCodes.Ret);
            return md;
        }

        private MethodDefinition GenerateCollection(TypeReference tr, TypeReference element, string name, ref bool failed)
        {
            var md = GenerateFunction(tr);
            GetFunction(element, ref failed);
            var extensions = assembly.MainModule.ImportReference(typeof(StreamExtensions));
            var mr = Helper.GetMethod(extensions, assembly, logger, name, ref failed);

            var method = new GenericInstanceMethod(mr);
            method.GenericArguments.Add(element);
            var worker = md.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, method);
            worker.Emit(OpCodes.Ret);
            return md;
        }

        private MethodDefinition GenerateClassOrStruct(TypeReference tr, ref bool failed)
        {
            var md = GenerateFunction(tr);
            md.Body.Variables.Add(new VariableDefinition(tr));
            var worker = md.Body.GetILProcessor();
            var td = tr.Resolve();

            if (!td.IsValueType)
            {
                IsNullCheck(worker, ref failed);
            }

            if (tr.IsValueType)
            {
                worker.Emit(OpCodes.Ldloca, 0);
                worker.Emit(OpCodes.Initobj, tr);
            }
            else if (td.IsDerivedFrom<ScriptableObject>())
            {
                var generic = new GenericInstanceMethod(models.CreateInstanceMethodRef);
                generic.GenericArguments.Add(tr);
                worker.Emit(OpCodes.Call, generic);
                worker.Emit(OpCodes.Stloc_0);
            }
            else
            {
                var ctor = Helper.ResolveDefaultPublicCtor(tr);
                if (ctor == null)
                {
                    logger.Error($"{tr.Name} 不能被反序列化，因为它没有默认的构造函数", tr);
                    failed = true;
                }
                else
                {
                    var ctorRef = assembly.MainModule.ImportReference(ctor);

                    worker.Emit(OpCodes.Newobj, ctorRef);
                    worker.Emit(OpCodes.Stloc_0);
                }
            }

            GetFields(tr, worker, ref failed);
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ret);
            return md;
        }

        private MethodDefinition GenerateFunction(TypeReference tr)
        {
            var md = new MethodDefinition($"Read{NetworkMessage.GetHashByName(tr.FullName)}", CONST.RAW_ATTRS, tr);
            md.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, models.Import<NetworkReader>()));
            md.Body.InitLocals = true;
            Register(tr, md);
            generate.Methods.Add(md);
            return md;
        }

        private MethodDefinition GenerateArraySegment(TypeReference tr, ref bool failed)
        {
            var generic = (GenericInstanceType)tr;
            var element = generic.GenericArguments[0];
            var md = GenerateFunction(tr);
            var worker = md.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, GetFunction(new ArrayType(element), ref failed));
            worker.Emit(OpCodes.Newobj, models.ArraySegmentRef.MakeHostInstanceGeneric(assembly.MainModule, generic));
            worker.Emit(OpCodes.Ret);
            return md;
        }

        private void IsNullCheck(ILProcessor worker, ref bool failed)
        {
            var nop = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, GetFunction(models.Import<bool>(), ref failed));
            worker.Emit(OpCodes.Brtrue, nop);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ret);
            worker.Append(nop);
        }

        private void GetFields(TypeReference tr, ILProcessor worker, ref bool failed)
        {
            foreach (var field in tr.FindAllPublicFields())
            {
                worker.Emit(tr.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc, 0);
                var mr = GetFunction(field.FieldType, ref failed);
                if (mr != null)
                {
                    worker.Emit(OpCodes.Ldarg_0);
                    worker.Emit(OpCodes.Call, mr);
                }
                else
                {
                    logger.Error($"{field.Name} 有不受支持的类型", field);
                    failed = true;
                }

                worker.Emit(OpCodes.Stfld, assembly.MainModule.ImportReference(field));
            }
        }

        internal void InitializeReaders(ILProcessor worker)
        {
            var module = assembly.MainModule;
            var reader = module.ImportReference(typeof(Reader<>));
            var func = module.ImportReference(typeof(Func<,>));
            var tr = module.ImportReference(typeof(NetworkReader));
            var fr = module.ImportReference(typeof(Reader<>).GetField(nameof(Reader<object>.read)));
            var mr = module.ImportReference(typeof(Func<,>).GetConstructors()[0]);
            foreach (var (type, method) in funcs)
            {
                worker.Emit(OpCodes.Ldnull);
                worker.Emit(OpCodes.Ldftn, method);
                var instance = func.MakeGenericInstanceType(tr, type);
                worker.Emit(OpCodes.Newobj, mr.MakeHostInstanceGeneric(assembly.MainModule, instance));
                instance = reader.MakeGenericInstanceType(type);
                worker.Emit(OpCodes.Stsfld, fr.SpecializeField(assembly.MainModule, instance));
            }
        }
    }
}