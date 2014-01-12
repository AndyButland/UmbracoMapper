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
            CollectionFromXml = new List<NamedItemViewModel>();
            CollectionFromDictionary = new List<NamedItemViewModel>();
            CollectionFromJson = new List<NamedItemViewModel>();
            SubModel = new SubModel();
        }

        public int Id { get; set; }

        public string Heading { get; set; }

        public DateTime CreatedOn { get; set; }

        public IHtmlString BodyText { get; set; }
        
        public int StarRating { get; set; }

        public bool IsApproved { get; set; }

        public decimal AverageScore { get; set; }

        public MediaFile MainImage { get; set; }

        public string SingleValueFromXml { get; set; }

        public string SingleValueFromDictionary { get; set; }

        public string SingleValueFromJson { get; set; }

        public IList<CommentViewModel> Comments  { get; set; }

        public IList<CountryViewModel> Countries { get; set; }

        public IList<LinkViewModel> RelatedLinks { get; set; }

        public IList<NamedItemViewModel> CollectionFromXml { get; set; }

        public IList<NamedItemViewModel> CollectionFromDictionary { get; set; }

        public IList<NamedItemViewModel> CollectionFromJson { get; set; }

        public SubModel SubModel { get; set; }
    }

    public class SubModel
    {
        public int Id { get; set; }

        public string Heading { get; set; }
    }
}
