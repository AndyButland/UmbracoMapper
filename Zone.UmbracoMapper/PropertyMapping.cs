namespace Zone.UmbracoMapper.V7
{
    using Zone.UmbracoMapper.Common;

    /// <summary>
    /// Class defining the override to the mapping convention for a particular type.
    /// Will be passed in a Dictionary indicating the destination property on the view model, i.e.: Dictionary<string, PropertyMapping>
    /// </summary>
    public class PropertyMapping : PropertyMappingBase
    {
        /// <summary>
        /// Provides an instance of a <see cref="CustomMapping"/> that will be used in preference to any named or unnamed
        /// custom mapping that might be registered globally. 
        /// </summary>
        public CustomMapping CustomMapping { get; set; }
    }
}
