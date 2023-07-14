using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal static class SyncObjectInitializer
    {
        public static void GenerateSyncObjectInitializer(ILProcessor worker, Processor processor, FieldDefinition fd)
        {
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldfld, fd);
            worker.Emit(OpCodes.Call, processor.InitSyncObjectReference);
        }

        public static bool ImplementsSyncObject(TypeReference typeRef)
        {
            try
            {
                if (typeRef.IsValueType)
                {
                    return false;
                }

                return typeRef.Resolve().IsDerivedFrom<SyncObject>();
            }
            catch
            {
                // ignored
            }

            return false;
        }
    }
}