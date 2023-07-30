using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal static class Helpers
    {
        public static bool IsEditorAssembly(AssemblyDefinition assembly)
        {
            return assembly.MainModule.AssemblyReferences.Any(reference => reference.Name.StartsWith(nameof(UnityEditor)));
        }
    }

    public class SyncVarAccess
    {
        private readonly Dictionary<string, int> syncVars = new Dictionary<string, int>();

        public readonly Dictionary<FieldDefinition, MethodDefinition> setter = new Dictionary<FieldDefinition, MethodDefinition>();

        public readonly Dictionary<FieldDefinition, MethodDefinition> getter = new Dictionary<FieldDefinition, MethodDefinition>();

        public int GetSyncVar(string className) => syncVars.TryGetValue(className, out int value) ? value : 0;
        public void SetSyncVar(string className, int index) => syncVars[className] = index;

        public void Clear()
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