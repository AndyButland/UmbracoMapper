namespace Zone.UmbracoMapper.Tests.Attributes
{
    using System;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Property)]
    public class SimpleMapFromForSimpleViewModelAttribute : Attribute, IMapFromAttribute
    {
        public void SetPropertyValue<T>(object fromObject, PropertyInfo property, T model, IUmbracoMapper mapper)
        {
            property.SetValue(model, new SimpleViewModel { Id = 1001, Name = "Child item" });
        }
    }
}
