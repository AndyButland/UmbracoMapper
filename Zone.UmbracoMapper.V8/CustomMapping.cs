namespace Zone.UmbracoMapper.V8
{
    using Umbraco.Core.Models.PublishedContent;

    /// <summary>
    /// Provides a custom mapping of a single property of an IPublishedContent to an object
    /// </summary>
    /// <param name="mapper">The instance of IUmbracoMapper performing the mapping</param>
    /// <param name="content">Instance of IPublishedContent</param>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="fallback">Fallback method(s) to use when content not found</param>
    /// <returns>Instance of object containing mapped data</returns>
    public delegate object CustomMapping(IUmbracoMapper mapper, IPublishedContent content, string propertyName, Fallback fallback);

    /// <summary>
    /// Provides a custom mapping from a dictionary object to an object
    /// </summary>
    /// <param name="mapper">The instance of IUmbracoMapper performing the mapping</param>
    /// <param name="value">Instance of the object to map from</param>
    /// <returns>Instance of object containing mapped data</returns>
    public delegate object CustomObjectMapping(IUmbracoMapper mapper, object value);
}
