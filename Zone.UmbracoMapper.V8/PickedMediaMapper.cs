namespace Zone.UmbracoMapper.V8
{
    using System.Collections.Generic;
    using System.Linq;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Zone.UmbracoMapper.Common.BaseDestinationTypes;

    public static class PickedMediaMapper
    {
        /// <summary>
        /// Native mapper for mapping a multiple  media picker property
        /// </summary>
        /// <param name="mapper">The mapper</param>
        /// <param name="contentToMapFrom">Umbraco content item to map from</param>
        /// <param name="propName">Name of the property to map</param>
        /// <param name="fallback">Fallback method(s) to use when content not found</param>
        /// <returns>MediaFile instance</returns>
        public static object MapMediaFileCollection(IUmbracoMapper mapper, IPublishedContent contentToMapFrom,
            string propName, Fallback fallback)
        {
            // With V8 will get IPublishedContent
            var mediaCollection = contentToMapFrom.Value<IEnumerable<IPublishedContent>>(propName, fallback: fallback);
            if (mediaCollection == null)
            {
                // Also check for single IPublishedContent (which could get if multiple media disabled)
                var media = contentToMapFrom.Value<IPublishedContent>(propName, fallback: fallback);
                if (media != null)
                {
                    mediaCollection = new List<IPublishedContent> { media };
                }
            }

            return mediaCollection != null ? GetMediaFileCollection(mediaCollection, mapper.AssetsRootUrl) : null;
        }

        /// <summary>
        /// Native mapper for mapping a media picker property
        /// </summary>
        /// <param name="mapper">The mapper</param>
        /// <param name="contentToMapFrom">Umbraco content item to map from</param>
        /// <param name="propName">Name of the property to map</param>
        /// <param name="fallback">Fallback method(s) to use when content not found</param>
        /// <returns>MediaFile instance</returns>
        public static object MapMediaFile(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, 
            string propName, Fallback fallback)
        {
            // With V8 will get IPublishedContent
            var media = contentToMapFrom.Value<IPublishedContent>(propName, fallback: fallback);
            return media != null ? GetMediaFile(media, mapper.AssetsRootUrl) : null;
        }

        /// <summary>
        /// Helper to convert a collection of IPublishedContent media item into a collection of MediaFile objects
        /// </summary>
        /// <param name="mediaCollection">Selected media</param>
        /// <param name="rootUrl">Root Url for mapping full path to media</param>
        /// <returns>MediaFile instance</returns>
        private static IEnumerable<MediaFile> GetMediaFileCollection(IEnumerable<IPublishedContent> mediaCollection, string rootUrl)
        {
            return mediaCollection.Select(media => GetMediaFile(media, rootUrl));
        }

        /// <summary>
        /// Helper to convert an IPublishedContent media item into a MediaFile object
        /// </summary>
        /// <param name="media">Selected media</param>
        /// <param name="rootUrl">Root Url for mapping full path to media</param>
        /// <returns>MediaFile instance</returns>
        private static MediaFile GetMediaFile(IPublishedContent media, string rootUrl)
        {
            var mediaFile = new MediaFile
            {
                Id = media.Id,
                Name = media.Name,
                Url = media.Url,
                DomainWithUrl = rootUrl + media.Url,
                DocumentTypeAlias = media.ContentType.Alias,
                Width = media.Value<int>("umbracoWidth"),
                Height = media.Value<int>("umbracoHeight"),
                Size = media.Value<int>("umbracoBytes"),
                FileExtension = media.Value<string>("umbracoExtension"),
                AltText = media.Value<string>("altText")
            };

            return mediaFile;
        }
    }
}
