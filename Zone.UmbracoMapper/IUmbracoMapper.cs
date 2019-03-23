namespace Zone.UmbracoMapper.V7
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Umbraco.Core.Models;
    using Zone.UmbracoMapper.Common;

    public interface IUmbracoMapper
    {
        /// <summary>
        /// Gets or sets the root URL from where assets are served from in order to populate
        /// absolute URLs for media files (and support CDN delivery)
        /// </summary>
        string AssetsRootUrl { get; set; }

        /// <summary>
        /// Gets or sets a flag enabling caching.  On by default.
        /// </summary>
        bool EnableCaching { get; set; }

        /// <summary>
        /// Allows the mapper to use a custom mapping for a specified type from IPublishedContent
        /// </summary>
        /// <param name="propertyTypeFullName">Full name of the property type to map to</param>
        /// <param name="mapping">Mapping function</param>
        /// <param name="propertyName">Restricts this custom mapping to properties of this name</param>
        IUmbracoMapper AddCustomMapping(string propertyTypeFullName,
                                        CustomMapping mapping,
                                        string propertyName = null);

        /// <summary>
        /// Allows the mapper to use a custom mapping for a specified type from an object
        /// </summary>
        /// <param name="propertyTypeFullName">Full name of the property type to map to</param>
        /// <param name="mapping">Mapping function</param>
        /// <param name="propertyName">Restricts this custom mapping to properties of this name</param>
        IUmbracoMapper AddCustomMapping(string propertyTypeFullName,
                                        CustomObjectMapping mapping,
                                        string propertyName = null);

        /// <summary>
        /// Maps an instance of IPublishedContent to the passed view model based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="content">Instance of IPublishedContent</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <param name="propertySet">Set of properties to map</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        IUmbracoMapper Map<T>(IPublishedContent content,
                              T model,
                              Dictionary<string, PropertyMapping> propertyMappings = null,
                              string[] recursiveProperties = null,
                              PropertySet propertySet = PropertySet.All)
            where T : class;

        /// <summary>
        /// Maps content held in XML to the passed view model based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="xml">XML fragment to map from</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        IUmbracoMapper Map<T>(XElement xml,
                              T model,
                              Dictionary<string, PropertyMapping> propertyMappings = null)
            where T : class;

        /// <summary>
        /// Maps custom data held in a dictionary
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="dictionary">Dictionary of property name/value pairs</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        IUmbracoMapper Map<T>(Dictionary<string, object> dictionary,
                              T model,
                              Dictionary<string, PropertyMapping> propertyMappings = null)
            where T : class;

        /// <summary>
        /// Maps information held in a JSON string
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="json">JSON string</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        IUmbracoMapper Map<T>(string json,
                              T model,
                              Dictionary<string, PropertyMapping> propertyMappings = null)
            where T : class;

        /// <summary>
        /// Maps a collection of IPublishedContent to the passed view model
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="contentCollection">Collection of IPublishedContent</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <param name="propertySet">Set of properties to map</param>
        /// <param name="clearCollectionBeforeMapping">Flag indicating whether to clear the collection mapping too before carrying out the mapping</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection,
                                        IList<T> modelCollection,
                                        Dictionary<string, PropertyMapping> propertyMappings = null,
                                        string[] recursiveProperties = null,
                                        PropertySet propertySet = PropertySet.All, 
                                        bool clearCollectionBeforeMapping = true)
            where T : class, new();

        /// <summary>
        /// Maps a collection of content held in XML to the passed view model collection based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="xml">XML fragment to map from</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="groupElementName">Name of the element grouping each item in the XML (defaults to "Item")</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this XML element is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        IUmbracoMapper MapCollection<T>(XElement xml, IList<T> modelCollection,
                                        Dictionary<string, PropertyMapping> propertyMappings = null,
                                        string groupElementName = "Item",
                                        bool createItemsIfNotAlreadyInList = true,
                                        string sourceIdentifyingPropName = "Id",
                                        string destIdentifyingPropName = "Id")
            where T : class, new();

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
        IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries,
                                        IList<T> modelCollection,
                                        Dictionary<string, PropertyMapping> propertyMappings = null,
                                        bool createItemsIfNotAlreadyInList = true,
                                        string sourceIdentifyingPropName = "Id",
                                        string destIdentifyingPropName = "Id")
            where T : class, new();

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
        IUmbracoMapper MapCollection<T>(string json, IList<T> modelCollection,
                                        Dictionary<string, PropertyMapping> propertyMappings = null,
                                        string rootElementName = "items",
                                        bool createItemsIfNotAlreadyInList = true,
                                        string sourceIdentifyingPropName = "Id",
                                        string destIdentifyingPropName = "Id")
            where T : class, new();
    }
}
