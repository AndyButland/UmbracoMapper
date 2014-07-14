namespace Zone.UmbracoMapper
{
    using System;
    using System.Collections.Generic;

    public class PropertyMappingAttribute : Attribute, IPropertyMapping
    {
        public string SourceProperty { get; set; }

        public int LevelsAbove { get; set; }

        public string SourceRelatedProperty { get; set; }

        public string SourceChildProperty { get; set; }

        public string[] SourcePropertiesForConcatenation { get; set; }

        public string ConcatenationSeperator { get; set; }

        public string[] SourcePropertiesForCoalescing { get; set; }
    }
}
