namespace TestWebApp80.Controllers
{
    using System.Web.Mvc;
    using Umbraco.Web.Models;
    using Umbraco.Web.Mvc;
    using Zone.UmbracoMapper.V8;

    public abstract class BaseController : SurfaceController, IRenderMvcController
    {
        protected IUmbracoMapper Mapper { get; set; }

        protected BaseController(IUmbracoMapper mapper)
        {
            Mapper = mapper;
            Mapper.EnableCaching = true;
        }

        #region IRenderMvcController methods

        protected ActionResult CurrentTemplate<T>(T model)
        {
            return View(ControllerContext.RouteData.Values["action"].ToString(), model);
        }

        public virtual ActionResult Index(ContentModel model)
        {
            return CurrentTemplate(model);
        }

        #endregion
    }
}
