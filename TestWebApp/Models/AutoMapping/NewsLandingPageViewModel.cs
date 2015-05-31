namespace TestWebApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using Zone.UmbracoMapper;

    public class NewsLandingPageViewModel
    {
        public NewsLandingPageViewModel()
        {
            NewsCategory = new Category();
            TopStory = new NewsStory();
            OtherStories = new List<NewsStory>();
        }

        public string Heading { get; set; }

        [PropertyMapping(LevelsAbove = 1)]
        public Category NewsCategory { get; set; }

        public NewsStory TopStory { get; set; }

        public IEnumerable<NewsStory> OtherStories { get; set; }

        public class Category
        {
            public string Title { get; set; }
        }

        public class NewsStory
        {
            public string Headline { get; set; }

            public DateTime StoryDate { get; set; }

            public IHtmlString BodyText { get; set; }
        }
    }
}