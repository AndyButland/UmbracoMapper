namespace Zone.UmbracoMapper.Tests.Stubs
{
    using System;
    using System.Collections.Generic;
    using Umbraco.Core.Models;
 
    public class StubPublishedContent : IPublishedContent
    {
        #region Interface properties and methods

        public IEnumerable<IPublishedContent> Children
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime CreateDate
        {
            get { throw new NotImplementedException(); }
        }

        public int CreatorId
        {
            get { throw new NotImplementedException(); }
        }

        public string CreatorName
        {
            get { return "A.N. Editor"; }
        }

        public string DocumentTypeAlias
        {
            get { return "SimpleDocType"; }
        }

        public int DocumentTypeId
        {
            get { throw new NotImplementedException(); }
        }

        public int Id
        {
            get { return 1000; }
        }

        public PublishedItemType ItemType
        {
            get { throw new NotImplementedException(); }
        }

        public int Level
        {
            get { throw new NotImplementedException(); }
        }

        public string Name
        {
            get { return "Test Content"; }
        }

        public IPublishedContent Parent
        {
            get { throw new NotImplementedException(); }
        }

        public string Path
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection<IPublishedContentProperty> Properties
        {
            get { throw new NotImplementedException(); }
        }

        public int SortOrder
        {
            get { throw new NotImplementedException(); }
        }

        public int TemplateId
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime UpdateDate
        {
            get { throw new NotImplementedException(); }
        }

        public string Url
        {
            get { throw new NotImplementedException(); }
        }

        public string UrlName
        {
            get { throw new NotImplementedException(); }
        }

        public Guid Version
        {
            get { throw new NotImplementedException(); }
        }

        public int WriterId
        {
            get { throw new NotImplementedException(); }
        }

        public string WriterName
        {
            get { throw new NotImplementedException(); }
        }

        public object this[string propertyAlias]
        {
            get { throw new NotImplementedException(); }
        }

        public IPublishedContentProperty GetProperty(string alias)
        {
            switch (alias)
            {
                case "bodyText":
                    return new StubPublishedContentProperty("bodyText", "This is the body text");
            }

            return null;
        }

        #endregion

        #region Overrides of Umbraco extension methods

        public object GetPropertyValue(string alias)
        {
            return GetPropertyValue(alias, false);
        }

        public object GetPropertyValue(string alias, bool recursive)
        {
            switch (alias)
            {
                case "bodyText":
                    return "This is the body text";
            }

            return null;
        }

        #endregion
    }
}
