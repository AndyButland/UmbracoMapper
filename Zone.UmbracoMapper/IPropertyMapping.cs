namespace Zone.UmbracoMapper
{
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
    }
}
