namespace TestWebApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using Zone.UmbracoMapper;
    
    public class TestDocTypeViewModel
    {
        public int Id { get; set; }

        public string Heading { get; set; }

        public DateTime CreatedOn { get; set; }

        public IHtmlString BodyText { get; set; }        
        
    }
}
