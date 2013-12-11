namespace Zone.UmbracoMapper
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Umbraco.Core.Models;

    public interface IUmbracoMapper
    {
        IUmbracoMapper Map<T>(IPublishedContent content, 
            T model, 
            Dictionary<string, string> propertyNameMappings = null,
            string[] recursiveProperties = null);

        IUmbracoMapper Map<T>(XElement xml, 
            T model,
            Dictionary<string, string> propertyNameMappings = null);

        IUmbracoMapper Map<T>(Dictionary<string, object> dictionary,
            T model,
            Dictionary<string, string> propertyNameMappings = null);

        IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection, 
            IList<T> modelCollection,
            Dictionary<string, string> propertyNameMappings = null,
            string[] recursiveProperties = null) where T : new();

        IUmbracoMapper MapCollection<T>(XElement xml,
            IList<T> modelCollection,
            Dictionary<string, string> propertyNameMappings = null,
            string groupElementName = "Item", 
            bool createItemsIfNotAlreadyInList = false,
            string modelPropNameForMatchingExistingItems = "Id",
            string itemElementNameForMatchingExistingItems = "Id") where T : new();

        IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries, 
            IList<T> modelCollection,
            Dictionary<string, string> propertyNameMappings = null,
            bool createItemsIfNotAlreadyInList = false,
            string modelPropNameForMatchingExistingItems = "Id",
            string itemElementNameForMatchingExistingItems = "Id") where T : new();
    }
}
