using System.Linq;
using System.Runtime.CompilerServices;
using JFramework.Interface;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace JFramework.Editor
{
    /// <summary>
    /// 流处理
    /// </summary>
    internal static class StreamingProcess
    {
        /// <summary>
        /// 在Process中调用
        /// </summary>
        /// <param name="currentAssembly"></param>
        /// <param name="resolver"></param>
        /// <param name="logger"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        /// <param name="isFailed"></param>
        /// <returns></returns>
        public static bool Process(AssemblyDefinition currentAssembly, IAssemblyResolver resolver, Logger logger, Writers writers, Readers readers, ref bool isFailed)
        {
            ProcessNetworkCode(currentAssembly, resolver, logger, writers, readers);
            return ProcessCustomCode(currentAssembly, currentAssembly, writers, readers);
        }

        /// <summary>
        /// 处理网络代码
        /// </summary>
        /// <param name="currentAssembly"></param>
        /// <param name="resolver"></param>
        /// <param name="logger"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        private static void ProcessNetworkCode(AssemblyDefinition currentAssembly, IAssemblyResolver resolver,Logger logger, Writers writers, Readers readers)
        {
            AssemblyNameReference assemblyReference = currentAssembly.MainModule.FindReference(CONST.ASSEMBLY_NAME);
            if (assemblyReference != null)
            {
                AssemblyDefinition networkAssembly = resolver.Resolve(assemblyReference);
                if (networkAssembly != null)
                {
                    ProcessCustomCode(currentAssembly, networkAssembly, writers, readers);
                }
                else
                {
                    logger.Error($"自动生成网络代码失败: {assemblyReference}");
                }
            }
            else
            {
                logger.Error("注册程序集 JFramework.Net.dll 失败");
            }
        }

        /// <summary>
        /// 处理本地代码
        /// </summary>
        /// <param name="CurrentAssembly"></param>
        /// <param name="assembly"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        /// <returns></returns>
        private static bool ProcessCustomCode(AssemblyDefinition CurrentAssembly, AssemblyDefinition assembly,Writers writers, Readers readers)
        {
            bool modified = false;
            foreach (var definition in assembly.MainModule.Types.Where(definition => definition.IsAbstract && definition.IsSealed))
            {
                modified |= LoadDeclaredWriters(CurrentAssembly, definition, writers);
                modified |= LoadDeclaredReaders(CurrentAssembly, definition, readers);
            }

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                modified |= LoadStreamingMessage(CurrentAssembly.MainModule, writers, readers, type);
            }
            return modified;
        }
        
        /// <summary>
        /// 加载声明的写入器
        /// </summary>
        /// <param name="currentAssembly"></param>
        /// <param name="type"></param>
        /// <param name="writers"></param>
        /// <returns></returns>
        private static bool LoadDeclaredWriters(AssemblyDefinition currentAssembly, TypeDefinition type, Writers writers)
        {
            bool modified = false;
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.Parameters.Count != 2)
                    continue;

                if (!method.Parameters[0].ParameterType.Is<NetworkWriter>())
                    continue;

                if (!method.ReturnType.Is(typeof(void)))
                    continue;

                if (!method.HasCustomAttribute<ExtensionAttribute>())
                    continue;

                if (method.HasGenericParameters)
                    continue;
                
                writers.Register(method.Parameters[1].ParameterType, currentAssembly.MainModule.ImportReference(method));
                modified = true;
            }
            return modified;
        }

        /// <summary>
        /// 加载声明的读取器
        /// </summary>
        /// <param name="currentAssembly"></param>
        /// <param name="type"></param>
        /// <param name="readers"></param>
        /// <returns></returns>
        private static bool LoadDeclaredReaders(AssemblyDefinition currentAssembly, TypeDefinition type, Readers readers)
        {
            bool modified = false;
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.Parameters.Count != 1)
                    continue;

                if (!method.Parameters[0].ParameterType.Is<NetworkReader>())
                    continue;

                if (method.ReturnType.Is(typeof(void)))
                    continue;

                if (!method.HasCustomAttribute<ExtensionAttribute>())
                    continue;

                if (method.HasGenericParameters)
                    continue;

                readers.Register(method.ReturnType, currentAssembly.MainModule.ImportReference(method));
                modified = true;
            }
            return modified;
        }

        /// <summary>
        /// 加载读写流的信息
        /// </summary>
        /// <param name="module"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        /// <param name="type"></param>
        /// <param name="isFailed"></param>
        /// <returns></returns>
        private static bool LoadStreamingMessage(ModuleDefinition module, Writers writers, Readers readers, TypeDefinition type)
        {
            bool modified = false;
            if (!type.IsAbstract && !type.IsInterface && type.ImplementsInterface<IEvent>())
            {
                readers.GetReadFunc(module.ImportReference(type));
                writers.GetWriteFunc(module.ImportReference(type));
                modified = true;
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                modified |= LoadStreamingMessage(module, writers, readers, nested);
            }

            return modified;
        }
        
        /// <summary>
        /// 添加 RuntimeInitializeLoad 属性类型
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="process"></param>
        /// <param name="method"></param>
        private static void AddRuntimeInitializeOnLoadAttribute(AssemblyDefinition assembly, Processor process, MethodDefinition method)
        {
            MethodDefinition definition = process.runtimeInitializeOnLoadMethodAttribute.GetConstructors().Last();
            CustomAttribute attribute = new CustomAttribute(assembly.MainModule.ImportReference(definition));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(process.Import<RuntimeInitializeLoadType>(), RuntimeInitializeLoadType.BeforeSceneLoad));
            method.CustomAttributes.Add(attribute);
        }
        
        /// <summary>
        /// 添加 RuntimeInitializeLoad 属性标记
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="process"></param>
        /// <param name="method"></param>
        private static void AddInitializeOnLoadAttribute(AssemblyDefinition assembly, Processor process, MethodDefinition method)
        {
            MethodDefinition ctor = process.initializeOnLoadMethodAttribute.GetConstructors().First();
            CustomAttribute attribute = new CustomAttribute(assembly.MainModule.ImportReference(ctor));
            method.CustomAttributes.Add(attribute);
        }
        
        /// <summary>
        /// 初始化读写流
        /// </summary>
        /// <param name="currentAssembly"></param>
        /// <param name="process"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        /// <param name="generatedClass"></param>
        public static void StreamingInitialize(AssemblyDefinition currentAssembly, Processor process, Writers writers, Readers readers, TypeDefinition generatedClass)
        {
            MethodDefinition initReadWriters = new MethodDefinition("RuntimeInitializeOnLoad", MethodAttributes.Public | MethodAttributes.Static, process.Import(typeof(void)));
            
            AddRuntimeInitializeOnLoadAttribute(currentAssembly, process, initReadWriters);
            
            if (Helpers.IsEditorAssembly(currentAssembly))
            {
                AddInitializeOnLoadAttribute(currentAssembly, process, initReadWriters);
            }
            
            ILProcessor worker = initReadWriters.Body.GetILProcessor();
            writers.InitializeWriters(worker);
            readers.InitializeReaders(worker);
            worker.Emit(OpCodes.Ret);
            generatedClass.Methods.Add(initReadWriters);
        }
    }
}