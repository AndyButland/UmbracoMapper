namespace TestWebApp.Controllers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Web.Mvc;
    using System.Xml.Linq;
    using TestWebApp.Models;
    using Umbraco.Core.Models;
    using Umbraco.Web;
    using Zone.UmbracoMapper;
    
    public class UberDocTypeController : BaseController
    {
        public UberDocTypeController(IUmbracoMapper mapper)
            : base(mapper)
        {
        }

        public ActionResult UberDocType()
        {
            // Get related content
            var countryNodes = GetRelatedNodes();
            var relatedLinksXml = GetRelatedLinks();

            // Create view model and run mapping
            var model = new UberDocTypeViewModel();
            Mapper.Map(CurrentPage, model, new Dictionary<string, PropertyMapping> 
                    { 
                        { 
                            "CreatedOn", new PropertyMapping 
                                { 
                                    SourceProperty = "CreateDate" 
                                } 
                        }, 
                        { 
                            "SelectedComment", new PropertyMapping 
                                { 
                                    SourceRelatedProperty = "text" 
                                } 
                        }, 
                        { 
                            "SelectedCommentId", new PropertyMapping 
                                { 
                                    SourceProperty = "selectedComment",
                                    SourceRelatedProperty = "Id" 
                                } 
                        }, 
                        { 
                            "ConcatenatedValue", new PropertyMapping 
                                { 
                                    SourcePropertiesForConcatenation = new string[] { "heading", "starRating", "Id", "Url" },
                                    ConcatenationSeperator = ", ",
                                } 
                        }, 
                    })
                .MapCollection(CurrentPage.Children, model.Comments,
                    new Dictionary<string, PropertyMapping>
                        { 
                            { 
                                "ParentPage", new PropertyMapping 
                                    { 
                                        SourceProperty = "Name", 
                                        LevelsAbove = 1 
                                    }
                            },
                            {
                                "Country", new PropertyMapping 
                                    { 
                                        SourceRelatedProperty = "Name", 
                                    } 
                            }, 
                        },
                    new string[] { "mainImage" })
                .MapCollection(countryNodes, model.Countries)
                .MapCollection(relatedLinksXml, model.RelatedLinks, null, "link")
                .Map(GetSingleXml(), model, new Dictionary<string, PropertyMapping> { { "SingleValueFromXml", new PropertyMapping { SourceProperty = "Day" } }, })
                .MapCollection(GetCollectionXml(), model.CollectionFromXml, null, "Month")
                .Map(GetSingleDictionary(), model, new Dictionary<string, PropertyMapping> { { "SingleValueFromDictionary", new PropertyMapping { SourceProperty = "Animal" } }, })
                .MapCollection(GetCollectionDictionary(), model.CollectionFromDictionary)
                .Map(GetSingleJson(), model, new Dictionary<string, PropertyMapping> { { "SingleValueFromJson", new PropertyMapping { SourceProperty = "Name" } }, })
                .MapCollection(GetCollectionJson(), model.CollectionFromJson)
                .Map(CurrentPage, model.SubModel);

            return CurrentTemplate(model);
        }

        private IEnumerable<IPublishedContent> GetRelatedNodes()
        {
            IEnumerable<IPublishedContent> countryNodes = null;
            var countryIds = CurrentPage.GetPropertyValue<string>("countries");
            if (!string.IsNullOrEmpty(countryIds))
            {
                countryNodes = Umbraco.TypedContent(countryIds.Split(','));
            }

            return countryNodes;
        }

        private XElement GetRelatedLinks()
        {
            var sr = new StringReader(CurrentPage.GetPropertyValue<string>("relatedLinks"));
            return XElement.Load(sr);
        }

        private XElement GetSingleXml()
        {
            return new XElement("Date",
                new XElement("Day", "Sunday"));
        }

        private XElement GetCollectionXml()
        {
            return new XElement("Months",
                new XElement("Month",
                    new XElement("Name", "January")),
                new XElement("Month",
                    new XElement("Name", "February")));
        }

        private Dictionary<string, object> GetSingleDictionary()
        {
            return new Dictionary<string, object> 
            { 
                { "Animal", "Iguana" },
            };
        }

        private List<Dictionary<string, object>> GetCollectionDictionary()
        {
            return new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object>
                {
                    { "Name", "Shark" },
                },
                new Dictionary<string, object>
                {
                    { "Name", "Whale" },
                },
                new Dictionary<string, object>
                {
                    { "Name", "Dophin" },
                },
            };
        }

        private string GetSingleJson()
        {
            return @"{
                    'Name': 'Eric Cantona',
                }";
        }

        private string GetCollectionJson()
        {
            return @"{ 'items': [{
                    'Name': 'David Gower',
                },
                {
                    'Name': 'Geoffrey Boycott',
                }]}";
        }
    }
}
