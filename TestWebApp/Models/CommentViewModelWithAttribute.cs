namespace TestWebApp.Models
{
    using System;

    using Zone.UmbracoMapper;
    using Zone.UmbracoMapper.V7.Attributes;
    using Zone.UmbracoMapper.V7.BaseDestinationTypes;

    public class CommentViewModelWithAttribute
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public DateTime CreatedOn { get; set; }

        public string Author { get; set; }

        [PropertyMapping(SourceProperty = "Name", LevelsAbove = 1)]
        public string ParentPage { get; set; }

        [PropertyMapping(SourceRelatedProperty = "Name")]
        public string Country { get; set; }

        [PropertyMapping(MapRecursively = true)]
        public MediaFile MainImage { get; set; }

        public string Heading { get; set; }

        [PropertyMapping(MapRecursively = true)]
        public int StarRating { get; set; }
    }
}