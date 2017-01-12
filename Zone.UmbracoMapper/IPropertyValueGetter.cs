namespace Zone.UmbracoMapper
{
    using Umbraco.Core.Models;

    public interface IPropertyValueGetter
    {
        object GetPropertyValue(IPublishedContent content, string alias, bool recursive);
    }
}
