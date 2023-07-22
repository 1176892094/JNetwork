using System.Linq;
using JFramework.Net;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal static class MonoBehaviourProcess
    {
        public static void Process(Logger logger, TypeDefinition td)
        {
            ProcessVar(logger, td);
            ProcessRpc(logger, td);
        }

        private static void ProcessVar(Logger logger, TypeDefinition td)
        {
            foreach (var fd in td.Fields.Where(fd => fd.HasCustomAttribute<SyncVarAttribute>()))
            {
                logger.Error($"网络变量 {fd.Name} 必须在 NetworkEntity 中使用。", fd);
                Editor.Process.failed = true;
            }
        }

        private static void ProcessRpc(Logger logger, TypeDefinition td)
        {
            foreach (MethodDefinition md in td.Methods)
            {
                if (md.HasCustomAttribute<ServerRpcAttribute>())
                {
                    logger.Error($"ServerRpc {md.Name} 必须在 NetworkEntity 中使用。", md);
                    Editor.Process.failed = true;
                }

                if (md.HasCustomAttribute<ClientRpcAttribute>())
                {
                    logger.Error($"ClientRpc {md.Name} 必须在 NetworkEntity 中使用。", md);
                    Editor.Process.failed = true;
                }

                if (md.HasCustomAttribute<TargetRpcAttribute>())
                {
                    logger.Error($"TargetRpc {md.Name} 必须在 NetworkEntity 中使用。", md);
                    Editor.Process.failed = true;
                }
            }
        }
    }
}