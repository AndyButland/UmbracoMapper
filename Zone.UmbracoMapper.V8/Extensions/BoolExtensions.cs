namespace Zone.UmbracoMapper.V8.Extensions
{
    using Umbraco.Core.Models.PublishedContent;

    public static class BoolExtensions
    {
        public static Fallback ToRecuriveFallback(this bool recursive)
        {
            return recursive ? Fallback.ToAncestors : Fallback.To(Fallback.None);
        }
    }
}
