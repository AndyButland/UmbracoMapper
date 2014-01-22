namespace Zone.UmbracoMapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Web;
    using System.Xml.Linq;
    using Newtonsoft.Json.Linq;
    using Umbraco.Core.Models;
    using Umbraco.Web;

    public class UmbracoMapper : IUmbracoMapper
    {
        #region Fields

        private Dictionary<string, Func<IUmbracoMapper, IPublishedContent, string, bool, object>> _customMappings;

        #endregion

        #region Constructor

        public UmbracoMapper()
        {
            _customMappings = new Dictionary<string, Func<IUmbracoMapper, IPublishedContent, string, bool, object>>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the root URL from where assets are served from in order to populate
        /// absolute URLs for media files (and support CDN delivery)
        /// </summary>
        public string AssetsRootUrl { get; set; }

        #endregion

        #region Interface methods

        /// <summary>
        /// Allows the mapper to use a custom mapping for a specified type
        /// </summary>
        /// <param name="propertyTypeFullName">Full name of the property type to map to</param>
        /// <param name="mapperFunction">Mapping function</param>
        public IUmbracoMapper AddCustomMapping(string propertyTypeFullName, Func<IUmbracoMapper, IPublishedContent, string, bool, object> mapperFunction)
        {
            _customMappings[propertyTypeFullName] = mapperFunction;
            return this;
        }

        /// <summary>
        /// Maps an instance of IPublishedContent to the passed view model based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="content">Instance of IPublishedContent</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(IPublishedContent content,
            T model,
            Dictionary<string, PropertyMapping> propertyMappings = null,
            string[] recursiveProperties = null)
        {
            if (content != null)
            {
                // Ensure model is not null
                if (model == null)
                {
                    throw new ArgumentNullException("model", "Object to map to cannot be null");
                }

                // Loop through all settable properties on model
                foreach (var property in model.GetType().GetProperties().Where(p => p.GetSetMethod() != null))
                {
                    // Check if we want to map to content at a level above the currently passed node
                    var contentToMapFrom = GetContentToMapFrom(content, propertyMappings, property.Name);

                    // Set native IPublishedContent properties (using convention that names match exactly)
                    var propName = GetMappedPropertyName(property.Name, propertyMappings);

                    if (contentToMapFrom.GetType().GetProperty(propName) != null)
                    {
                        MapNativeIPublishedContentProperty<T>(model, property, contentToMapFrom, propName);
                        continue;
                    }

                    // Set custom properties (using convention that names match but with camelCasing on IPublishedContent 
                    // properties, unless override provided)
                    propName = GetMappedPropertyName(property.Name, propertyMappings, true);                    

                    // Map properties, first checking for custom mappings
                    var isRecursiveProperty = IsRecursiveProperty(recursiveProperties, propName);
                    if (_customMappings.ContainsKey(property.PropertyType.FullName))
                    {
                        var value = _customMappings[property.PropertyType.FullName](this, contentToMapFrom, propName, isRecursiveProperty);
                        property.SetValue(model, value);
                    }
                    else
                    {
                        // Otherwise map types we can handle
                        var value = contentToMapFrom.GetPropertyValue(propName, isRecursiveProperty);
                        if (value != null)
                        {
                            // Check if we are mapping to a related IPublishedContent
                            if (IsMappingFromRelatedProperty(propertyMappings, property.Name))
                            {
                                // The value we have will either be:
                                //  - an Id of a related IPublishedContent
                                //  - or the related content itself (if the Umbraco Core Property Editor Converters are in use)
                                var relatedContentToMapFrom = value as IPublishedContent;
                                if (relatedContentToMapFrom == null)
                                {
                                    // It's not already IPublishedContent, so check using Id
                                    int relatedId;
                                    if (int.TryParse(value.ToString(), out relatedId))
                                    {
                                        // Get the related content
                                        var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
                                        relatedContentToMapFrom = umbracoHelper.TypedContent(relatedId);
                                    }
                                }

                                // If we have a related content item...
                                if (relatedContentToMapFrom != null)
                                {
                                    var relatedPropName = propertyMappings[property.Name].SourceRelatedProperty;
                                    // Get the mapped field from the related content
                                    if (relatedContentToMapFrom.GetType().GetProperty(relatedPropName) != null)
                                    {
                                        // Got a native field
                                        MapNativeIPublishedContentProperty<T>(model, property, relatedContentToMapFrom, relatedPropName);
                                    }
                                    else
                                    {
                                        // Otherwise look at a doc type field
                                        value = relatedContentToMapFrom.GetPropertyValue(relatedPropName);
                                        if (value != null)
                                        {
                                            // Map primitive types
                                            SetTypedPropertyValue(model, property, value.ToString());
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Map primitive types
                                SetTypedPropertyValue(model, property, value.ToString());
                            }
                        }
                    }
                }
            }

            return this;
        }        

        /// <summary>
        /// Maps content held in XML to the passed view model based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="xml">XML fragment to map from</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(XElement xml, 
            T model,
            Dictionary<string, PropertyMapping> propertyMappings = null)
        {
            if (xml != null)
            {
                // Ensure model is not null
                if (model == null)
                {
                    throw new ArgumentNullException("model", "Object to map to cannot be null");
                }

                // Loop through all settable properties on model
                foreach (var property in model.GetType().GetProperties().Where(p => p.GetSetMethod() != null))
                {
                    var propName = GetMappedPropertyName(property.Name, propertyMappings, false);

                    // If element with mapped name found, map the value (check case insensitively)
                    var mappedElement = GetXElementCaseInsensitive(xml, propName);

                    if (mappedElement != null)
                    {
                        // Check if we are looking for a child mapping
                        if (IsMappingFromChildProperty(propertyMappings, property.Name))
                        {
                            mappedElement = mappedElement.Element(propertyMappings[property.Name].SourceChildProperty);
                        }

                        if (mappedElement != null)
                        {
                            SetTypedPropertyValue<T>(model, property, mappedElement.Value);
                        }
                    }
                    else
                    {
                        // Try to see if it's in an attribute too
                        var mappedAttribute = GetXAttributeCaseInsensitive(xml, propName);
                        if (mappedAttribute != null)
                        {
                            SetTypedPropertyValue<T>(model, property, mappedAttribute.Value);
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Maps custom data held in a dictionary
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="dictionary">Dictionary of property name/value pairs</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(Dictionary<string, object> dictionary, 
            T model,
            Dictionary<string, PropertyMapping> propertyMappings = null)
        {
            if (dictionary != null)
            {
                // Ensure model is not null
                if (model == null)
                {
                    throw new ArgumentNullException("model", "Object to map to cannot be null");
                }

                // Loop through all settable properties on model
                foreach (var property in model.GetType().GetProperties().Where(p => p.GetSetMethod() != null))
                {
                    var propName = GetMappedPropertyName(property.Name, propertyMappings);

                    // If element with mapped name found, map the value
                    if (dictionary.ContainsKey(propName))
                    {
                        var stringValue = dictionary[propName].ToString();
                        SetTypedPropertyValue<T>(model, property, stringValue);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Maps information held in a JSON string
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="json">JSON string</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(string json,
            T model,
            Dictionary<string, PropertyMapping> propertyMappings = null)
        {
            if (!string.IsNullOrEmpty(json))
            {
                // Ensure model is not null
                if (model == null)
                {
                    throw new ArgumentNullException("model", "Object to map to cannot be null");
                }

                // Parse JSON string to queryable object
                var jsonObj = JObject.Parse(json);

                // Loop through all settable properties on model
                foreach (var property in model.GetType().GetProperties().Where(p => p.GetSetMethod() != null))
                {
                    var propName = GetMappedPropertyName(property.Name, propertyMappings, false);

                    // If element with mapped name found, map the value
                    var childPropName = string.Empty;
                    if (IsMappingFromChildProperty(propertyMappings, property.Name))
                    {
                        childPropName = propertyMappings[property.Name].SourceChildProperty;
                    }

                    var stringValue = GetJsonFieldCaseInsensitive(jsonObj, propName, childPropName);
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        SetTypedPropertyValue<T>(model, property, stringValue);
                    }                    
                }
            }

            return this;
        }

        /// <summary>
        /// Maps a collection of IPublishedContent to the passed view model
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="contentCollection">Collection of IPublishedContent</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection, 
            IList<T> modelCollection,
            Dictionary<string, PropertyMapping> propertyMappings = null,
            string[] recursiveProperties = null) where T : new()
        {
            if (contentCollection != null)
            {
                if (modelCollection == null)
                {
                    throw new ArgumentNullException("modelCollection", "Collection to map to can be empty, but not null");
                }

                foreach (var item in contentCollection)
                {
                    var itemToCreate = new T();
                    Map<T>(item, itemToCreate, propertyMappings, recursiveProperties);
                    modelCollection.Add(itemToCreate);
                }
            }

            return this;
        }

        /// <summary>
        /// Maps a collection of content held in XML to the passed view model collection based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="xml">XML fragment to map from</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="groupElementName">Name of the element grouping each item in the XML (defaults to "Item")</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this XML element is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(XElement xml, IList<T> modelCollection, 
            Dictionary<string, PropertyMapping> propertyMappings = null, 
            string groupElementName = "item", 
            bool createItemsIfNotAlreadyInList = true, 
            string sourceIdentifyingPropName = "Id", 
            string destIdentifyingPropName = "Id") where T : new()
        {
            if (xml != null)
            {
                if (modelCollection == null)
                {
                    throw new ArgumentNullException("modelCollection", "Collection to map to can be empty, but not null");
                }

                // Loop through each of the items defined in the XML
                foreach (var element in xml.Elements(groupElementName))
                {
                    // Check if item is already in the list by looking up provided unique key
                    T itemToUpdate = default(T);
                    if (TypeHasProperty(typeof(T), destIdentifyingPropName))
                    {
                        itemToUpdate = GetExistingItemFromCollection(modelCollection, destIdentifyingPropName, element.Element(sourceIdentifyingPropName).Value);
                    }

                    if (itemToUpdate != null)
                    {
                        // Item found, so map it
                        Map<T>(element, itemToUpdate, propertyMappings);
                    }
                    else
                    {
                        // Item not found, so create if that was requested
                        if (createItemsIfNotAlreadyInList)
                        {
                            var itemToCreate = new T();
                            Map<T>(element, itemToCreate, propertyMappings);
                            modelCollection.Add(itemToCreate);
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Maps a collection custom data held in an linked dictionary to a collection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="dictionaries">Collection of custom data containing a list of dictionary of property name/value pairs.  One of these keys provides a lookup for the existing collection.</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this dictionary key is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries, 
            IList<T> modelCollection, 
            Dictionary<string, PropertyMapping> propertyMappings = null, 
            bool createItemsIfNotAlreadyInList = true, 
            string sourceIdentifyingPropName = "Id", 
            string destIdentifyingPropName = "Id") where T : new()
        {
            if (dictionaries != null)
            {
                if (modelCollection == null)
                {
                    throw new ArgumentNullException("modelCollection", "Collection to map to can be empty, but not null");
                }

                // Loop through each of the items defined in the dictionary
                foreach (var customDataItem in dictionaries)
                {
                    // Check if item is already in the list by looking up provided unique key
                    T itemToUpdate = default(T);
                    if (TypeHasProperty(typeof(T), destIdentifyingPropName))
                    {
                        itemToUpdate = GetExistingItemFromCollection(modelCollection, destIdentifyingPropName, customDataItem[sourceIdentifyingPropName].ToString());
                    }

                    if (itemToUpdate != null)
                    {
                        // Item found, so map it
                        Map<T>(customDataItem, itemToUpdate, propertyMappings);
                    }
                    else
                    {
                        // Item not found, so create if that was requested
                        if (createItemsIfNotAlreadyInList)
                        {
                            var itemToCreate = new T();
                            Map<T>(customDataItem, itemToCreate, propertyMappings);
                            modelCollection.Add(itemToCreate);
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Maps a collection custom data held in a JSON string to a collection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="json">JSON string containing collection</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="rootElementName">Name of root element in JSON array</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this dictionary key is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(string json, IList<T> modelCollection,
            Dictionary<string, PropertyMapping> propertyMappings = null,
            string rootElementName = "items", 
            bool createItemsIfNotAlreadyInList = true,
            string sourceIdentifyingPropName = "Id",
            string destIdentifyingPropName = "Id") where T : new()
        {
            if (!string.IsNullOrEmpty(json))
            {
                if (modelCollection == null)
                {
                    throw new ArgumentNullException("modelCollection", "Collection to map to can be empty, but not null");
                }

                // Loop through each of the items defined in the JSON
                var jsonObject = JObject.Parse(json);
                foreach (var element in jsonObject[rootElementName].Children())
                {
                    // Check if item is already in the list by looking up provided unique key
                    T itemToUpdate = default(T);
                    if (TypeHasProperty(typeof(T), destIdentifyingPropName))
                    {
                        itemToUpdate = GetExistingItemFromCollection(modelCollection, destIdentifyingPropName, element[sourceIdentifyingPropName].Value<string>());
                    }

                    if (itemToUpdate != null)
                    {
                        // Item found, so map it
                        Map<T>(element.ToString(), itemToUpdate, propertyMappings);
                    }
                    else
                    {
                        // Item not found, so create if that was requested
                        if (createItemsIfNotAlreadyInList)
                        {
                            var itemToCreate = new T();
                            Map<T>(element.ToString(), itemToCreate, propertyMappings);
                            modelCollection.Add(itemToCreate);
                        }
                    }
                }
            }

            return this;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Helper method to find the property name to map to based on conventions (and/or overrides)
        /// </summary>
        /// <param name="propName">Name of property to map to</param>
        /// <param name="propertyMappings">Set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="convertToCamelCase">Flag for whether to convert property name to camel casing before attempting mapping</param>
        /// <returns>Name of property to map from</returns>
        private string GetMappedPropertyName(string propName, Dictionary<string, PropertyMapping> propertyMappings,
            bool convertToCamelCase = false)
        {
            var mappedName = propName;
            if (propertyMappings != null && 
                propertyMappings.ContainsKey(propName) &&
                !string.IsNullOrEmpty(propertyMappings[propName].SourceProperty))
            {
                mappedName = propertyMappings[propName].SourceProperty;
            }

            if (convertToCamelCase)
            {
                mappedName = CamelCase(mappedName);
            }

            return mappedName;
        }

        /// <summary>
        /// Gets the IPublishedContent to map from.  Normally this will be the one passed but it's possible to map at a level above the current node.
        /// </summary>
        /// <param name="content">Passed content to map from</param>
        /// <param name="propertyMappings">Dictionary of properties and levels to map from</param>
        /// <param name="propName">Name of property to map</param>
        /// <returns>Instance of IPublishedContent to map from</returns>
        private IPublishedContent GetContentToMapFrom(IPublishedContent content, Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            var contentToMapFrom = content;
            if (propertyMappings != null && propertyMappings.ContainsKey(propName))
            {
                for (int i = 0; i < propertyMappings[propName].LevelsAbove; i++)
                {
                    contentToMapFrom = contentToMapFrom.Parent;
                }
            }

            return contentToMapFrom;
        }

        /// <summary>
        /// Helper to check if particular property should be mapped from a related property
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>True if mapping should be from child property</returns>
        private bool IsMappingFromRelatedProperty(Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            return propertyMappings != null &&
                propertyMappings.ContainsKey(propName) &&
                !string.IsNullOrEmpty(propertyMappings[propName].SourceRelatedProperty);
        }

        /// <summary>
        /// Helper to check if particular property should be mapped from a child property
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>True if mapping should be from child property</returns>
        private bool IsMappingFromChildProperty(Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            return propertyMappings != null &&
                propertyMappings.ContainsKey(propName) &&
                !string.IsNullOrEmpty(propertyMappings[propName].SourceChildProperty);
        }

        /// <summary>
        /// Helper to check whether given property is defined as recursive
        /// </summary>
        /// <param name="recursiveProperties">Array of recursive property names</param>
        /// <param name="propertyName">Name of property</param>
        /// <returns>True if in list of recursive properties</returns>
        private bool IsRecursiveProperty(string[] recursiveProperties, string propertyName)
        {
            return recursiveProperties != null && recursiveProperties.Contains(propertyName);
        }

        /// <summary>
        /// Helper to map a native IPublishedContent property to a view model property
        /// </summary>
        /// <typeparam name="T">Type of view model to map to</typeparam>
        /// <param name="model">View model to map to</param>
        /// <param name="property">Property to map to</param>
        /// <param name="contentToMapFrom">IPublishedContent instance to map from</param>
        /// <param name="propName">Name of property to map from</param>
        private void MapNativeIPublishedContentProperty<T>(T model, PropertyInfo property, IPublishedContent contentToMapFrom, string propName)
        {
            // If we are mapping to a string, make sure to call ToString().  That way even if the source property is numeric, it'll be mapped.
            if (property.PropertyType.Name == "String")
            {
                property.SetValue(model, contentToMapFrom.GetType().GetProperty(propName).GetValue(contentToMapFrom).ToString());
            }
            else
            {
                property.SetValue(model, contentToMapFrom.GetType().GetProperty(propName).GetValue(contentToMapFrom));
            }
        }

        /// <summary>
        /// Helper method to convert a string value to an appropriate type for setting via reflection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">View model to map to</param>
        /// <param name="property">Property to map to</param>
        /// <param name="stringValue">String representation of property value</param>
        private void SetTypedPropertyValue<T>(T model, PropertyInfo property, string stringValue)
        {
            switch (property.PropertyType.Name)
            {
                case "Boolean":
                    bool boolValue;
                    if (bool.TryParse(stringValue, out boolValue))
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
                        property.SetValue(model, dateTimeValue);
                    }

                    break;
                case "IHtmlString":
                    var htmlString = new HtmlString(stringValue);
                    property.SetValue(model, htmlString);
                    break;
                case "String":                
                    property.SetValue(model, stringValue);
                    break;
            }
        }

        /// <summary>
        /// Helper to check if a given type has a property
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <param name="propName">Name of property</param>
        /// <returns>True of property exists on type</returns>
        private bool TypeHasProperty(Type type, string propName)
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
        private T GetExistingItemFromCollection<T>(IList<T> modelCollection, string modelPropertyName, string valueToMatch) where T : new()
        {
            return modelCollection
                .Where(x => x.GetType()
                    .GetProperties()
                    .Single(p => p.Name == modelPropertyName)
                    .GetValue(x).ToString().ToLowerInvariant() == valueToMatch.ToLowerInvariant())
                .SingleOrDefault();
        }

        /// <summary>
        /// Helper method to convert a string into camel case
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Camel cased string</returns>
        private string CamelCase(string input)
        {
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Helper to retrieve an XElement by name case insensitively
        /// </summary>
        /// <param name="xml">Xml fragment to search in</param>
        /// <param name="propName">Element name to look up</param>
        /// <returns>Matched XElement</returns>
        private XElement GetXElementCaseInsensitive(XElement xml, string propName)
        {
            if (xml != null)
            {
                return xml.Elements().SingleOrDefault(s => string.Compare(s.Name.ToString(), propName, true) == 0);
            }

            return null;
        }

        /// <summary>
        /// Helper to retrieve an XAttribute by name case insensitively
        /// </summary>
        /// <param name="xml">Xml fragment to search in</param>
        /// <param name="propName">Element name to look up</param>
        /// <returns>Matched XAttribue</returns>
        private XAttribute GetXAttributeCaseInsensitive(XElement xml, string propName)
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
        private string GetJsonFieldCaseInsensitive(JObject jsonObj, string propName, string childPropName)
        {
            var token = GetJToken(jsonObj, propName);
            if (token != null)
            {
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
            }           

            return string.Empty;
        }

        /// <summary>
        /// Lookup for JSON property
        /// </summary>
        /// <param name="jsonObj">JSON object</param>
        /// <param name="propName">Property to get value for</param>
        /// <returns>JToken object, if found</returns>
        private JToken GetJToken(JObject jsonObj, string propName)
        {
            var token = jsonObj[propName];

            // If not found, try with lower case
            if (token == null)
            {
                token = jsonObj[propName.ToLowerInvariant()];
            }

            // If still not found, try with camel case
            if (token == null)
            {
                token = jsonObj[CamelCase(propName)];
            }

            return token;
        }

        #endregion
    }
}
