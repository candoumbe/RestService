using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Defines extension methods for the Type type
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Verifies whether it is an anonymous type object
        /// </summary>
        /// <param name="type">The object type to be tested</param>
        /// <returns>true if it is an anonymous type object</returns>
        /// <example>
        /// <code>
        /// var x = new { id = 1, name = "foo", lastName = "bar" };
        /// 
        /// bool result = x.GetType().IsAnonymousType(); //true
        /// </code>
        /// </example>
        public static bool IsAnonymousType(this Type type)
        {
            bool hasCompilerGeneratedAttribute = type.GetTypeInfo().GetCustomAttribute<CompilerGeneratedAttribute>() != null;
            bool nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
            bool isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

            return isAnonymousType;
        }
    }
}
