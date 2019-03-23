﻿namespace Zone.UmbracoMapper.V7
{
    using Umbraco.Core.Models;

    /// <summary>
    /// Provides a custom mapping of a single property of an IPublishedContent to an object
    /// </summary>
    /// <param name="mapper">The instance of IUmbracoMapper performing the mapping</param>
    /// <param name="content">Instance of IPublishedContent</param>
    /// <param name="propertyName">Name of the property to map</param>
    /// <param name="recursive">Whether the property should be treated as recursive for mapping</param>
    /// <returns>Instance of object containing mapped data</returns>
    public delegate object CustomMapping(IUmbracoMapper mapper, IPublishedContent content, string propertyName, bool recursive);

    /// <summary>
    /// Provides a custom mapping from a dictionary object to an object
    /// </summary>
    /// <param name="mapper">The instance of IUmbracoMapper performing the mapping</param>
    /// <param name="value">Instance of the object to map from</param>
    /// <returns>Instance of object containing mapped data</returns>
    public delegate object CustomObjectMapping(IUmbracoMapper mapper, object value);
}
