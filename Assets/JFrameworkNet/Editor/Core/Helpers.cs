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
    
    internal class Comparer : IEqualityComparer<TypeReference>
    {
        public bool Equals(TypeReference x, TypeReference y) => x?.FullName == y?.FullName;

        public int GetHashCode(TypeReference obj) => obj.FullName.GetHashCode();
    }
}