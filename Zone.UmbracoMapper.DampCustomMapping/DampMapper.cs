namespace Zone.UmbracoMapper.DampCustomMapping
{
    using Umbraco.Core.Models;
    using Umbraco.Web;
    using DampModel = DAMP.PropertyEditorValueConverter.Model;

    public static class DampMapper
    {
        /// <summary>
        /// Custom mapper for mapping a DAMP property
        /// </summary>
        /// <param name="mapper">The mapper</param>
        /// <param name="contentToMapFrom">Umbraco content item to map from</param>
        /// <param name="propName">Name of the property to map</param>
        /// <param name="isRecursive">Flag to indicate if property should be retrieved recursively up the tree</param>
        /// <returns>MediaFile instance</returns>
        public static object MapMediaFile(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propName, bool isRecursive)
        {
            return GetMediaFile(contentToMapFrom.GetPropertyValue<DampModel>(propName, isRecursive, null), mapper.AssetsRootUrl);
        }

        /// <summary>
        /// Helper to convert a DAMP model into a standard MediaFile object
        /// </summary>
        /// <param name="dampModel">DAMP model</param>
        /// <returns>MediaFile instance</returns>
        private static MediaFile GetMediaFile(DampModel dampModel, string rootUrl)
        {
            if (dampModel != null && dampModel.Any)
            {
                var mediaFile = new MediaFile();

                var dampModelItem = dampModel.First;

                mediaFile.Id = dampModelItem.Id;
                mediaFile.Name = dampModelItem.Name;
                mediaFile.Url = dampModelItem.Url;
                mediaFile.DomainWithUrl = rootUrl + dampModelItem.Url;
                mediaFile.DocumentTypeAlias = dampModelItem.Type;

                if (dampModelItem.Type == "Image")
                {
                    int tempWidth;
                    if (int.TryParse(dampModelItem.GetProperty("umbracoWidth"), out tempWidth))
                    {
                        mediaFile.Width = tempWidth;
                    }

                    int tempHeight;
                    if (int.TryParse(dampModelItem.GetProperty("umbracoHeight"), out tempHeight))
                    {
                        mediaFile.Height = tempHeight;
                    }
                }

                int tempSize;
                if (int.TryParse(dampModelItem.GetProperty("umbracoBytes"), out tempSize))
                {
                    mediaFile.Size = tempSize;
                }

                mediaFile.FileExtension = dampModelItem.GetProperty("umbracoExtension");

                mediaFile.AltText = string.IsNullOrWhiteSpace(dampModelItem.GetProperty("altText"))
                    ? dampModelItem.Alt
                    : dampModelItem.GetProperty("altText");

                return mediaFile;
            }

            return null;
        }
    }
}
