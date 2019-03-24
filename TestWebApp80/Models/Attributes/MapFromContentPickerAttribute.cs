namespace TestWebApp80.Models.Attributes
{
    using System;
    using System.Reflection;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web.Composing;
    using Zone.UmbracoMapper.V8;
    using Zone.UmbracoMapper.V8.Attributes;

    [AttributeUsage(AttributeTargets.Property)]
    public class MapFromContentPickerAttribute : Attribute, IMapFromAttribute
    {
        public void SetPropertyValue<T>(object fromObject, PropertyInfo property, T model, IUmbracoMapper mapper)
        {
            var method = GetType().GetMethod("GetInstance", BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = method.MakeGenericMethod(property.PropertyType);
            var item = genericMethod.Invoke(this, new[] { fromObject, mapper });
            property.SetValue(model, item);
        }

        private T GetInstance<T>(object fromObject, IUmbracoMapper mapper)
            where T : class
        {
            T instance = default(T);
            if (fromObject != null)
            {
                // Check first if already IPublishedContent (as core converters installed)
                var content = fromObject as IPublishedContent;
                if (content == null)
                {
                    // Otherwise handle if Id passed
                    int id;
                    if (int.TryParse(fromObject.ToString(), out id))
                    {
                        content = Current.UmbracoHelper.Content(id);
                    }
                }

                if (content != null)
                {
                    instance = Activator.CreateInstance<T>();
                    mapper.Map(content, instance);
                }

            }

            return instance;
        }
    }
}