﻿namespace Zone.UmbracoMapper.Helpers
{
    using System;
    using System.Linq;
    using System.Web;

    internal static class TypeExtensions
    {
        /// <summary>
        /// Determine whether a type is simple (String, Decimal, DateTime, etc) 
        /// or complex (i.e. custom class with public properties and methods).
        /// </summary>
        /// <see cref="http://stackoverflow.com/questions/2442534/how-to-test-if-type-is-primitive"/>
        internal static bool IsSimpleType(
            this Type type)
        {
            return
                type.IsValueType ||
                type.IsPrimitive ||
                new[] 
                    {
                        typeof(string),
                        typeof(IHtmlString),
                        typeof(decimal),
                        typeof(DateTime),
                        typeof(DateTimeOffset),
                        typeof(TimeSpan),
                        typeof(Guid)
                    }.Contains(type) ||
                Convert.GetTypeCode(type) != TypeCode.Object;
        }
    }
}
