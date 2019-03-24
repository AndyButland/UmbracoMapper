namespace TestWebApp80.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Web.Mvc;
    using System.Xml.Linq;
    using TestWebApp80.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Zone.UmbracoMapper.V8;

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
                            "SelectedCommentModel", new PropertyMapping 
                                { 
                                    SourceProperty = "selectedComment",
                                } 
                        }, 
                        { 
                            "ConcatenatedValue", new PropertyMapping 
                                { 
                                    SourcePropertiesForConcatenation = new string[] { "heading", "starRating", "Id", "Url" },
                                    ConcatenationSeperator = ", ",
                                } 
                        }, 
                        { 
                            "ConditionalValueMet", new PropertyMapping 
                                { 
                                    SourceProperty = "heading",
                                    MapIfPropertyMatches = new KeyValuePair<string, string>("isApproved", "true"),
                                } 
                        }, 
                        { 
                            "ConditionalValueNotMet", new PropertyMapping 
                                { 
                                    SourceProperty = "heading",
                                    MapIfPropertyMatches = new KeyValuePair<string, string>("isApproved", "0"),
                                } 
                        },
                        { 
                            "CoalescedValue", new PropertyMapping 
                                { 
                                    SourcePropertiesForCoalescing = new string[] { "emptyField", "Name" },
                                } 
                        }, 
                        { 
                            "UpperCaseHeading", new PropertyMapping 
                                { 
                                    SourceProperty = "heading",
                                    StringValueFormatter = x => 
                                    {
                                        return x.ToUpper();
                                    }
                                } 
                        }, 
                        { 
                            "FormattedCreatedOnDate", new PropertyMapping 
                                { 
                                    SourceProperty = "CreateDate",
                                    StringValueFormatter = x => DateTime.Parse(x).ToString("dd MMMM, yyyy")
                                } 
                        }, 
                        { 
                            "NonMapped", new PropertyMapping 
                                { 
                                    DefaultValue = "Default text",
                                } 
                        }, 
                        { 
                            "NonMappedFromEmptyString", new PropertyMapping 
                                { 
                                    SourceProperty = "emptyField",
                                    DefaultValue = "Default text",
                                } 
                        }, 
                        { 
                            "HeadingWithDefaultValue", new PropertyMapping 
                                { 
                                    SourceProperty = "Heading",
                                    DefaultValue = "Default text",
                                } 
                        }, 
                        { 
                            "DocumentTypeAlias", new PropertyMapping 
                                { 
                                    Ignore = true,
                                } 
                        }, 
                        { 
                            "DictionaryValue", new PropertyMapping 
                                { 
                                    DictionaryKey = "testKey" 
                                } 
                        }, 
                    })
                .MapCollection(CurrentPage.Children.Where(x => x.IsDocumentType("Comment")), model.Comments,
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
                    new string[] { "mediaPickedImage", "starRating" })
                .MapCollection(countryNodes, model.Countries)
                .Map(GetSingleXml(), model, new Dictionary<string, PropertyMapping> { { "SingleValueFromXml", new PropertyMapping { SourceProperty = "Day" } }, })
                .MapCollection(GetCollectionXml(), model.CollectionFromXml, null, "Month")
                .Map(GetSingleDictionary(), model, new Dictionary<string, PropertyMapping> { { "SingleValueFromDictionary", new PropertyMapping { SourceProperty = "Animal" } }, })
                .MapCollection(GetCollectionDictionary(), model.CollectionFromDictionary)
                .Map(GetSingleJson(), model, new Dictionary<string, PropertyMapping> { { "SingleValueFromJson", new PropertyMapping { SourceProperty = "Name" } }, })
                .MapCollection(GetCollectionJson(), model.CollectionFromJson)
                .Map(CurrentPage, model.SubModel);

            return CurrentTemplate(model);
        }

        public ActionResult UberDocTypeWithAttribute()
        {
            // Get related content
            var countryNodes = GetRelatedNodes();

            // Create view model and run mapping
            var model = new UberDocTypeViewModelWithAttribute();
            MapToViewModelWithAttributes(countryNodes, model);

            return CurrentTemplate(model);
        }

        private void MapToViewModelWithAttributes(IEnumerable<IPublishedContent> countryNodes, UberDocTypeViewModelWithAttribute model)
        {
            Mapper.Map(CurrentPage, model, new Dictionary<string, PropertyMapping> 
                    { 
                        { 
                            "ConditionalValueMet", new PropertyMapping 
                                { 
                                    SourceProperty = "heading",
                                    MapIfPropertyMatches = new KeyValuePair<string, string>("isApproved", "true"),
                                } 
                        }, 
                        { 
                            "ConditionalValueNotMet", new PropertyMapping 
                                { 
                                    SourceProperty = "heading",
                                    MapIfPropertyMatches = new KeyValuePair<string, string>("isApproved", "0"),
                                } 
                        },
                        { 
                            "UpperCaseHeading", new PropertyMapping 
                                { 
                                    SourceProperty = "heading",
                                    StringValueFormatter = x => 
                                    {
                                        return x.ToUpper();
                                    }
                                } 
                        }, 
                        { 
                            "FormattedCreatedOnDate", new PropertyMapping 
                                { 
                                    SourceProperty = "CreateDate",
                                    StringValueFormatter = x => 
                                    {
                                        return DateTime.Parse(x).ToString("dd MMMM, yyyy");
                                    }
                                } 
                        }, 
                    })
                .MapCollection(CurrentPage.Children.Where(x => x.IsDocumentType("Comment")), model.Comments)
                .MapCollection(countryNodes, model.Countries)
                .Map(GetSingleXml(), model, new Dictionary<string, PropertyMapping> { { "SingleValueFromXml", new PropertyMapping { SourceProperty = "Day" } }, })
                .MapCollection(GetCollectionXml(), model.CollectionFromXml, null, "Month")
                .Map(GetSingleDictionary(), model, new Dictionary<string, PropertyMapping> { { "SingleValueFromDictionary", new PropertyMapping { SourceProperty = "Animal" } }, })
                .MapCollection(GetCollectionDictionary(), model.CollectionFromDictionary)
                .Map(GetSingleJson(), model, new Dictionary<string, PropertyMapping> { { "SingleValueFromJson", new PropertyMapping { SourceProperty = "Name" } }, })
                .MapCollection(GetCollectionJson(), model.CollectionFromJson)
                .Map(CurrentPage, model.SubModel);
        }

        public ActionResult UberDocTypeWithAttributeAndDiagnostics()
        {
            // Get related content
            var countryNodes = GetRelatedNodes();

            var sw = new Stopwatch();

            var model = new UberDocTypeViewModelWithAttribute();

            var times = 10000;

            sw.Start();
            for (int i = 0; i < times; i++)
            {
                MapToViewModelWithAttributes(countryNodes, model);
            }

            var timeTaken = sw.ElapsedMilliseconds;
            sw.Stop();

            model.TimeTaken = string.Format("Time taken for {0} mapping operations: {1}ms", times, timeTaken);
            return CurrentTemplate(model);
        }

        private IEnumerable<IPublishedContent> GetRelatedNodes()
        {
            return CurrentPage.Value<IEnumerable<IPublishedContent>>("countries");
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
