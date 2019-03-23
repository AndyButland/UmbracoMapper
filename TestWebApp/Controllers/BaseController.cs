﻿namespace TestWebApp.Controllers
{
    using System.Web.Mvc;
    using Umbraco.Web.Models;
    using Umbraco.Web.Mvc;
    using Zone.UmbracoMapper;
    using Zone.UmbracoMapper.DampCustomMapping;
    using Zone.UmbracoMapper.V7;

    public abstract class BaseController : SurfaceController, IRenderMvcController
    {
        protected IUmbracoMapper Mapper { get; set; }

        protected BaseController(IUmbracoMapper mapper)
        {
            Mapper = mapper;
            //Mapper.AddCustomMapping(typeof(MediaFile).FullName, DampMapper.MapMediaFile);
            Mapper.EnableCaching = true;
        }

        #region IRenderMvcController methods

        protected ActionResult CurrentTemplate<T>(T model)
        {
            return View(ControllerContext.RouteData.Values["action"].ToString(), model);
        }

        public virtual ActionResult Index(RenderModel model)
        {
            return CurrentTemplate(model);
        }

        #endregion
    }
}
