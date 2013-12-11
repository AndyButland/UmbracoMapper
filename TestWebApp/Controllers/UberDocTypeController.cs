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
            // Get related nodes selected through node picker
            IEnumerable<IPublishedContent> countryNodes = null;
            var countryIds = CurrentPage.GetPropertyValue<string>("countries");
            if (!string.IsNullOrEmpty(countryIds))
            {
                countryNodes = Umbraco.TypedContent(countryIds.Split(','));
            }

            // Get related links as XML
            var sr = new StringReader(CurrentPage.GetPropertyValue<string>("relatedLinks"));
            var relatedLinksXml = XElement.Load(sr);                

            // Create view model and run mapping
            var model = new UberDocTypeViewModel();
            Mapper.Map(CurrentPage, model, new Dictionary<string, string> { { "CreatedOn", "CreateDate" }, })
                  .MapCollection(CurrentPage.Children, model.Comments)
                  .MapCollection(countryNodes, model.Countries)
                  .MapCollection(relatedLinksXml, model.RelatedLinks, null, "link");

            // TODO:
            // - XML data test (single value and collection)
            // - dictionary data test (single value and collection)
            // - node picker test
            // - related links (XML)

            return CurrentTemplate(model);
        }
    }
}
