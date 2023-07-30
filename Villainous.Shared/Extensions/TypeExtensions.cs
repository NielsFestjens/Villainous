using System.Reflection;

namespace Villainous.Extensions;

public static class TypeExtensions
{
    public static IEnumerable<MethodInfo> GetAllMethods(this Type type, BindingFlags bindingFlags) => type.GetMethods(bindingFlags).Concat(type.GetInterfaces().SelectMany(x => x.GetAllMethods(bindingFlags)));
}