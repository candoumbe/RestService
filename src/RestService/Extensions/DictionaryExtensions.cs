using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Converts a dictionary to its representation
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static string ToQueryString(this IDictionary<string, object> dictionary)
        {
            dictionary = dictionary ?? new Dictionary<string, object>();
            
            StringBuilder sb = new StringBuilder();
            IEnumerable<KeyValuePair<string, object>> keysAndValues = dictionary.Where(kv => kv.Value != null);
            foreach (var kv in keysAndValues)
            {
                sb = sb.Append($"{(sb.Length > 0 ? "&" : string.Empty)}{kv.Key}={kv.Value}");
            }


            return sb.ToString();
        }
    }
}
