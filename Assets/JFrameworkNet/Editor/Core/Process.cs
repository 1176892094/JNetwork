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
        public static bool failed;
        public static bool change;
        private Writers writers;
        private Readers readers;
        private Model model;
        private TypeDefinition generate;
        private AssemblyDefinition currentAssembly;
        private readonly Logger logger;

        public Process(Logger logger)
        {
            this.logger = logger;
        }

        public bool Execute(AssemblyDefinition assembly, IAssemblyResolver resolver)
        {
            failed = false;
            change = false;
            try
            {
                currentAssembly = assembly;
                if (currentAssembly.MainModule.Contains(CONST.GEN_NAMESPACE, CONST.GEN_NET_CODE))
                {
                    return true;
                }
                
                model = new Model(currentAssembly, logger);
                generate = new TypeDefinition(CONST.GEN_NAMESPACE, CONST.GEN_NET_CODE, CONST.TYPE_ATTRS, model.Import<object>());
                writers = new Writers(currentAssembly, model, generate, logger);
                readers = new Readers(currentAssembly, model, generate, logger);
                change = StreamingProcess.Process(currentAssembly, resolver, logger, writers, readers);

                ModuleDefinition moduleDefinition = currentAssembly.MainModule;

                change |= ProcessModule(moduleDefinition);
                if (failed)
                {
                    return false;
                }
                
                if (change)
                {
                    SyncVarProcessReplace.Process(moduleDefinition);
                    moduleDefinition.Types.Add(generate);
                    StreamingProcess.StreamingInitialize(currentAssembly, model, writers,readers,generate);
                }
                
                return true;
            }
            catch (Exception e)
            {
                failed = true;
                SyncVarUtils.Clear();
                logger.Error(e.ToString());
                return false;
            }
        }
        
        private bool ProcessNetworkBehavior(TypeDefinition td)
        {
            
            if (!td.IsClass) return false;
            if (!td.IsDerivedFrom<NetworkBehaviour>())
            {
                if (td.IsDerivedFrom<MonoBehaviour>())
                {
                    MonoBehaviourProcess.Process(logger, td);
                }
                return false;
            }
           
            var behaviourClasses = new List<TypeDefinition>();

            TypeDefinition parent = td;
            while (parent != null)
            {
                if (parent.Is<NetworkBehaviour>())
                {
                    break;
                }

                try
                {
                    behaviourClasses.Insert(0, parent);
                    parent = parent.BaseType.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    break;
                }
            }

            bool changed = false;
            foreach (TypeDefinition behaviour in behaviourClasses)
            {
                changed |= new NetworkBehaviourProcess(currentAssembly, model,  writers, readers, logger, behaviour).Process();
            }
            return changed;
        }
        
        private bool ProcessModule(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Types.Where(td => td.IsClass && td.BaseType.CanBeResolved()).Aggregate(false, (current, td) => current | ProcessNetworkBehavior(td));
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