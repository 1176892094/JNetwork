using System;
using System.Collections.Generic;
using System.Linq;
using JFramework.Net;
using Mono.Cecil;
using UnityEngine;

namespace JFramework.Editor
{
    internal class Injection
    {
        public static bool failed;
        public static bool change;
        private Writers writers;
        private Readers readers;
        private Processor processor;
        private SyncVarList syncVars;
        private TypeDefinition generate;
        private AssemblyDefinition currentAssembly;
        private readonly Logger logger;
        
        public Injection(Logger logger)
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

                syncVars = new SyncVarList();
                processor = new Processor(currentAssembly, logger);
                
                generate = new TypeDefinition(CONST.GEN_NAMESPACE, CONST.GEN_NET_CODE, CONST.ATTRIBUTES, processor.Import<object>());
                writers = new Writers(currentAssembly, processor, generate, logger);
                readers = new Readers(currentAssembly, processor, generate, logger);
                change = StreamingProcess.Process(currentAssembly, resolver, logger, writers, readers);

                ModuleDefinition moduleDefinition = currentAssembly.MainModule;

                change |= WeaveModule(moduleDefinition);
                if (failed)
                {
                    return false;
                }
                
                if (change)
                {
                    moduleDefinition.Types.Add(generate);
                    StreamingProcess.StreamingInitialize(currentAssembly, processor, writers,readers,generate);
                }
                
                return true;
            }
            catch (Exception e)
            {
                failed = true;
                logger.Error(e.ToString());
                return false;
            }
        }
        
        private bool WeaveNetworkBehavior(TypeDefinition td)
        {
            if (!td.IsClass) return false;
            if (!td.IsDerivedFrom<NetworkBehaviour>())
            {
                if (td.IsDerivedFrom<MonoBehaviour>())
                {
                    MonoBehaviourProcess.Process(logger, td, ref failed);
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
                changed |= new NetworkBehaviourProcess(currentAssembly, processor, syncVars, writers, readers, logger, behaviour).Process(ref failed);
            }
            return changed;
        }
        
        private bool WeaveModule(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Types.Where(td => td.IsClass && td.BaseType.CanBeResolved()).Aggregate(false, (current, td) => current | WeaveNetworkBehavior(td));
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
            return md.Parameters.Aggregate(prefix, (str, definition) => str + $"_{NetworkEvent.GetHashByName(definition.ParameterType.Name)}");
        }
    }
}