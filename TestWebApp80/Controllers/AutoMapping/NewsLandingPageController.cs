namespace TestWebApp80.Controllers
{
    using System.Web.Mvc;
    using TestWebApp80.Models;
    using Zone.UmbracoMapper.V8;

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
