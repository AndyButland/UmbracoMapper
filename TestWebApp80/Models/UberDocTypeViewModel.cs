namespace TestWebApp80.Models
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using Attributes;
    using Zone.UmbracoMapper.Common.BaseDestinationTypes;

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
            AutoMapSingle = new CountryViewModel();
            AutoMapMultiple = new List<CountryViewModel>();
            WelcomeTextEnglish = new CultureSpecificModel();
            WelcomeTextItalian = new CultureSpecificModel();
        }

        public int Id { get; set; }

        public string Heading { get; set; }

        public string UpperCaseHeading { get; set; }

        public string HeadingWithDefaultValue { get; set; }

        public DateTime CreatedOn { get; set; }

        public string FormattedCreatedOnDate { get; set; }

        public IHtmlString BodyText { get; set; }
        
        public int StarRating { get; set; }

        public bool IsApproved { get; set; }

        public decimal AverageScore { get; set; }

        public MediaFile MainImage { get; set; }
        
        public MediaFile MediaPickedImage { get; set; }

        public IEnumerable<MediaFile> MultipleMediaPickedImages { get; set; }
        
        public string SingleValueFromXml { get; set; }

        public string SingleValueFromDictionary { get; set; }

        public string SingleValueFromJson { get; set; }

        public IList<CommentViewModel> Comments { get; set; }

        public IList<CountryViewModel> Countries { get; set; }

        public IList<LinkViewModel> RelatedLinks { get; set; }

        public IList<NamedItemViewModel> CollectionFromXml { get; set; }

        public IList<NamedItemViewModel> CollectionFromDictionary { get; set; }

        public IList<NamedItemViewModel> CollectionFromJson { get; set; }

        public SubModel SubModel { get; set; }

        [MapFromContentPicker]
        public CommentModel SelectedCommentModel { get; set; }

        public string SelectedComment { get; set; }

        public int SelectedCommentId { get; set; }

        public string ConcatenatedValue { get; set; }

        public string ConditionalValueMet { get; set; }

        public string ConditionalValueNotMet { get; set; }

        public string CoalescedValue { get; set; }

        public string NonMapped { get; set; }

        public string NonMappedFromEmptyString { get; set; }

        public string DocumentTypeAlias { get; set; }

        public DateTime? Date1 { get; set; }

        public DateTime? Date2 { get; set; }

        public CountryViewModel AutoMapSingle { get; set; }

        public IEnumerable<CountryViewModel> AutoMapMultiple { get; set; }

        public string DictionaryValue { get; set; }

        public CultureSpecificModel WelcomeTextEnglish { get; set; }

        public CultureSpecificModel WelcomeTextItalian { get; set; }
    }

    public class SubModel
    {
        public int Id { get; set; }

        public string Heading { get; set; }
    }

    public class CommentModel
    {
        public int Id { get; set; }

        public string Text { get; set; }
    }

    public class CultureSpecificModel
    {
        public string WelcomeText { get; set; }

        public string HelloText { get; set; }
    }
}
