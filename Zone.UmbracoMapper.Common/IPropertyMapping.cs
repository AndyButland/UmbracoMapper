namespace Zone.UmbracoMapper.Common
{
    using System;
    using System.Collections.Generic;

    public interface IPropertyMapping
    {
        /// <summary>
        /// The name of the property on the source to map from.  If not passed, exact name match convention is used.
        /// </summary>
        string SourceProperty { get; set; }

        /// <summary>
        /// Defines the number of levels above the current content to map the value from.  If not passed, 0 (the current
        /// level) is assumed.
        /// Only for IPublishedContent mappings.
        /// </summary>
        int LevelsAbove { get; set; }

        /// <summary>
        /// If passed, the source property is assumed to be a structure that has related content (e.g. a Content Picker that
        /// contains an integer Id for another IPublishedContent).  The mapping is then done from the named property of 
        /// that child element.
        /// Only for IPublishedContent mappings.
        /// </summary>
        string SourceRelatedProperty { get; set; }

        /// <summary>
        /// If passed, the source property is assumed to be a structure that has child content.  The mapping is then done 
        /// from the named field of that child element.
        /// Only for XML and JSON mappings.
        /// </summary>
        string SourceChildProperty { get; set; }

        /// <summary>
        /// The names of the properties on the source to map from and concatenate.
        /// If not passed, exact name match convention or single match on SourceProperty is used.
        /// Only for IPublishedContent mappings.
        /// </summary>
        string[] SourcePropertiesForConcatenation { get; set; }

        /// <summary>
        /// When SourcePropertiesForConcatenation is used, the separator string used to concatenate the items.
        /// If not passed, no separator is assumed.
        /// Only for IPublishedContent mappings.
        /// </summary>
        string ConcatenationSeperator { get; set; }

        /// <summary>
        /// The names of the properties on the source to map from and coalesce (take the take the first non null, empty or whitespace property)
        /// If not passed, exact name match convention or single match on SourceProperty is used.
        /// Only for IPublishedContent mappings.
        /// </summary>
        string[] SourcePropertiesForCoalescing { get; set; }

        /// <summary>
        /// If assigned to a property it will be mapped recursively.
        /// It will use Umbraco default camel-case naming convention (i.e. if assigned to a view model property called 
        /// 'StarRating', it'll look for an Umbraco property called 'starRating')
        /// </summary>
        bool MapRecursively { get; set; }

        /// <summary>
        /// Accepts an array of integers defining methods of fall-back when content is not found.
        /// See Umbraco 8's Umbraco.Core.Models.PublishedContent.Fallback class.
        /// If mapping recursively it will use Umbraco default camel-case naming convention (i.e. if assigned to a view model property called 
        /// 'StarRating', it'll look for an Umbraco property called 'starRating')
        /// </summary>
        /// <remarks>
        /// If provided, this will take precedence over a value provided in MapRecursively.
        /// </remarks>
        int[] FallbackMethods { get; set; }

        /// <summary>
        /// Sets a default value for a property to be used if the mapped value cannot be found.
        /// </summary>
        object DefaultValue { get; set; }

        /// <summary>
        /// If set property will not be mapped even if an appropriate mapping could be found
        /// </summary>
        bool Ignore { get; set; }

        /// <summary>
        /// If set property will be mapped from the given Umbraco dictionary key
        /// </summary>
        string DictionaryKey { get; set; }

        /// <summary>
        /// Provides a type that must implement IPropertyValueGetter to be used when retrieving the property value from Umbraco.
        /// A use case for this is to use Vorto, where we want to call GetVortoValue instead of GetPropertyValue.
        /// </summary>
        Type PropertyValueGetter { get; set; }

        /// <summary>
        /// Provides a type that must implement be of type CustomMapping (defined in version specific assemblies) to be used
        /// in preference to any named or unnamed custom mapping that might be registered globally. 
        /// </summary>
        Type CustomMappingType { get; set; }

        /// <summary>
        /// Provides the name of a method of type CustomMapping (defined in project specific versions) that will be used in preference to any named or unnamed
        /// custom mapping that might be registered globally. 
        /// </summary>
        string CustomMappingMethod { get; set; }
    }
}
