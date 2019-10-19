namespace Zone.UmbracoMapper.V8
{
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;

    public class DefaultPropertyValueGetter : IPropertyValueGetter
    {
        public virtual object GetPropertyValue(IPublishedElement content, string alias, string culture, string segment, Fallback fallback)
        {
            // We need to cast to IPublishedContent if that's what we are mapping from, such that the fallback methods are
            // handled correctly.
            var publishedContent = content as IPublishedContent;
            var cultureOrNull = string.IsNullOrEmpty(culture) ? null : culture;
            return publishedContent != null
                ? publishedContent.Value(alias, cultureOrNull, segment, fallback)
                : content.Value(alias, cultureOrNull, segment, fallback);
        }
    }
}
