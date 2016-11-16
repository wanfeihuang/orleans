using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Orleans.CodeGeneration;
using Orleans.Core;
using Orleans.Serialization;
using Orleans.Serialization.Registration;

namespace Orleans.Runtime
{
    /// <summary>
    /// The assembly processor.
    /// </summary>
    internal class AssemblyProcessor : IDisposable
    {
        /// <summary>
        /// The collection of assemblies which have already been processed.
        /// </summary>
        private readonly HashSet<Assembly> processedAssemblies = new HashSet<Assembly>();

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly Logger logger;
        
        /// <summary>
        /// The initialization lock.
        /// </summary>
        private readonly object initializationLock = new object();

        /// <summary>
        /// The type metadata cache.
        /// </summary>
        private readonly TypeMetadataCache typeCache;

        /// <summary>
        /// Whether or not this class has been initialized.
        /// </summary>
        private bool initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyProcessor"/> class.
        /// </summary>
        /// <param name="typeCache">
        /// The type cache.
        /// </param>
        public AssemblyProcessor(TypeMetadataCache typeCache)
        {
            this.logger = LogManager.GetLogger("AssemblyProcessor");
            this.typeCache = typeCache;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (this.initialized)
            {
                return;
            }

            lock (this.initializationLock)
            {
                if (this.initialized)
                {
                    return;
                }

                // load the code generator before intercepting assembly loading
                CodeGeneratorManager.Initialize(); 

                // initialize serialization for all assemblies to be loaded.
                AppDomain.CurrentDomain.AssemblyLoad += this.OnAssemblyLoad;

                ICollection<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies();

                // initialize serialization for already loaded assemblies.
                var generated = CodeGeneratorManager.GenerateAndCacheCodeForAssemblies(assemblies);
                assemblies = assemblies.Union(generated).ToList();

                var serializerFeature = new OrleansSerializationFeature();
                var grainInvokerFeature = new GrainInvokerFeature();
                var grainReferenceFeature = new GrainReferenceFeature();

                var serializerFeatureProvider = new OrleansSerializationFeatureProvider();
                var grainInvokerFeatureProvider = new GrainInvokerFeatureProvider();
                var grainReferenceFeatureProvider = new GrainReferenceFeatureProvider();

                var types = assemblies.SelectMany(asm => TypeUtils.GetDefinedTypes(asm, logger)).ToList();

                serializerFeatureProvider.PopulateFeature(types, serializerFeature);
                grainInvokerFeatureProvider.PopulateFeature(types, grainInvokerFeature);
                grainReferenceFeatureProvider.PopulateFeature(types, grainReferenceFeature);

                this.typeCache.RegisterGrainInvokers(grainInvokerFeature);
                this.typeCache.RegisterGrainReferences(grainReferenceFeature);

                SerializationManager.Register(serializerFeature);
                //foreach (var asm in assemblies)
                //{
                //    ProcessAssembly(asm);
                //}

                this.initialized = true;
            }
        }

        /// <summary>
        /// Handles <see cref="AppDomain.AssemblyLoad"/> events.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event arguments.</param>
        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            this.ProcessAssembly(args.LoadedAssembly);
        }

        /// <summary>
        /// Processes the provided assembly.
        /// </summary>
        /// <param name="assembly">The assembly to process.</param>
        private void ProcessAssembly(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;
            if (this.logger.IsVerbose3)
            {
                this.logger.Verbose3("Processing assembly {0}", assemblyName);
            }

#if !NETSTANDARD
            // If the assembly is loaded for reflection only avoid processing it.
            if (assembly.ReflectionOnly)
            {
                return;
            }
#endif
            // Don't bother re-processing an assembly we've already scanned
            lock (this.processedAssemblies)
            {
                if (!this.processedAssemblies.Add(assembly))
                {
                    return;
                }
            }

            // If the assembly does not reference Orleans, avoid generating code for it.
            if (TypeUtils.IsOrleansOrReferencesOrleans(assembly))
            {
                // Code generation occurs in a self-contained assembly, so invoke it separately.
                CodeGeneratorManager.GenerateAndCacheCodeForAssembly(assembly);
            }

            // Process each type in the assembly.
            var assemblyTypes = TypeUtils.GetDefinedTypes(assembly, this.logger).ToArray();

            // Process each type in the assembly.
            foreach (TypeInfo typeInfo in assemblyTypes)
            {
                try
                {
                    var type = typeInfo.AsType();
                    string typeName = typeInfo.FullName;
                    if (this.logger.IsVerbose3)
                    {
                        this.logger.Verbose3("Processing type {0}", typeName);
                    }

                    //SerializationManager.FindSerializationInfo(type);
    
                    this.typeCache.FindSupportClasses(type);
                }
                catch (Exception exception)
                {
                    this.logger.Error(ErrorCode.SerMgr_TypeRegistrationFailure, "Failed to load type " + typeInfo.FullName + " in assembly " + assembly.FullName + ".", exception);
                }
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= this.OnAssemblyLoad;
        }
    }
}
