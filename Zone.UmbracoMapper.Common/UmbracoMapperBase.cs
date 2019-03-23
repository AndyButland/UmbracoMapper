namespace Zone.UmbracoMapper.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Web;
    using System.Xml.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Base class for implementations of the main mapping class in Umbraco version specific assemblies.
    /// Contains various helpers that can be used without an Umbraco dependency.
    /// </summary>
    public abstract class UmbracoMapperBase
    {
        /// <summary>
        /// Provides a cache of view model settable properties, only need to use reflection once for each view model within the 
        /// application lifetime
        /// </summary>
        private static readonly ConcurrentDictionary<string, IList<PropertyInfo>> _settableProperties = new ConcurrentDictionary<string, IList<PropertyInfo>>();
        
        /// <summary>
        /// Gets or sets the root URL from where assets are served from in order to populate
        /// absolute URLs for media files (and support CDN delivery)
        /// </summary>
        public string AssetsRootUrl { get; set; }

        /// <summary>
        /// Gets or sets a flag enabling caching.  On by default.
        /// </summary>
        public bool EnableCaching { get; set; }

        protected static MethodInfo GetCustomMappingMethod(IReflect type, string methodName)
        {
            return type.GetMethod(methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Helper to get the settable properties from a model for mapping from the cache or the model object
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">Instance of view model</param>
        /// <returns>Enumerable of settable properties from the model</returns>
        protected IEnumerable<PropertyInfo> SettableProperties<T>(T model) where T : class
        {
            if (!EnableCaching)
            {
                return SettablePropertiesFromObject(model);
            }

            var cacheKey = model.GetType().FullName;
            if (!_settableProperties.ContainsKey(cacheKey))
            {
                _settableProperties[cacheKey] = SettablePropertiesFromObject(model);
            }

            return _settableProperties[cacheKey];
        }

        /// <summary>
        /// Helper to get the settable properties from a model for mapping from the cache or the model object
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">Instance of view model</param>
        /// <returns>Enumerable of settable properties from the model</returns>
        private static IList<PropertyInfo> SettablePropertiesFromObject<T>(T model) where T : class
        {
            return model.GetType().GetProperties()
                .Where(p => p.GetSetMethod() != null)
                .ToList();
        }

        protected static bool AreMappingToString(PropertyInfo property)
        {
            return property.PropertyType.Name == "String";
        }

        protected static string GetStringValueOrEmpty<T>(T model, PropertyInfo property)
        {
            return property.GetValue(model)?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Helper to check whether given property is defined as recursive
        /// </summary>
        /// <param name="recursiveProperties">Array of recursive property names</param>
        /// <param name="propertyName">Name of property</param>
        /// <returns>True if in list of recursive properties</returns>
        protected static bool IsRecursiveProperty(string[] recursiveProperties, string propertyName)
        {
            return recursiveProperties != null && recursiveProperties.Contains(propertyName);
        }

        /// <summary>
        /// Helper method to convert a string value to an appropriate type for setting via reflection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">View model to map to</param>
        /// <param name="property">Property to map to</param>
        /// <param name="stringValue">String representation of property value</param>
        /// <param name="concatenateToExistingValue">Flag for if we want to concatenate the value to the existing value</param>
        /// <param name="concatenationSeperator">If using concatenation, use this string to separate items</param>
        /// <param name="coalesceWithExistingValue">Flag for if we want to coalesce the value to the existing value</param>
        /// <param name="stringValueFormatter">A function for transformation of the string value, if passed</param>
        /// <param name="propertySet">Set of properties to map. This function will only run for All and Custom</param>
        protected static void SetTypedPropertyValue<T>(T model, PropertyInfo property, string stringValue,
                                                       bool concatenateToExistingValue = false, string concatenationSeperator = "",
                                                       bool coalesceWithExistingValue = false,
                                                       Func<string, string> stringValueFormatter = null,
                                                       PropertySet propertySet = PropertySet.All)
        {
            if (propertySet != PropertySet.All && propertySet != PropertySet.Custom)
            {
                return;
            }

            var propertyTypeName = property.PropertyType.Name;
            var isNullable = false;
            if (propertyTypeName == "Nullable`1" && property.PropertyType.GenericTypeArguments.Length == 1)
            {
                propertyTypeName = property.PropertyType.GenericTypeArguments[0].Name;
                isNullable = true;
            }

            switch (propertyTypeName)
            {
                case "Boolean":
                    bool boolValue;
                    if (stringValue == "1" || stringValue == "0")
                    {
                        // Special case: Archetype stores "1" for boolean true, so we'll handle that convention
                        property.SetValue(model, stringValue == "1");
                    }
                    else if (bool.TryParse(stringValue, out boolValue))
                    {
                        property.SetValue(model, boolValue);
                    }

                    break;
                case "Byte":
                    byte byteValue;
                    if (byte.TryParse(stringValue, out byteValue))
                    {
                        property.SetValue(model, byteValue);
                    }

                    break;
                case "Int16":
                    short shortValue;
                    if (short.TryParse(stringValue, out shortValue))
                    {
                        property.SetValue(model, shortValue);
                    }

                    break;
                case "Int32":
                    int intValue;
                    if (int.TryParse(stringValue, out intValue))
                    {
                        property.SetValue(model, intValue);
                    }

                    break;
                case "Int64":
                    long longValue;
                    if (long.TryParse(stringValue, out longValue))
                    {
                        property.SetValue(model, longValue);
                    }

                    break;
                case "Decimal":
                    decimal decimalValue;
                    if (decimal.TryParse(stringValue, out decimalValue))
                    {
                        property.SetValue(model, decimalValue);
                    }

                    break;
                case "Float":
                    float floatValue;
                    if (float.TryParse(stringValue, out floatValue))
                    {
                        property.SetValue(model, floatValue);
                    }

                    break;
                case "Double":
                    double doubleValue;
                    if (double.TryParse(stringValue, out doubleValue))
                    {
                        property.SetValue(model, doubleValue);
                    }

                    break;
                case "DateTime":
                    DateTime dateTimeValue;
                    if (DateTime.TryParse(stringValue, out dateTimeValue))
                    {
                        // Umbraco returns DateTime.MinValue if no date set.  If mapping to a nullable date time makes more sense
                        // to leave as null if this value is returned.
                        if (dateTimeValue > DateTime.MinValue || !isNullable)
                        {
                            property.SetValue(model, dateTimeValue);
                        }
                    }

                    break;
                case "IHtmlString":
                    var htmlString = new HtmlString(stringValue);
                    property.SetValue(model, htmlString);
                    break;
                case "String":

                    // Only supporting/makes sense to allow concatenation and coalescing for String type
                    if (concatenateToExistingValue)
                    {
                        var prefixValueWith = property.GetValue(model) + concatenationSeperator;
                        property.SetValue(model, prefixValueWith + stringValue);
                    }
                    else if (coalesceWithExistingValue)
                    {
                        // Check is existing value and only set if it's null, empty or whitespace
                        if (property.GetValue(model) == null || string.IsNullOrWhiteSpace(property.GetValue(model).ToString()))
                        {
                            property.SetValue(model, stringValue);
                        }
                    }
                    else if (stringValueFormatter != null)
                    {
                        property.SetValue(model, stringValueFormatter(stringValue));
                    }
                    else if (!string.IsNullOrEmpty(stringValue))
                    {
                        property.SetValue(model, stringValue);
                    }

                    break;
            }
        }

        /// <summary>
        /// Helper to map a value read from a native IPublishedContent property to a view model property
        /// </summary>
        /// <typeparam name="T">Type of view model to map to</typeparam>
        /// <param name="model">View model to map to</param>
        /// <param name="property">Property to map to</param>
        /// <param name="concatenateToExistingValue">Flag for if we want to concatenate the value to the existing value</param>
        /// <param name="concatenationSeperator">If using concatenation, use this string to separate items</param>
        /// <param name="coalesceWithExistingValue">Flag for if we want to coalesce the value to the existing value</param>
        /// <param name="stringValueFormatter">A function for transformation of the string value, if passed</param>
        /// <param name="value">Value to map</param>
        protected static void MapNativeContentPropertyValue<T>(T model, PropertyInfo property,
                                                               bool concatenateToExistingValue, string concatenationSeperator,
                                                               bool coalesceWithExistingValue,
                                                               Func<string, string> stringValueFormatter,
                                                               object value)
        {
            // If we are mapping to a string, make sure to call ToString().  That way even if the source property is numeric, it'll be mapped.
            // Concatenation and coalescing only supported/makes sense in this case too.
            if (property.PropertyType.Name == "String")
            {
                var stringValue = value.ToString();
                if (concatenateToExistingValue)
                {
                    var prefixValueWith = property.GetValue(model) + concatenationSeperator;
                    property.SetValue(model, prefixValueWith + stringValue);
                }
                else if (coalesceWithExistingValue)
                {
                    // Check is existing value and only set if it's null, empty or whitespace
                    var existingValue = property.GetValue(model);
                    if (existingValue == null || string.IsNullOrWhiteSpace(existingValue.ToString()))
                    {
                        property.SetValue(model, stringValue);
                    }
                }
                else if (stringValueFormatter != null)
                {
                    property.SetValue(model, stringValueFormatter(stringValue));
                }
                else
                {
                    property.SetValue(model, stringValue);
                }
            }
            else
            {
                property.SetValue(model, value);
            }
        }

        /// <summary>
        /// Helper to check if a given type has a property
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <param name="propName">Name of property</param>
        /// <returns>True of property exists on type</returns>
        protected static bool TypeHasProperty(Type type, string propName)
        {
            return type
                .GetProperties()
                .SingleOrDefault(p => p.Name == propName) != null;
        }

        /// <summary>
        /// Helper method to get an existing item from the model collection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="modelPropertyName">Model property name to look up</param>
        /// <param name="valueToMatch">Property value to match on</param>
        /// <returns>Single instance of T if found in the collection</returns>
        protected static T GetExistingItemFromCollection<T>(IEnumerable<T> modelCollection, string modelPropertyName, string valueToMatch) where T : new()
        {
            return modelCollection.SingleOrDefault(x => x.GetType()
                                                         .GetProperties()
                                                         .Single(p => p.Name == modelPropertyName)
                                                         .GetValue(x).ToString().ToLowerInvariant() == valueToMatch.ToLowerInvariant());
        }

        /// <summary>
        /// Helper method to convert a string into camel case
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Camel cased string</returns>
        protected static string CamelCase(string input)
        {
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Helper to retrieve an XElement by name case insensitively
        /// </summary>
        /// <param name="xml">Xml fragment to search in</param>
        /// <param name="propName">Element name to look up</param>
        /// <returns>Matched XElement</returns>
        protected static XElement GetXElementCaseInsensitive(XElement xml, string propName)
        {
            return xml?.Elements().SingleOrDefault(s => string.Compare(s.Name.ToString(), propName, true) == 0);
        }

        /// <summary>
        /// Helper to retrieve an XAttribute by name case insensitively
        /// </summary>
        /// <param name="xml">Xml fragment to search in</param>
        /// <param name="propName">Element name to look up</param>
        /// <returns>Matched XAttribute</returns>
        protected static XAttribute GetXAttributeCaseInsensitive(XElement xml, string propName)
        {
            return xml.Attributes().SingleOrDefault(s => string.Compare(s.Name.ToString(), propName, true) == 0);
        }

        /// <summary>
        /// Helper to retrieve a JSON field by name case insensitively
        /// </summary>
        /// <param name="jsonObj">JSON object to get field value from</param>
        /// <param name="propName">Property name to look up</param>
        /// <param name="childPropName">Child property name to look up</param>
        /// <returns>String value of JSON field</returns>
        protected string GetJsonFieldCaseInsensitive(JObject jsonObj, string propName, string childPropName)
        {
            var token = GetJToken(jsonObj, propName);
            if (token == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(childPropName))
            {
                // Looking up on child object
                var childToken = token[childPropName];

                // If not found, try with lower case
                if (childToken == null)
                {
                    childToken = token[childPropName.ToLowerInvariant()];
                }

                // If still not found, try with camel case
                if (childToken == null)
                {
                    childToken = token[CamelCase(childPropName)];
                }

                if (childToken != null)
                {
                    return (string)childToken;
                }
            }
            else
            {
                // Looking up directly on object
                return (string)token;
            }

            return string.Empty;
        }

        /// <summary>
        /// Lookup for JSON property
        /// </summary>
        /// <param name="jsonObj">JSON object</param>
        /// <param name="propName">Property to get value for</param>
        /// <returns>JToken object, if found</returns>
        private static JToken GetJToken(JObject jsonObj, string propName)
        {
            // If not found, try with lower case, ff still not found, try with camel case
            return (jsonObj[propName] ?? jsonObj[propName.ToLowerInvariant()]) ?? jsonObj[CamelCase(propName)];
        }

        /// <summary>
        /// Helper to determine the type of a generic collection
        /// </summary>
        /// <param name="property">Property info for collection</param>
        /// <returns>Type of collection</returns>
        protected static Type GetGenericCollectionType(PropertyInfo property)
        {
            return property.PropertyType.GetTypeInfo().GenericTypeArguments[0];
        }

        /// <summary>
        /// Helper to set the default value of a mapped property, if provided and mapping operation hasn't already found a value
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <param name="property">Property of view model to map to</param>
        protected static void SetDefaultValueIfProvided<T>(T model, IReadOnlyDictionary<string, PropertyMappingBase> propertyMappings, PropertyInfo property)
        {
            if (HasDefaultValue(propertyMappings, property.Name))
            {
                property.SetValue(model, propertyMappings[property.Name].DefaultValue);
            }
        }

        /// <summary>
        /// Helper to check if particular property has a default value
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>True if mapping should be from child property</returns>
        private static bool HasDefaultValue(IReadOnlyDictionary<string, PropertyMappingBase> propertyMappings, string propName)
        {
            return propertyMappings.ContainsKey(propName) && propertyMappings[propName].DefaultValue != null;
        }
    }
}
