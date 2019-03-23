namespace Zone.UmbracoMapper.V8.Attributes
{
    using System.Reflection;

    /// <summary>
    /// Attributes which implement this interface can be applied to model properties,
    /// declaring that the property should be mapped in a specific way
    /// </summary>
    public interface IMapFromAttribute
    {
        /// <summary>
        /// Defines how a property value should be set
        /// </summary>
        /// <typeparam name="T">Model type</typeparam>
        /// <param name="fromObject">Data from which to obtain the property value</param>
        /// <param name="property">Property which we're setting the value of</param>
        /// <param name="model">Model being populated</param>
        /// <param name="mapper">Umbraco Mapper instance</param>
        void SetPropertyValue<T>(object fromObject, PropertyInfo property, T model, IUmbracoMapper mapper);
    }
}
