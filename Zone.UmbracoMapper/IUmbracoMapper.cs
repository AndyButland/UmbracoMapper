namespace Zone.UmbracoMapper
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Umbraco.Core.Models;

    public interface IUmbracoMapper
    {
        string AssetsRootUrl { get; set; }

        void AddCustomMapping(string propertyTypeFullName,
            Func<IUmbracoMapper, IPublishedContent, string, object> mapperFunction);

        IUmbracoMapper Map<T>(IPublishedContent content, 
            T model,
            Dictionary<string, PropertyMapping> propertyMappings = null,
            string[] recursiveProperties = null);

        IUmbracoMapper Map<T>(XElement xml, 
            T model,
            Dictionary<string, PropertyMapping> propertyMappings = null);

        IUmbracoMapper Map<T>(Dictionary<string, object> dictionary,
            T model,
            Dictionary<string, PropertyMapping> propertyMappings = null);

        IUmbracoMapper Map<T>(string json,
            T model,
            Dictionary<string, PropertyMapping> propertyMappings = null);

        IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection, 
            IList<T> modelCollection,
            Dictionary<string, PropertyMapping> propertyMappings = null,
            string[] recursiveProperties = null) where T : new();

        IUmbracoMapper MapCollection<T>(XElement xml, IList<T> modelCollection,
            Dictionary<string, PropertyMapping> propertyMappings = null, 
            string groupElementName = "Item", 
            bool createItemsIfNotAlreadyInList = true, 
            string sourceIdentifyingPropName = "Id", 
            string destIdentifyingPropName = "Id") where T : new();

        IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries, 
            IList<T> modelCollection,
            Dictionary<string, PropertyMapping> propertyMappings = null, 
            bool createItemsIfNotAlreadyInList = true, 
            string sourceIdentifyingPropName = "Id", 
            string destIdentifyingPropName = "Id") where T : new();

        IUmbracoMapper MapCollection<T>(string json, IList<T> modelCollection,
            Dictionary<string, PropertyMapping> propertyMappings = null,
            string rootElementName = "items", 
            bool createItemsIfNotAlreadyInList = true, 
            string sourceIdentifyingPropName = "Id", 
            string destIdentifyingPropName = "Id") where T : new();
    }
}
