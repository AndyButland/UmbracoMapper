namespace TestWebApp.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Web.Mvc;
    using System.Xml.Linq;
    using TestWebApp.Models;
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Web;
    using Umbraco.Web.Models;
    using Zone.UmbracoMapper;

    public class NewsLandingPageController : BaseController
    {
        public NewsLandingPageController(IUmbracoMapper mapper)
            : base(mapper)
        {
        }

        public ActionResult NewsLandingPage()
        {
            var model = new NewsLandingPageViewModel();
            Mapper.Map(CurrentPage, model);

            return CurrentTemplate(model);
        }
    }
}
