using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal class ReflectionProvider : IReflectionImporterProvider
    {
        public IReflectionImporter GetReflectionImporter(ModuleDefinition module) => new ReflectionImporter(module);
    }
    
    internal class ReflectionImporter : DefaultReflectionImporter
    {
        private const string SystemPrivateCoreLib = "System.Private.CoreLib";
        private readonly AssemblyNameReference fixedCoreLib;

        public ReflectionImporter(ModuleDefinition module) : base(module)
        {
            fixedCoreLib = module.AssemblyReferences.FirstOrDefault(assembly => assembly.Name is "mscorlib" or "netstandard" or SystemPrivateCoreLib);
        }

        public override AssemblyNameReference ImportReference(AssemblyName name)
        {
            if (name.Name == SystemPrivateCoreLib && fixedCoreLib != null)
            {
                return fixedCoreLib;
            }

            return base.ImportReference(name);
        }
    }
}