namespace Zone.UmbracoMapper
{
    using Umbraco.Core.Models;
    using Umbraco.Web;

    public class DefaultPropertyValueGetter : IPropertyValueGetter
    {
        public virtual object GetPropertyValue(IPublishedContent content, string alias, bool recursive)
        {
            return content.GetPropertyValue(alias, recursive);
        }
    }
}
