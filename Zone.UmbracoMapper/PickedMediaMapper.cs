namespace Zone.UmbracoMapper.V7
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Reflection;
    using Umbraco.Core.Models;
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
        /// <param name="isRecursive">Flag to indicate if property should be retrieved recursively up the tree</param>
        /// <returns>MediaFile instance</returns>
        public static object MapMediaFileCollection(IUmbracoMapper mapper, IPublishedContent contentToMapFrom,
            string propName, bool isRecursive)
        {
            // If Umbraco Core Property Editor Converters will get IEnumerable<IPublishedContent>, so try that first
            var mediaCollection = GetTypedMediaFromPropertyValue<IEnumerable<IPublishedContent>>(contentToMapFrom, propName, isRecursive);
            if (mediaCollection == null)
            {
                // Also check for single IPublishedContent (which could get if multiple media disabled)
                var media = contentToMapFrom.GetPropertyValue<IPublishedContent>(propName, isRecursive, null);
                if (media != null)
                {
                    mediaCollection = new List<IPublishedContent> { media };
                }

                if (mediaCollection == null)
                {
                    // If Umbraco Core Property Editor Converters not installed, need to dig out the Ids
                    var mediaIds = contentToMapFrom.GetPropertyValue<string>(propName, isRecursive, string.Empty);
                    if (!string.IsNullOrEmpty(mediaIds))
                    {
                        var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
                        mediaCollection = new List<IPublishedContent>();
                        foreach (var mediaId in mediaIds.Split(','))
                        {
                            ((List<IPublishedContent>)mediaCollection).Add(umbracoHelper.TypedMedia(mediaId));
                        }
                    }
                }
            }

            return mediaCollection != null ? GetMediaFileCollection(mediaCollection, mapper.AssetsRootUrl) : null;
        }

        private static T GetTypedMediaFromPropertyValue<T>(IPublishedContent contentToMapFrom, string propName, bool isRecursive)
        {
            return contentToMapFrom.GetPropertyValue<T>(propName, isRecursive);
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
            var media = GetTypedMediaFromPropertyValue<IPublishedContent>(contentToMapFrom, propName, isRecursive);
            if (media == null)
            {
                // If Umbraco Core Property Editor Converters not installed, need to dig out the Id
                var mediaId = contentToMapFrom.GetPropertyValue(propName, isRecursive, string.Empty);
                if (!string.IsNullOrEmpty(mediaId))
                {
                    var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
                    media = umbracoHelper.TypedMedia(mediaId);
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
                DocumentTypeAlias = media.DocumentTypeAlias,
                Width = media.GetPropertyValue<int>("umbracoWidth"),
                Height = media.GetPropertyValue<int>("umbracoHeight"),
                Size = media.GetPropertyValue<int>("umbracoBytes"),
                FileExtension = media.GetPropertyValue<string>("umbracoExtension"),
                AltText = media.GetPropertyValue<string>("altText")
            };

            return mediaFile;
        }
    }
}
