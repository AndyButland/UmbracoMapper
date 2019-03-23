namespace Zone.UmbracoMapper.Common.BaseDestinationTypes
{
    public class BaseNodeViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string DocumentTypeAlias { get; set; }

        public int SortOrder { get; set; }

        public string Url { get; set; }

        public int Level { get; set; }
    }
}
