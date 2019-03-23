namespace Zone.UmbracoMapper.V8
{
    using System.Collections.Generic;
    using System.Linq;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Composing;
    using Zone.UmbracoMapper.Common.BaseDestinationTypes;
    using Zone.UmbracoMapper.V8.Extensions;

    public static class PickedMediaMapper
    {
        /// <summary>
        /// Native mapper for mapping a multiple  media picker property
        /// </summary>
        /// <param name="mapper">The mapper</param>
        /// <param name="contentToMapFrom">Umbraco content item to map from</param>
        /// <param name="propName">Name of the property to map</param>
        /// <param name="isRecursive">Flag to indicate if property should be retrieved recursively up the tree</param>
        /// <returns>MediaFile instance</returns>
        public static object MapMediaFileCollection(IUmbracoMapper mapper, IPublishedContent contentToMapFrom,
            string propName, bool isRecursive)
        {
            // If Umbraco Core Property Editor Converters will get IEnumerable<IPublishedContent>, so try that first
            var fallback = isRecursive.ToRecuriveFallback();
            var mediaCollection = contentToMapFrom.Value<IEnumerable<IPublishedContent>>(propName, fallback: fallback);
            if (mediaCollection == null)
            {
                // Also check for single IPublishedContent (which could get if multiple media disabled)
                var media = contentToMapFrom.Value<IPublishedContent>(propName, fallback: fallback);
                if (media != null)
                {
                    mediaCollection = new List<IPublishedContent> { media };
                }

                if (mediaCollection == null)
                {
                    // If Umbraco Core Property Editor Converters not installed, need to dig out the Ids
                    var mediaIds = contentToMapFrom.Value<string>(propName, fallback: fallback);
                    if (!string.IsNullOrEmpty(mediaIds))
                    {
                        mediaCollection = new List<IPublishedContent>();
                        foreach (var mediaId in mediaIds.Split(','))
                        {
                            ((List<IPublishedContent>)mediaCollection).Add(Current.UmbracoHelper.Media(mediaId));
                        }
                    }
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
        /// <param name="isRecursive">Flag to indicate if property should be retrieved recursively up the tree</param>
        /// <returns>MediaFile instance</returns>
        public static object MapMediaFile(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, 
            string propName, bool isRecursive)
        {
            // If Umbraco Core Property Editor Converters will get IPublishedContent, so try that first
            var fallback = isRecursive.ToRecuriveFallback();
            var media = contentToMapFrom.Value<IPublishedContent>(propName, fallback: fallback);
            if (media == null)
            {
                // If Umbraco Core Property Editor Converters not installed, need to dig out the Id
                var mediaId = contentToMapFrom.Value<string>(propName, fallback: fallback);
                if (!string.IsNullOrEmpty(mediaId))
                {
                    media = Current.UmbracoHelper.Media(mediaId);
                }
            }

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
