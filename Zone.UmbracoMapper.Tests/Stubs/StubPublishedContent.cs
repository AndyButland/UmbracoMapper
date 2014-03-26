namespace Zone.UmbracoMapper.Tests.Stubs
{
    using System;
    using System.Collections.Generic;
    using Umbraco.Core.Models;

    public class StubPublishedContent : IPublishedContent
    {
        #region Stubbed properties and methods

        public int Id
        {
            get 
            {
                return 1000;
            }
        }

        public string Name
        {
            get
            {
                return "Test content";
            }
        }

        public string CreatorName
        {
            get 
            {
                return "A.N. Editor";
            }
        }

        public string DocumentTypeAlias
        {
            get
            {
                return "TestContent";
            }
        }

        public IPublishedContentProperty GetProperty(string alias)
        {
            return new StubPublishedContentProperty(alias);
        }

        #endregion

        #region Other properties and methods

        public IEnumerable<IPublishedContent> Children
        {
            get { throw new System.NotImplementedException(); }
        }

        public DateTime CreateDate
        {
            get { throw new System.NotImplementedException(); }
        }

        public int CreatorId
        {
            get { throw new System.NotImplementedException(); }
        }

        public int DocumentTypeId
        {
            get { throw new System.NotImplementedException(); }
        }

        public PublishedItemType ItemType
        {
            get { throw new System.NotImplementedException(); }
        }

        public int Level
        {
            get { throw new System.NotImplementedException(); }
        }

        public IPublishedContent Parent
        {
            get { throw new System.NotImplementedException(); }
        }

        public string Path
        {
            get { throw new System.NotImplementedException(); }
        }

        public ICollection<IPublishedContentProperty> Properties
        {
            get { throw new System.NotImplementedException(); }
        }

        public int SortOrder
        {
            get { throw new System.NotImplementedException(); }
        }

        public int TemplateId
        {
            get { throw new System.NotImplementedException(); }
        }

        public DateTime UpdateDate
        {
            get { throw new System.NotImplementedException(); }
        }

        public string Url
        {
            get { throw new System.NotImplementedException(); }
        }

        public string UrlName
        {
            get { throw new System.NotImplementedException(); }
        }

        public System.Guid Version
        {
            get { throw new System.NotImplementedException(); }
        }

        public int WriterId
        {
            get { throw new System.NotImplementedException(); }
        }

        public string WriterName
        {
            get { throw new System.NotImplementedException(); }
        }

        public object this[string propertyAlias]
        {
            get { throw new System.NotImplementedException(); }
        }

        #endregion
    }
}
