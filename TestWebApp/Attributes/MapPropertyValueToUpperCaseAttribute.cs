namespace TestWebApp.Attributes
{
    using System;
    using System.Reflection;
    using Zone.UmbracoMapper;
    using Zone.UmbracoMapper.V7;
    using Zone.UmbracoMapper.V7.Attributes;

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
