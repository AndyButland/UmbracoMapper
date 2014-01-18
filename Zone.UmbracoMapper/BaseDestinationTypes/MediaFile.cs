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

        public int Width { get; set; }

        public int Height { get; set; }

        public int Size { get; set; }

        public string FileExtension { get; set; }
    }
}
