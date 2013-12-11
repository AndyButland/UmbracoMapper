namespace TestWebApp.Models
{
    using System;
    using System.Web;
    
    public class LinkViewModel
    {
        public string Title { get; set; }

        public string Link { get; set; }

        public bool NewWindow { get; set; }
    }
}