using Newtonsoft.Json;
using System.Reflection;

namespace BookMoth_Api_With_C_.ZaloPay.Extension
{
    public static class ObjectExtension
    {
        public static Dictionary<string, string> AsParams(this object source)
        {
            return source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(
                    propInfo => propInfo.Name.ToLower(),
                    propInfo =>
                    {
                        var value = propInfo.GetValue(source, null);
                        return (value is string || value == null) ? value?.ToString() : JsonConvert.SerializeObject(value);
                    }
                );
        }

    }

    public static class DictionaryExtension
    {
        public static object GetOrDefault(this Dictionary<string, object> source, string key, object defaultValue)
        {
            return source.ContainsKey(key) ? source[key] : defaultValue;
        }
    }
}