namespace Zone.UmbracoMapper.Attributes
{
    using System;

    public class PropertyMappingAttribute : Attribute, IPropertyMapping
    {
        public string SourceProperty { get; set; }

        public int LevelsAbove { get; set; }

        public string[] SourcePropertiesForConcatenation { get; set; }

        public string ConcatenationSeperator { get; set; }

        public string[] SourcePropertiesForCoalescing { get; set; }
    }
}
