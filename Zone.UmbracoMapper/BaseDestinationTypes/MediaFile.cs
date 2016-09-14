namespace Zone.UmbracoMapper
{
    public class MediaFile
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string DocumentTypeAlias { get; set; }

        public int SortOrder { get; set; }

        public string Url { get; set; }

        public string DomainWithUrl { get; set; }

        public string AltText { get; set; }

        [PropertyMapping(SourceProperty = "umbracoWidth")]
        public int Width { get; set; }

        [PropertyMapping(SourceProperty = "umbracoHeight")]
        public int Height { get; set; }

        [PropertyMapping(SourceProperty = "umbracoBytes")]
        public int Size { get; set; }

        [PropertyMapping(SourceProperty = "umbracoExtension")]
        public string FileExtension { get; set; }
    }
}
