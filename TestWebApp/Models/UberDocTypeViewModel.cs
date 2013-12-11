namespace TestWebApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using Zone.UmbracoMapper;
    
    public class UberDocTypeViewModel
    {
        public UberDocTypeViewModel()
        {
            Comments = new List<CommentViewModel>();
            Countries = new List<CountryViewModel>();
            RelatedLinks = new List<LinkViewModel>();
        }

        public int Id { get; set; }

        public string Heading { get; set; }

        public DateTime CreatedOn { get; set; }

        public IHtmlString BodyText { get; set; }
        
        public int StarRating { get; set; }

        public bool IsApproved { get; set; }

        public decimal AverageScore { get; set; }

        public MediaFile MainImage { get; set; }

        public IList<CommentViewModel> Comments  { get; set; }

        public IList<CountryViewModel> Countries { get; set; }

        public IList<LinkViewModel> RelatedLinks { get; set; }
    }
}