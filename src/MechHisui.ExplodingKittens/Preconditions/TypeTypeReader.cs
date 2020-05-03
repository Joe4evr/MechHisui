using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;

using CommandParameter = Discord.Commands.ParameterInfo;

namespace MechHisui.ExplodingKittens
{
    internal sealed class TypeTypeReader : TypeReader
    {
        //private readonly Type _type;

        //public TypeTypeReader()
        //{
        //    _type = typeof(T);
        //}

        public override Task<TypeReaderResult> ReadAsync(
            ICommandContext context, string input, IServiceProvider services)
        {
            var entry = Assembly.GetEntryAssembly();
            var allAssemblies = entry.GetReferencedAssemblies().Select(an => Assembly.ReflectionOnlyLoad(an.FullName)).ToList();
            allAssemblies.Add(entry);
            
            var allTypes = allAssemblies.SelectMany(a => a.DefinedTypes);
            var matches = allTypes.Where(t => t.Name.Equals(input, StringComparison.OrdinalIgnoreCase)).ToList();

            if (matches.Count == 0)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ObjectNotFound, "Could not find that type."));
            else if (matches.Count == 1)
                return Task.FromResult(TypeReaderResult.FromSuccess(matches.Single()));
            else
                return Task.FromResult(TypeReaderResult.FromError(CommandError.MultipleMatches, "Found multiple matches."));
        }
    }

    internal sealed class RequireBaseTypeAttribute : ParameterPreconditionAttribute
    {
        private readonly Type _requiredBaseType;

        public RequireBaseTypeAttribute(Type requiredBaseType)
        {
            _requiredBaseType = requiredBaseType;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandParameter parameter,
            object value, IServiceProvider services)
        {
            if (!(value is Type type))
                return Task.FromResult(PreconditionResult.FromError("Precondition may only be used on a parameter of type 'Type'."));

            return (_requiredBaseType.IsAssignableFrom(type))
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError($"Type '{type.Name}' does not inherit the required base type '{_requiredBaseType.Name}'."));
        }
    }
}
