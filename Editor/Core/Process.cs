using System;
using System.Collections.Generic;
using System.Linq;
using JFramework.Net;
using Mono.Cecil;
using UnityEngine;

namespace JFramework.Editor
{
    internal class Process
    {
        private bool failed;
        private Models models;
        private Writers writers;
        private Readers readers;
        private SyncVarAccess access;
        private TypeDefinition generate;
        private AssemblyDefinition assembly;
        private readonly Logger logger;

        public Process(Logger logger) => this.logger = logger;

        /// <summary>
        /// 执行程序集注入
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resolver"></param>
        /// <param name="change"></param>
        /// <returns></returns>
        public bool Execute(AssemblyDefinition assembly, IAssemblyResolver resolver, out bool change)
        {
            failed = false;
            change = false;
            try
            {
                this.assembly = assembly;
                if (assembly.MainModule.Contains(CONST.GEN_NAMESPACE, CONST.GEN_NAME))
                {
                    return true;
                }

                access = new SyncVarAccess();
                models = new Models(assembly, logger, ref failed);
                generate = new TypeDefinition(CONST.GEN_NAMESPACE, CONST.GEN_NAME, CONST.GEN_ATTRS, models.Import<object>());
                writers = new Writers(assembly, models, generate, logger);
                readers = new Readers(assembly, models, generate, logger);
                change = StreamingProcess.Process(assembly, resolver, logger, writers, readers, ref failed);

                var mainModule = assembly.MainModule;

                change |= ProcessModule(mainModule);
                if (failed)
                {
                    return false;
                }

                if (change)
                {
                    SyncVarProcessReplace.Process(mainModule, access);
                    mainModule.Types.Add(generate);
                    StreamingProcess.StreamingInitialize(assembly, models, writers, readers, generate);
                }

                return true;
            }
            catch (Exception e)
            {
                failed = true;
                logger.Error(e.ToString());
                return false;
            }
            finally
            {
                access.Clear();
            }
        }

        /// <summary>
        /// 处理 NetworkBehaviour
        /// </summary>
        /// <param name="td"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool ProcessNetworkBehavior(TypeDefinition td, ref bool failed)
        {
            if (!td.IsClass) return false;
            if (!td.IsDerivedFrom<NetworkBehaviour>())
            {
                if (td.IsDerivedFrom<MonoBehaviour>())
                {
                    MonoBehaviourProcess.Process(td, logger, ref failed);
                }

                return false;
            }

            var behaviours = new List<TypeDefinition>();

            TypeDefinition parent = td;
            while (parent != null)
            {
                if (parent.Is<NetworkBehaviour>())
                {
                    break;
                }

                try
                {
                    behaviours.Insert(0, parent);
                    parent = parent.BaseType.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    break;
                }
            }

            bool changed = false;
            foreach (TypeDefinition behaviour in behaviours)
            {
                changed |= new NetworkBehaviourProcess(assembly,access, models, writers, readers, logger, behaviour).Process(ref failed);
            }

            return changed;
        }

        /// <summary>
        /// 处理功能
        /// </summary>
        /// <param name="moduleDefinition"></param>
        /// <returns></returns>
        private bool ProcessModule(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Types.Where(td => td.IsClass && td.BaseType.CanBeResolved()).Aggregate(false, (current, td) => current | ProcessNetworkBehavior(td, ref failed));
        }

        /// <summary>
        /// 处理方法中的参数
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="md"></param>
        /// <returns></returns>
        public static string GenerateMethodName(string prefix, MethodDefinition md)
        {
            prefix += md.Name;
            return md.Parameters.Aggregate(prefix, (str, definition) => str + $"{NetworkMessage.GetHashByName(definition.ParameterType.Name)}");
        }
    }
}