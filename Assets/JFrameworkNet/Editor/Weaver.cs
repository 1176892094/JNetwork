using Mono.Cecil;

namespace JFramework.Editor
{
    internal class Weavers
    {
        public Weavers()
        {
        }

        public bool Weave(AssemblyDefinition definition, AssemblyResolver resolver, out bool modified)
        {
            modified = false;
            return false;
        }
    }
}