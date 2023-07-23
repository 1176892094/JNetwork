using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal static class Helpers
    {
        public static bool IsEditorAssembly(AssemblyDefinition currentAssembly)
        {
            return currentAssembly.MainModule.AssemblyReferences.Any(assemblyReference => assemblyReference.Name.StartsWith(nameof(UnityEditor)));
        }
    }

    public static class SyncVarHelpers
    {
        private static readonly Dictionary<string, int> syncVars = new Dictionary<string, int>();
        public static readonly Dictionary<FieldDefinition, MethodDefinition> setter = new Dictionary<FieldDefinition, MethodDefinition>();
        public static readonly Dictionary<FieldDefinition, MethodDefinition> getter = new Dictionary<FieldDefinition, MethodDefinition>();
        public static int GetSyncVar(string className) => syncVars.TryGetValue(className, out int value) ? value : 0;
        public static void SetSyncVar(string className, int index) => syncVars[className] = index;

        public static void Clear()
        {
            setter.Clear();
            getter.Clear();
            syncVars.Clear();
        }
    }
    
    internal class Comparer : IEqualityComparer<TypeReference>
    {
        public bool Equals(TypeReference x, TypeReference y) => x?.FullName == y?.FullName;

        public int GetHashCode(TypeReference obj) => obj.FullName.GetHashCode();
    }
}