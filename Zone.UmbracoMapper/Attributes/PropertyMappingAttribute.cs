namespace Zone.UmbracoMapper.Attributes
{
    using System;

    public class PropertyMappingAttribute : Attribute, IPropertyMapping
    {
        public string SourceProperty { get; set; }

        public int LevelsAbove { get; set; }
    }
}
