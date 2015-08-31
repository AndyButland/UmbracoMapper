namespace Zone.UmbracoMapper.Tests
{
    using System;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Property)]
    public class SimpleMapFromAttribute : Attribute, IMapFromAttribute
    {
        public void SetPropertyValue<T>(object fromObject, PropertyInfo property, T model, IUmbracoMapper mapper)
        {
            property.SetValue(model, new SimpleViewModel {Id = 1001, Name = "Child item"});
        }
    }
}
