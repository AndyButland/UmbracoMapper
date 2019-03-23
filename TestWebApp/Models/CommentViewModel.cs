namespace TestWebApp.Models
{
    using System;

    using Zone.UmbracoMapper;
    using Zone.UmbracoMapper.V7.BaseDestinationTypes;

    public class CommentViewModel
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public DateTime CreatedOn { get; set; }

        public string Author { get; set; }

        public string ParentPage { get; set; }

        public string Country { get; set; }

        public MediaFile MainImage { get; set; }

        public string Heading { get; set; }

        public int StarRating { get; set; }
    }
}