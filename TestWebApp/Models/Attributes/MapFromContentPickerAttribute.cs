
namespace TestWebApp.Models.Attributes
{
    using System;
    using System.Reflection;
    using Umbraco.Core.Models;
    using Umbraco.Web;
    using Zone.UmbracoMapper;
    using Zone.UmbracoMapper.V7;
    using Zone.UmbracoMapper.V7.Attributes;

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
                        var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
                        content = umbracoHelper.TypedContent(id);
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