namespace Zone.UmbracoMapper.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using Umbraco.Core.Models;

    public class SimpleViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class SimpleViewModel1b
    {
        public int Id { get; set; }

        [PropertyMapping(Ignore = true)]
        public string Name { get; set; }
    }

    public class SimpleViewModel2 : SimpleViewModel
    {
        public string Author { get; set; }
    }

    public class SimpleViewModel2WithAttribute : SimpleViewModel
    {
        [PropertyMapping(SourceProperty = "CreatorName")]
        public string Author { get; set; }
    }

    public class SimpleViewModel2bWithAttribute : SimpleViewModel
    {
        public SimpleViewModel2bWithAttribute()
        {
            Parent = new SimpleViewModel();
        }

        [PropertyMapping(LevelsAbove = 1)]
        public SimpleViewModel Parent { get; set; }
    }

    public class SimpleViewModel3 : SimpleViewModel2
    {
        public string BodyText { get; set; }

        public string BodyText2 { get; set; }

        public IHtmlString BodyTextAsHtmlString { get; set; }

        public int NonMapped { get; set; }

        public int MappedFromZero { get; set; }

        public bool MappedFromFalse { get; set; }
    }

    public class SimpleViewModel3WithAttribute : SimpleViewModel2
    {
        public SimpleViewModel3WithAttribute()
        {
            SubModelValue = new SubModel();
            SubModelValues = new List<SubModel>();
        }

        [PropertyMapping(DefaultValue = "Default body text")]
        public string BodyText { get; set; }

        [PropertyMapping(DefaultValue = "Default body text 2")]
        public string BodyText2 { get; set; }

        [PropertyMapping(DefaultValue = 99)]
        public int NonMapped { get; set; }

        public SubModel SubModelValue { get; set; }

        public IList<SubModel> SubModelValues { get; set; }

        public class SubModel
        {
            public string SubHeading { get; set; }
        }
    }

    public class SimpleViewModel3bWithAttribute : SimpleViewModel2
    {
        [PropertyMapping(Ignore = true)]
        public string BodyText { get; set; }

        public int NonMapped { get; set; }
    }

    public class SimpleViewModel3cWithAttribute : SimpleViewModel2
    {
        [PropertyMapping(PropertyValueGetter = typeof(SuffixAddingPropertyValueGetter))]
        public string BodyText { get; set; }

        public int NonMapped { get; set; }
    }

    public class SimpleViewModel4 : SimpleViewModel2
    {
        public string BodyCopy { get; set; }
    }

    public class SimpleViewModel4WithAttribute : SimpleViewModel2WithAttribute
    {
        [PropertyMapping(SourceProperty = "bodyText")]
        public string BodyCopy { get; set; }
    }

    public class SimpleViewModel4bWithAttribute : SimpleViewModel2WithAttribute
    {
        [PropertyMapping(SourceProperty = "bodyText", MapRecursively = true)]
        public string BodyCopy { get; set; }

        public DateTime? DateTime { get; set; }
    }

    public class SimpleViewModel5 : SimpleViewModel2
    {
        public string HeadingAndBodyText { get; set; }

        public string SummaryText { get; set; }
    }

    public class SimpleViewModel5WithAttribute : SimpleViewModel2WithAttribute
    {
        [PropertyMapping(SourcePropertiesForConcatenation = new string[] { "Name", "bodyText" }, ConcatenationSeperator = ",")]
        public string HeadingAndBodyText { get; set; }

        [PropertyMapping(SourcePropertiesForCoalescing = new string[] { "summaryText", "bodyText" })]
        public string SummaryText { get; set; }
    }

    public class SimpleViewModel5bWithAttribute : SimpleViewModel2WithAttribute
    {
        [PropertyMapping(SourcePropertiesForConcatenation = new string[] { "Name", "bodyText" }, ConcatenationSeperator = ",")]
        public string HeadingAndBodyText { get; set; }

        [PropertyMapping(SourcePropertiesForCoalescing = new string[] { "emptyText", "bodyText" })]
        public string SummaryText { get; set; }
    }

    public class SimpleViewModel6 : SimpleViewModel
    {
        public byte Age { get; set; }

        public long FacebookId { get; set; }

        public decimal AverageScore { get; set; }

        public DateTime RegisteredOn { get; set; }

        public string NonMapped { get; set; }

        public bool IsMember { get; set; }

        public string TwitterUserName { get; set; }
    }

    public class SimpleViewModel7 : SimpleViewModel
    {
        public int ParentId { get; set; }
    }

    public class SimpleViewModel7WithAttribute : SimpleViewModel
    {
        [PropertyMapping(SourceProperty = "Id", LevelsAbove = 1)]
        public int ParentId { get; set; }
    }

    public class SimpleViewModel8 : SimpleViewModel
    {
        public GeoCoordinate GeoCoordinate { get; set; }
    }

    public class SimpleViewModel9 : SimpleViewModel
    {
        [SimpleMapFrom]
        public SimpleViewModel Child { get; set; }
    }

    public class SimpleViewModelWithCollection : SimpleViewModel
    {
        public SimpleViewModelWithCollection()
        {
            Comments = new List<Comment>();
        }

        public IList<Comment> Comments { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Text { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    public class GeoCoordinate
    {
        public decimal Longitude { get; set; }

        public decimal Latitude { get; set; }

        public int Zoom { get; set; }
    }

    public class SuffixAddingPropertyValueGetter : DefaultPropertyValueGetter
    {
        public override object GetPropertyValue(IPublishedContent content, string alias, bool recursive)
        {
            var value = base.GetPropertyValue(content, alias, recursive) as string ?? string.Empty;
            return value + "...";
        }
    }

    public class ComplexTypeReturningPropertyValueGetter : DefaultPropertyValueGetter
    {
        public override object GetPropertyValue(IPublishedContent content, string alias, bool recursive)
        {
            return new GeoCoordinate
                {
                    Latitude = 1.9M,
                    Longitude = 0.1M,
                    Zoom = 10
                };
        }
    }
}
