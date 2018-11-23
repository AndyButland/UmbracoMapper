namespace Zone.UmbracoMapper.Tests.Attributes
{
    using System;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Property)]
    public class SimpleMapPropertyValueAttribute : Attribute, IMapFromAttribute
    {
        public void SetPropertyValue<T>(object fromObject, PropertyInfo property, T model, IUmbracoMapper mapper)
        {
            var rawValue = fromObject as string;
            property.SetValue(model, rawValue);
        }
    }
}
