namespace TestWebApp80.Models
{
    using System;
    using Zone.UmbracoMapper.Common.Attributes;
    using Zone.UmbracoMapper.Common.BaseDestinationTypes;

    public class CommentViewModelWithAttribute
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public DateTime CreateDate { get; set; }

        public string Author { get; set; }

        [PropertyMapping(SourceProperty = "Name", LevelsAbove = 1)]
        public string ParentPage { get; set; }

        [PropertyMapping(SourceRelatedProperty = "Name")]
        public string Country { get; set; }

        [PropertyMapping(MapRecursively = true)]
        public MediaFile MediaPickedImage { get; set; }

        public string Heading { get; set; }

        [PropertyMapping(MapRecursively = true)]
        public int StarRating { get; set; }
    }
}