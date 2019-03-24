namespace Zone.UmbracoMapper.V8
{
    using Umbraco.Core.Models.PublishedContent;

    public interface IPropertyValueGetter
    {
        object GetPropertyValue(IPublishedContent content, string alias, string culture, string segment, Fallback fallback);
    }
}
