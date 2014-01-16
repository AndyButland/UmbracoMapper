namespace Zone.UmbracoMapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Web;
    using System.Xml.Linq;
    using Newtonsoft.Json.Linq;
    using Umbraco.Core.Models;
    using Umbraco.Web;
    using DampModel = DAMP.PropertyEditorValueConverter.Model;

    public static class DampMapper
    {
        #region Specific type converters

        /// <summary>
        /// Helper to map an IPublishedContent property to an object
        /// </summary>
        /// <param name="mapper">Mapper</param>
        /// <param name="contentToMapFrom">Umbraco content item to map from</param>
        /// <param name="propName">Name of the property to map</param>
        /// <returns>MediaFile instance</returns>
        public static object MapMediaFile(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propName)
        {
            return GetMediaFile(contentToMapFrom.GetPropertyValue<DampModel>(propName), mapper.AssetsRootUrl);
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

        #endregion
    }
}
