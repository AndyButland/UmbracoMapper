namespace Zone.UmbracoMapper.V8
{
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;

    public class DefaultPropertyValueGetter : IPropertyValueGetter
    {
        public virtual object GetPropertyValue(IPublishedContent content, string alias, string culture, string segment, Fallback fallback)
        {
            return content.Value(alias, culture, segment, fallback);
        }
    }
}
