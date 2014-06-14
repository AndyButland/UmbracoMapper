using System;
using System.Collections.Generic;
namespace Zone.UmbracoMapper
{
    /// <summary>
    /// Class defining the override to the mapping convention for a particular type.
    /// Will be passed in a Dictionary indicating the destination property on the view model, i.e.: 
    ///     Dictionary<string, PropertyMapping>
    /// </summary>
    public class PropertyMapping
    {
        /// <summary>
        /// The name of the property on the source to map from.  If not passed, exact name match convention is used.
        /// </summary>
        public string SourceProperty { get; set; }

        /// <summary>
        /// Defines the number of levels above the current content to map the value from.  If not passed, 0 (the current
        /// level) is assumed.
        /// Only for IPublishedContent mappings.
        /// </summary>
        public int LevelsAbove { get; set; }

        /// <summary>
        /// If passed, the source property is assumed to be a structure that has related content (e.g. a Content Picker that
        /// contains an integer Id for another IPublishedContent).  The mapping is then done from the named property of 
        /// that child element.
        /// Only for IPublishedContent mappings.
        /// </summary>
        public string SourceRelatedProperty { get; set; }

        /// <summary>
        /// If passed, the source property is assumed to be a structure that has child content.  The mapping is then done 
        /// from the named field of that child element.
        /// Only for XML and JSON mappings.
        /// </summary>
        public string SourceChildProperty { get; set; }

        /// <summary>
        /// The names of the properties on the source to map from and concatenate.
        /// If not passed, exact name match convention or single match on SourceProperty is used.
        /// Only for IPublishedContent mappings.
        /// </summary>
        public string[] SourcePropertiesForConcatenation { get; set; }

        /// <summary>
        /// When SourcePropertiesForConcatenation is used, the separator string used to concatenate the items.
        /// If not passed, no separator is assumed.
        /// Only for IPublishedContent mappings.
        /// </summary>
        public string ConcatenationSeperator { get; set; }

        /// <summary>
        /// The names of the properties on the source to map from and coalesce (take the take the first non null, empty or whitespace property)
        /// If not passed, exact name match convention or single match on SourceProperty is used.
        /// Only for IPublishedContent mappings.
        /// </summary>
        public string[] SourcePropertiesForCoalescing { get; set; }

        /// <summary>
        /// If provided, mapping is only carried out if the property provided in the key contains the value provided in the value.
        /// </summary>
        public KeyValuePair<string, string> MapIfPropertyMatches { get; set; }

        /// <summary>
        /// If provided, carries out the transformation provided in the function on the mapped value
        /// </summary>
        public Func<string, string> StringValueFormatter { get; set; }
    }
}
