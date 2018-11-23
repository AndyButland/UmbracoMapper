namespace TestWebApp.Attributes
{
    using System;
    using System.Reflection;
    using Zone.UmbracoMapper;

    [AttributeUsage(AttributeTargets.Property)]
    public class MapPropertyValueToUpperCaseAttribute : Attribute, IMapFromAttribute
    {
        public void SetPropertyValue<T>(object fromObject, PropertyInfo property, T model, IUmbracoMapper mapper)
        {
            var rawValue = fromObject as string;
            property.SetValue(model, rawValue?.ToUpperInvariant());
        }
    }
}
