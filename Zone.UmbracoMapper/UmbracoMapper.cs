namespace Zone.UmbracoMapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using Umbraco.Core.Models;
    using Umbraco.Web;
    using DampModel = DAMP.PropertyEditorValueConverter.Model;

    public class UmbracoMapper : IUmbracoMapper
    {
        /// <summary>
        /// Primary domain of the current Umbraco website, used for creating absolute paths to image files
        /// </summary>
        private readonly string _siteUrl;

        public UmbracoMapper(string siteUrl)
        {
            _siteUrl = siteUrl;
        }

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

                    case "String":
                    case "IHtmlString":
                        var value = content.GetPropertyValue(propName, IsRecursiveProperty(recursiveProperties, propName));
                        if (value != null)
                        {
                            property.SetValue(model, value);
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
                var propName = GetMappedPropertyName(property.Name, propertyNameMappings);

                // If element with mapped name found, map the value
                if (xml.Element(propName) != null)
                {
                    var stringValue = xml.Element(propName).Value;
                    SetTypedPropertyValue<T>(model, property, stringValue);
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
            if (modelCollection == null)
            {
                modelCollection = new List<T>();
            }

            foreach (var item in contentCollection)
            {
                var itemToCreate = new T();
                Map<T>(item, itemToCreate, propertyNameMappings, recursiveProperties);
                modelCollection.Add(itemToCreate);
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
        /// <param name="modelPropNameForMatchingExistingItems">When updating existing items in a collection, this property name is considered unique and used for look-ups to identifiy and update the correct item (defaults to "Id").</param>
        /// <param name="itemElementNameForMatchingExistingItems">When updating existing items in a collection, this XML element is considered unique and used for look-ups to identifiy and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(XElement xml,
            IList<T> modelCollection,
            Dictionary<string, string> propertyNameMappings = null,
            string groupElementName = "item",
            bool createItemsIfNotAlreadyInList = false,
            string modelPropNameForMatchingExistingItems = "Id",
            string itemElementNameForMatchingExistingItems = "Id") where T : new()
        {
            if (modelCollection == null)
            {
                modelCollection = new List<T>();
            }

            // Loop through each of the items defined in the XML
            foreach (var element in xml.Elements(groupElementName))
            {
                // Check if item is already in the list by looking up provided unique key
                var itemToUpdate = modelCollection
                    .Where(x => x.GetType()
                        .GetProperties()
                        .Single(p => p.Name == modelPropNameForMatchingExistingItems)
                        .GetValue(x).ToString().ToLowerInvariant() == element.Element(itemElementNameForMatchingExistingItems).Value.ToLowerInvariant())
                    .SingleOrDefault();
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
        /// <param name="modelPropNameForMatchingExistingItems">When updating existing items in a collection, this property name is considered unique and used for look-ups to identifiy and update the correct item (defaults to "Id").</param>
        /// <param name="itemElementNameForMatchingExistingItems">When updating existing items in a collection, this dictionary key is considered unique and used for look-ups to identifiy and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries,
            IList<T> modelCollection,
            Dictionary<string, string> propertyNameMappings = null,
            bool createItemsIfNotAlreadyInList = false,
            string modelPropNameForMatchingExistingItems = "Id",
            string itemElementNameForMatchingExistingItems = "Id") where T : new()
        {
            if (modelCollection == null)
            {
                modelCollection = new List<T>();
            }

            // Loop through each of the items defined in the XML
            foreach (var customDataItem in dictionaries)
            {
                // Check if item is already in the list by looking up provided unique key
                var itemToUpdate = modelCollection
                    .Where(x => x.GetType()
                        .GetProperties()
                        .Single(p => p.Name == modelPropNameForMatchingExistingItems)
                        .GetValue(x).ToString().ToLowerInvariant() == customDataItem[itemElementNameForMatchingExistingItems].ToString().ToLowerInvariant())
                    .SingleOrDefault();
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
        private string GetMappedPropertyName(string propName, Dictionary<string, string> propertyNameMappings, bool convertToCamelCase = false)
        {
            var mappedName = propName;
            if (propertyNameMappings != null && propertyNameMappings.ContainsKey(propName))
            {
                mappedName = propertyNameMappings[propName];
            }

            if (convertToCamelCase)
            {
                mappedName = char.ToLowerInvariant(mappedName[0]) + mappedName.Substring(1);
            }

            return mappedName;
        }

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
                mediaFile.DomainWithUrl = _siteUrl + dampModelItem.Url;
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
                case "Byte":
                    byte byteValue;
                    if (byte.TryParse(stringValue, out byteValue))
                    {
                        property.SetValue(model, byteValue);
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
                case "DateTime":
                    DateTime dateTimeValue;
                    if (DateTime.TryParse(stringValue, out dateTimeValue))
                    {
                        property.SetValue(model, dateTimeValue);
                    }

                    break;
                case "String":
                    property.SetValue(model, stringValue);
                    break;
            }
        }

        #endregion
    }
}
