using System.Linq;
using System.Runtime.CompilerServices;
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
        /// <param name="assembly"></param>
        /// <param name="resolver"></param>
        /// <param name="logger"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public static bool Process(AssemblyDefinition assembly, IAssemblyResolver resolver, Logger logger, Writers writers, Readers readers, ref bool failed)
        {
            ProcessNetworkCode(assembly, resolver, logger, writers, readers, ref failed);
            return ProcessCustomCode(assembly, assembly, writers, readers, ref failed);
        }

        /// <summary>
        /// 处理网络代码
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resolver"></param>
        /// <param name="logger"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        /// <param name="failed"></param>
        private static void ProcessNetworkCode(AssemblyDefinition assembly, IAssemblyResolver resolver, Logger logger, Writers writers, Readers readers, ref bool failed)
        {
            var assemblyReference = assembly.MainModule.FindReference(CONST.ASSEMBLY_NAME);
            if (assemblyReference != null)
            {
                var netAssembly = resolver.Resolve(assemblyReference);
                if (netAssembly != null)
                {
                    ProcessCustomCode(assembly, netAssembly, writers, readers, ref failed);
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
        /// <param name="assembly"></param>
        /// <param name="netAssembly"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private static bool ProcessCustomCode(AssemblyDefinition assembly, AssemblyDefinition netAssembly, Writers writers, Readers readers, ref bool failed)
        {
            bool changed = false;
            foreach (var definition in netAssembly.MainModule.Types.Where(definition => definition.IsAbstract && definition.IsSealed))
            {
                changed |= LoadDeclaredWriters(assembly, definition, writers);
                changed |= LoadDeclaredReaders(assembly, definition, readers);
            }

            foreach (TypeDefinition type in netAssembly.MainModule.Types)
            {
                changed |= LoadStreamingMessage(assembly.MainModule, writers, readers, type, ref failed);
            }

            return changed;
        }

        /// <summary>
        /// 加载声明的写入器
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="td"></param>
        /// <param name="writers"></param>
        /// <returns></returns>
        private static bool LoadDeclaredWriters(AssemblyDefinition assembly, TypeDefinition td, Writers writers)
        {
            bool change = false;
            foreach (var md in td.Methods)
            {
                if (md.Parameters.Count != 2)
                    continue;

                if (!md.Parameters[0].ParameterType.Is<NetworkWriter>())
                    continue;

                if (!md.ReturnType.Is(typeof(void)))
                    continue;

                if (!md.HasCustomAttribute<ExtensionAttribute>())
                    continue;

                if (md.HasGenericParameters)
                    continue;

                writers.Register(md.Parameters[1].ParameterType, assembly.MainModule.ImportReference(md));
                change = true;
            }

            return change;
        }

        /// <summary>
        /// 加载声明的读取器
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="td"></param>
        /// <param name="readers"></param>
        /// <returns></returns>
        private static bool LoadDeclaredReaders(AssemblyDefinition assembly, TypeDefinition td, Readers readers)
        {
            bool change = false;
            foreach (var md in td.Methods)
            {
                if (md.Parameters.Count != 1)
                    continue;

                if (!md.Parameters[0].ParameterType.Is<NetworkReader>())
                    continue;

                if (md.ReturnType.Is(typeof(void)))
                    continue;

                if (!md.HasCustomAttribute<ExtensionAttribute>())
                    continue;

                if (md.HasGenericParameters)
                    continue;

                readers.Register(md.ReturnType, assembly.MainModule.ImportReference(md));
                change = true;
            }

            return change;
        }

        /// <summary>
        /// 加载读写流的信息
        /// </summary>
        /// <param name="module"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        /// <param name="td"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private static bool LoadStreamingMessage(ModuleDefinition module, Writers writers, Readers readers, TypeDefinition td, ref bool failed)
        {
            bool change = false;
            if (!td.IsAbstract && !td.IsInterface && td.ImplementsInterface<Message>())
            {
                readers.GetReadFunc(module.ImportReference(td), ref failed);
                writers.GetWriteFunc(module.ImportReference(td), ref failed);
                change = true;
            }

            foreach (var nested in td.NestedTypes)
            {
                change |= LoadStreamingMessage(module, writers, readers, nested, ref failed);
            }

            return change;
        }

        /// <summary>
        /// 添加 RuntimeInitializeLoad 属性类型
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="models"></param>
        /// <param name="md"></param>
        private static void AddRuntimeInitializeOnLoadAttribute(AssemblyDefinition assembly, Models models, MethodDefinition md)
        {
            var definition = models.RuntimeInitializeOnLoadMethodAttribute.GetConstructors().Last();
            var attribute = new CustomAttribute(assembly.MainModule.ImportReference(definition));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(models.Import<RuntimeInitializeLoadType>(), RuntimeInitializeLoadType.BeforeSceneLoad));
            md.CustomAttributes.Add(attribute);
        }

        /// <summary>
        /// 添加 RuntimeInitializeLoad 属性标记
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="models"></param>
        /// <param name="md"></param>
        private static void AddInitializeOnLoadAttribute(AssemblyDefinition assembly, Models models, MethodDefinition md)
        {
            var ctor = models.InitializeOnLoadMethodAttribute.GetConstructors().First();
            var attribute = new CustomAttribute(assembly.MainModule.ImportReference(ctor));
            md.CustomAttributes.Add(attribute);
        }

        /// <summary>
        /// 初始化读写流
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="models"></param>
        /// <param name="writers"></param>
        /// <param name="readers"></param>
        /// <param name="td"></param>
        public static void StreamingInitialize(AssemblyDefinition assembly, Models models, Writers writers, Readers readers, TypeDefinition td)
        {
            var method = new MethodDefinition("RuntimeInitializeOnLoad", MethodAttributes.Public | MethodAttributes.Static, models.Import(typeof(void)));

            AddRuntimeInitializeOnLoadAttribute(assembly, models, method);

            if (Helpers.IsEditorAssembly(assembly))
            {
                AddInitializeOnLoadAttribute(assembly, models, method);
            }

            var worker = method.Body.GetILProcessor();
            writers.InitializeWriters(worker);
            readers.InitializeReaders(worker);
            worker.Emit(OpCodes.Ret);
            td.Methods.Add(method);
        }
    }
}