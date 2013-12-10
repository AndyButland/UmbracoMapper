namespace Zone.UmbracoMapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

                    // TODO: further type mappings
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
                    property.SetValue(model, xml.Element(propName).Value);
                }
            }

            return this;
        }

        /// <summary>
        /// Maps custom data held in a dictionary
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="customData">Dictionary of property name/value pairs</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(Dictionary<string, object> customData, 
            T model,
            Dictionary<string, string> propertyNameMappings = null)
        {
            // TODO: implement
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
                var obj = new T();
                Map<T>(item, obj, propertyNameMappings, recursiveProperties);
                modelCollection.Add(obj);
            }

            return this;
        }

        /// <summary>
        /// Maps a collection custom data held in an Id linked dictionary to a collection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="customDataCollection">Collection of custom data containing a dictionary of property name/value pairs wrapped in a dictionary providing the Id for lookup on an existing collection</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyNameMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(Dictionary<int, Dictionary<string, object>> customDataCollection,
            IList<T> modelCollection,
            Dictionary<string, string> propertyNameMappings = null)
        {
            // TODO: implement
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

        #endregion
    }
}
