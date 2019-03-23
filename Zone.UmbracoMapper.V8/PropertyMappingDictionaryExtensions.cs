namespace Zone.UmbracoMapper.V8
{
    using System.Collections.Generic;
    using System.Linq;
    using Zone.UmbracoMapper.Common;

    internal static class PropertyMappingDictionaryExtensions
    {
        internal static Dictionary<string, PropertyMappingBase> AsBaseDictionary(this Dictionary<string, PropertyMapping> propertyMapping)
        {
            return propertyMapping.ToDictionary(
                k => k.Key,
                v => (PropertyMappingBase)v.Value);
        }
    }
}
