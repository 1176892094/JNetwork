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
}