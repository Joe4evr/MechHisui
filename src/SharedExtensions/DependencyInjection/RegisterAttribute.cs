//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using Microsoft.Extensions.DependencyInjection;

//namespace SharedExtensions.DependencyInjection
//{
//    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
//    public sealed class RegisterAttribute : Attribute
//    {
//    }

//    public static class ServiceCollectionExtensions
//    {
//        public static void AutoRegisterServices(this IServiceCollection serviceCollection, Assembly entryAssembly)
//        {
//            IEnumerable<Type> GetRegisterMarkedTypes(AssemblyName assemblyName)
//            {
//                var asm = Assembly.ReflectionOnlyLoad(assemblyName.FullName);
//                var candidateTypes = new List<Type>();

//                foreach (var type in asm.ExportedTypes)
//                {
//                    if (type.GetCustomAttribute<RegisterAttribute>() != null)
//                    {
//                        candidateTypes.Add(type);
//                    }
//                }

//                return candidateTypes.Concat(asm.GetReferencedAssemblies().SelectMany(GetRegisterMarkedTypes));
//            }

//            var services = GetRegisterMarkedTypes(entryAssembly.GetName());

//            foreach (var svc in services)
//            {
//                serviceCollection.AddSingleton(svc);
//            }
//        }
//    }
//}
