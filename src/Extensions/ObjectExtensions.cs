using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace System
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// This method is intend is to parse an object to extract its properties.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IDictionary<string, object> ParseAnonymousObject(this object obj)
        {
            IDictionary<string, object> dictionary = new Dictionary<string, object>();
            if (obj != null)
            {
                dictionary = obj.GetType()
                    .GetRuntimeProperties()
                    .Where(pi => pi.CanRead && pi.GetValue(obj) != null)
                    .ToDictionary(pi => pi.Name, pi => pi.GetValue(obj));
            }

            return dictionary;
        }

        /// <summary>
        /// Converts an object to its
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToQueryString(this object obj) => ParseAnonymousObject(obj).ToQueryString();
    }


    
}
