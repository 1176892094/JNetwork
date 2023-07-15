using JFramework.Net;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal static class MonoBehaviourProcess
    {
        public static void Process(Logger logger, TypeDefinition td, ref bool isFailed)
        {
            ProcessVar(logger, td, ref isFailed);
            ProcessRpc(logger, td, ref isFailed);
        }

        private static void ProcessVar(Logger logger, TypeDefinition td, ref bool isFailed)
        {
            foreach (FieldDefinition fd in td.Fields)
            {
                if (fd.HasCustomAttribute<ServerVarAttribute>())
                {
                    logger.Error($"网络变量 {fd.Name} 必须在 NetworkEntity 中使用。", fd);
                    isFailed = true;
                }

                if (SyncObjectInitializer.ImplementsSyncObject(fd.FieldType))
                {
                    logger.Error($"网络对象 {fd.Name} 必须在 NetworkEntity 中使用。", fd);
                    isFailed = true;
                }
            }
        }

        private static void ProcessRpc(Logger logger, TypeDefinition td, ref bool isFailed)
        {
            foreach (MethodDefinition md in td.Methods)
            {
                if (md.HasCustomAttribute<ServerRpcAttribute>())
                {
                    logger.Error($"ServerRpc {md.Name} 必须在 NetworkEntity 中使用。", md);
                    isFailed = true;
                }

                if (md.HasCustomAttribute<ClientRpcAttribute>())
                {
                    logger.Error($"ClientRpc {md.Name} 必须在 NetworkEntity 中使用。", md);
                    isFailed = true;
                }

                if (md.HasCustomAttribute<TargetRpcAttribute>())
                {
                    logger.Error($"TargetRpc {md.Name} 必须在 NetworkEntity 中使用。", md);
                    isFailed = true;
                }
            }
        }
    }
}