namespace Zone.UmbracoMapper.Tests.Attributes
{
    using System;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Property)]
    public class SimpleMapFromForContatenateStringAttribute : Attribute, IMapFromAttribute
    {
        private static int _callCount;

        public bool SetToEmptyOnFirstResult { get; set; }

        public void SetPropertyValue<T>(object fromObject, PropertyInfo property, T model, IUmbracoMapper mapper)
        {
            _callCount++;

            var value = _callCount == 1 && SetToEmptyOnFirstResult
                ? string.Empty
                : $"Test {_callCount}";
            property.SetValue(model, value);
        }

        internal static void ResetCallCount()
        {
            _callCount = 0;
        }
    }
}
