﻿namespace Zone.UmbracoMapper.V8.Tests.Attributes
{
    using System;
    using System.Reflection;
    using Zone.UmbracoMapper.V8;
    using Zone.UmbracoMapper.V8.Attributes;

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
