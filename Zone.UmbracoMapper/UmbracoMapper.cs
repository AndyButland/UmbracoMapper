﻿namespace Zone.UmbracoMapper
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
    using DampModel = DAMP.PropertyEditorValueConverter.Model;

    public class UmbracoMapper : IUmbracoMapper
    {
        public UmbracoMapper()
        {
        }

        #region Properties

        /// <summary>
        /// Gets or sets the root URL from where assets are served from in order to populate
        /// absolute URLs for media files (and support CDN delivery)
        /// </summary>
        public string AssetsRootUrl { get; set; }

        #endregion

        #region Interface methods

        /// <summary>
        /// Maps an instance of IPublishedContent to the passed view model based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="content">Instance of IPublishedContent</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(IPublishedContent content, 
            T model, 
            Dictionary<string, string> propertyNameMappings = null,
            string[] recursiveProperties = null)
        {
            // Loop through all settable properties on model
            foreach (var property in model.GetType().GetProperties().Where(p => p.GetSetMethod() != null))
            {
                // Set native IPublishedContent properties (using convention that names match exactly)
                var propName = GetMappedPropertyName(property.Name, propertyNameMappings);
                if (content.GetType().GetProperty(propName) != null)
                {
                    property.SetValue(model, content.GetType().GetProperty(propName).GetValue(content));
                    continue;
                }

                // Set custom properties (using convention that names match but with camelCasing on IPublishedContent 
                // properties, unless override provided)
                propName = GetMappedPropertyName(property.Name, propertyNameMappings, true);

                // Map property for types we can handle
                switch (property.PropertyType.Name)
                {
                    case "MediaFile":
                        var mf = GetMediaFile(content.GetPropertyValue<DampModel>(propName));
                        property.SetValue(model, mf);
                        break;

                    default:
                        var value = content.GetPropertyValue(propName, IsRecursiveProperty(recursiveProperties, propName));
                        if (value != null)
                        {
                            SetTypedPropertyValue(model, property, value.ToString());
                        }

                        break;

                    // TODO: further type mappings (date, numbers, boolean)
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
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(XElement xml, 
            T model,
            Dictionary<string, string> propertyNameMappings = null)
        {
            // Loop through all settable properties on model
            foreach (var property in model.GetType().GetProperties().Where(p => p.GetSetMethod() != null))
            {
                var propName = GetMappedPropertyName(property.Name, propertyNameMappings, false);

                // If element with mapped name found, map the value (check case insensitively)
                var mappedElement = GetXElementCaseInsensitive(xml, propName);
                if (mappedElement != null)
                {
                    var stringValue = mappedElement.Value;
                    SetTypedPropertyValue<T>(model, property, stringValue);
                }
                else
                {
                    // Try to see if it's in an attribute too
                    var mappedAttribute = GetXAttributeCaseInsensitive(xml, propName);
                    if (mappedAttribute != null)
                    {
                        var stringValue = mappedAttribute.Value;
                        SetTypedPropertyValue<T>(model, property, stringValue);
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
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(Dictionary<string, object> dictionary, 
            T model,
            Dictionary<string, string> propertyNameMappings = null)
        {
            // Loop through all settable properties on model
            foreach (var property in model.GetType().GetProperties().Where(p => p.GetSetMethod() != null))
            {
                var propName = GetMappedPropertyName(property.Name, propertyNameMappings);

                // If element with mapped name found, map the value
                if (dictionary.ContainsKey(propName))
                {
                    var stringValue = dictionary[propName].ToString();
                    SetTypedPropertyValue<T>(model, property, stringValue);
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
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(string json,
            T model,
            Dictionary<string, string> propertyNameMappings = null)
        {
            if (!string.IsNullOrEmpty(json))
            {
                // Parse JSON string to queryable object
                var jsonObj = JObject.Parse(json);

                // Loop through all settable properties on model
                foreach (var property in model.GetType().GetProperties().Where(p => p.GetSetMethod() != null))
                {
                    var propName = GetMappedPropertyName(property.Name, propertyNameMappings, false);

                    // If element with mapped name found, map the value
                    var stringValue = GetJsonFieldCaseInsensitive(jsonObj, propName);
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
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection, 
            IList<T> modelCollection,
            Dictionary<string, string> propertyNameMappings = null,
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
                    Map<T>(item, itemToCreate, propertyNameMappings, recursiveProperties);
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
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="groupElementName">Name of the element grouping each item in the XML (defaults to "Item")</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this XML element is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(XElement xml, IList<T> modelCollection, 
            Dictionary<string, string> propertyNameMappings = null, 
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
                        Map<T>(element, itemToUpdate, propertyNameMappings);
                    }
                    else
                    {
                        // Item not found, so create if that was requested
                        if (createItemsIfNotAlreadyInList)
                        {
                            var itemToCreate = new T();
                            Map<T>(element, itemToCreate, propertyNameMappings);
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
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this dictionary key is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries, 
            IList<T> modelCollection, 
            Dictionary<string, string> propertyNameMappings = null, 
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
                        Map<T>(customDataItem, itemToUpdate, propertyNameMappings);
                    }
                    else
                    {
                        // Item not found, so create if that was requested
                        if (createItemsIfNotAlreadyInList)
                        {
                            var itemToCreate = new T();
                            Map<T>(customDataItem, itemToCreate, propertyNameMappings);
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
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="rootElementName">Name of root element in JSON array</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this dictionary key is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(string json, IList<T> modelCollection,
            Dictionary<string, string> propertyNameMappings = null,
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
                        Map<T>(element.ToString(), itemToUpdate, propertyNameMappings);
                    }
                    else
                    {
                        // Item not found, so create if that was requested
                        if (createItemsIfNotAlreadyInList)
                        {
                            var itemToCreate = new T();
                            Map<T>(element.ToString(), itemToCreate, propertyNameMappings);
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
        /// <param name="propertyNameMappings">Set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="convertToCamelCase">Flag for whether to convert property name to camel casing before attempting mapping</param>
        /// <returns>Name of property to map from</returns>
        private string GetMappedPropertyName(string propName, Dictionary<string, string> propertyNameMappings,
            bool convertToCamelCase = false)
        {
            var mappedName = propName;
            if (propertyNameMappings != null && propertyNameMappings.ContainsKey(propName))
            {
                mappedName = propertyNameMappings[propName];
            }

            if (convertToCamelCase)
            {
                mappedName = CamelCase(mappedName);
            }

            return mappedName;
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
            return xml.Elements().SingleOrDefault(s => string.Compare(s.Name.ToString(), propName, true) == 0);
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
        /// <param name="jsonObj"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        private string GetJsonFieldCaseInsensitive(JObject jsonObj, string propName)
        {
            var stringValue = (string)jsonObj[propName];

            // If not found, try with lower case
            if (string.IsNullOrEmpty(stringValue))
            {
                stringValue = (string)jsonObj[propName.ToLowerInvariant()];
            }

            // If still not found, try with camel case
            if (string.IsNullOrEmpty(stringValue))
            {
                stringValue = (string)jsonObj[CamelCase(propName)];
            }

            return stringValue;
        }

        #endregion

        #region Specific type converters

        /// <summary>
        /// Helper to convert a DAMP model into a standard MediaFile object
        /// </summary>
        /// <param name="dampModel">DAMP model</param>
        /// <returns>MediaFile instance</returns>
        private MediaFile GetMediaFile(DampModel dampModel)
        {
            if (dampModel != null && dampModel.Any)
            {
                var mediaFile = new MediaFile();

                var dampModelItem = dampModel.First;

                mediaFile.Id = dampModelItem.Id;
                mediaFile.Name = dampModelItem.Name;
                mediaFile.Url = dampModelItem.Url;
                mediaFile.DomainWithUrl = AssetsRootUrl + dampModelItem.Url;
                mediaFile.DocumentTypeAlias = dampModelItem.Type;

                if (dampModelItem.Type == "Image")
                {
                    int tempWidth;
                    if (int.TryParse(dampModelItem.GetProperty("umbracoWidth"), out tempWidth))
                    {
                        mediaFile.Width = tempWidth;
                    }

                    int tempHeight;
                    if (int.TryParse(dampModelItem.GetProperty("umbracoHeight"), out tempHeight))
                    {
                        mediaFile.Height = tempHeight;
                    }
                }

                int tempSize;
                if (int.TryParse(dampModelItem.GetProperty("umbracoBytes"), out tempSize))
                {
                    mediaFile.Size = tempSize;
                }

                mediaFile.FileExtension = dampModelItem.GetProperty("umbracoExtension");

                mediaFile.AltText = string.IsNullOrWhiteSpace(dampModelItem.GetProperty("altText"))
                    ? dampModelItem.Alt
                    : dampModelItem.GetProperty("altText");

                return mediaFile;
            }

            return null;
        }

        #endregion
    }
}
