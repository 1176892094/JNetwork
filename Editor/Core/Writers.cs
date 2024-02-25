using System;
using System.Collections.Generic;
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
        private readonly Dictionary<TypeReference, MethodReference> funcs = new Dictionary<TypeReference, MethodReference>(new Comparer());
        private readonly Models models;
        private readonly Logger logger;
        private readonly TypeDefinition generate;
        private readonly AssemblyDefinition assembly;

        public Writers(AssemblyDefinition assembly, Models models, TypeDefinition generate, Logger logger)
        {
            this.logger = logger;
            this.models = models;
            this.assembly = assembly;
            this.generate = generate;
        }

        public void Register(TypeReference tr, MethodReference md)
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
            return GenerateWriter(reference, ref failed);
        }

        private MethodReference GenerateWriter(TypeReference tr, ref bool failed)
        {
            if (tr.IsArray)
            {
                if (tr.IsMultidimensionalArray())
                {
                    logger.Error($"无法为多维数组 {tr.Name} 生成 Writer", tr);
                }

                var elementType = tr.GetElementType();
                return GenerateCollection(tr, elementType, nameof(StreamExtensions.WriteArray), ref failed);
            }

            if (tr.IsByReference)
            {
                logger.Error($"无法为反射 {tr.Name} 生成 Writer", tr);
            }

            if (tr.Resolve()?.IsEnum ?? false)
            {
                return GenerateEnum(tr, ref failed);
            }

            if (tr.Is(typeof(ArraySegment<>)))
            {
                var genericInstance = (GenericInstanceType)tr;
                var elementType = genericInstance.GenericArguments[0];
                return GenerateCollection(tr, elementType, nameof(StreamExtensions.WriteArraySegment), ref failed);
            }

            if (tr.Is(typeof(List<>)))
            {
                var genericInstance = (GenericInstanceType)tr;
                var elementType = genericInstance.GenericArguments[0];
                return GenerateCollection(tr, elementType, nameof(StreamExtensions.WriteList), ref failed);
            }

            if (tr.IsDerivedFrom<NetworkBehaviour>() || tr.Is<NetworkBehaviour>())
            {
                return GetNetworkBehaviour(tr);
            }

            var td = tr.Resolve();
            if (td == null)
            {
                logger.Error($"无法为Null {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (td.IsDerivedFrom<Component>())
            {
                logger.Error($"无法为组件 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (tr.Is<Object>())
            {
                logger.Error($"无法为对象 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (tr.Is<ScriptableObject>())
            {
                logger.Error($"无法为可视化脚本 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (td.HasGenericParameters)
            {
                logger.Error($"无法为通用变量 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (td.IsInterface)
            {
                logger.Error($"无法为接口 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (td.IsAbstract)
            {
                logger.Error($"无法为抽象或泛型 {tr.Name} 生成 Writer", tr);
                return null;
            }

            return GenerateClassOrStruct(tr, ref failed);
        }

        private MethodReference GetNetworkBehaviour(TypeReference tr)
        {
            if (!funcs.TryGetValue(models.Import<NetworkBehaviour>(), out var mr))
            {
                throw new MissingMethodException($"无法从 NetworkBehaviour 获取 Writer");
            }

            Register(tr, mr);
            return mr;
        }

        private MethodDefinition GenerateEnum(TypeReference tr, ref bool failed)
        {
            var md = GenerateFunction(tr);
            var worker = md.Body.GetILProcessor();
            var mr = GetFunction(tr.Resolve().GetEnumUnderlyingType(), ref failed);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, mr);
            worker.Emit(OpCodes.Ret);
            return md;
        }

        private MethodDefinition GenerateCollection(TypeReference tr, TypeReference element, string name, ref bool failed)
        {
            var md = GenerateFunction(tr);
            var func = GetFunction(element, ref failed);

            if (func == null)
            {
                logger.Error($"无法为 {tr} 生成 Writer", tr);
                failed = true;
                return md;
            }

            var module = assembly.MainModule;
            var extensions = module.ImportReference(typeof(StreamExtensions));
            var mr = Helper.GetMethod(extensions, assembly, logger, name, ref failed);

            var method = new GenericInstanceMethod(mr);
            method.GenericArguments.Add(element);
            var worker = md.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, method);
            worker.Emit(OpCodes.Ret);
            return md;
        }

        private MethodDefinition GenerateClassOrStruct(TypeReference tr, ref bool failed)
        {
            var md = GenerateFunction(tr);
            var worker = md.Body.GetILProcessor();

            if (!tr.Resolve().IsValueType)
            {
                IsNullCheck(worker, ref failed);
            }

            if (!GetFields(tr, worker, ref failed))
            {
                return null;
            }

            worker.Emit(OpCodes.Ret);
            return md;
        }

        private MethodDefinition GenerateFunction(TypeReference tr)
        {
            var md = new MethodDefinition($"Write{NetworkMessage.GetHashByName(tr.FullName)}", CONST.RAW_ATTRS, models.Import(typeof(void)));
            md.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, models.Import<NetworkWriter>()));
            md.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, tr));
            md.Body.InitLocals = true;
            Register(tr, md);
            generate.Methods.Add(md);
            return md;
        }

        private void IsNullCheck(ILProcessor worker, ref bool failed)
        {
            var nop = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Brtrue, nop);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_0);
            worker.Emit(OpCodes.Call, GetFunction(models.Import<bool>(), ref failed));
            worker.Emit(OpCodes.Ret);
            worker.Append(nop);

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_1);
            worker.Emit(OpCodes.Call, GetFunction(models.Import<bool>(), ref failed));
        }

        private bool GetFields(TypeReference tr, ILProcessor worker, ref bool failed)
        {
            foreach (var field in tr.FindAllPublicFields())
            {
                var mr = GetFunction(field.FieldType, ref failed);
                if (mr == null)
                {
                    return false;
                }

                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldfld, assembly.MainModule.ImportReference(field));
                worker.Emit(OpCodes.Call, mr);
            }

            return true;
        }

        internal void InitializeWriters(ILProcessor worker)
        {
            var module = assembly.MainModule;
            var reader = module.ImportReference(typeof(Writer<>));
            var func = module.ImportReference(typeof(Action<,>));
            var tr = module.ImportReference(typeof(NetworkWriter));
            var fr = module.ImportReference(typeof(Writer<>).GetField(nameof(Writer<object>.write)));
            var mr = module.ImportReference(typeof(Action<,>).GetConstructors()[0]);
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