using System.Collections;
using System.Collections.Generic;

namespace RIoT2.Connector.InfluxDB.Services
{
    /// <summary>
    /// Flattens nested entity objects (as returned by RIoT2.Core's ValueModel.GetAsObject()
    /// for ValueType.Entity, typically a System.Dynamic.ExpandoObject) into a flat list of
    /// dot-separated paths mapped to their scalar (boolean/number) leaf values.
    /// </summary>
    public static class EntityFlattener
    {
        public static IEnumerable<(string Path, object Value)> Flatten(object entity, string prefix = "")
        {
            if (entity is IDictionary<string, object> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    var path = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";

                    if (kvp.Value is IDictionary<string, object>)
                    {
                        foreach (var nested in Flatten(kvp.Value, path))
                            yield return nested;
                    }
                    else if (IsSupportedLeafValue(kvp.Value))
                    {
                        yield return (path, kvp.Value);
                    }
                    //unsupported leaf types (string, null, arrays/lists) are skipped
                }
            }
        }

        private static bool IsSupportedLeafValue(object value)
        {
            return value is bool
                || value is int
                || value is long
                || value is double
                || value is float
                || value is decimal;
        }
    }
}
