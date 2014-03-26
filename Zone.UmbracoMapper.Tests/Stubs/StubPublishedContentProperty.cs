namespace Zone.UmbracoMapper.Tests.Stubs
{
    using System;
    using System.Collections.Generic;
    using Umbraco.Core.Models;

    public class StubPublishedContentProperty : IPublishedContentProperty
    {
        private readonly string _alias;

        public StubPublishedContentProperty(string alias)
        {
            _alias = alias;
        }

        public string Alias
        {
            get 
            {
                return _alias;
            }
        }

        public object Value
        {
            get { throw new NotImplementedException(); }
        }

        public Guid Version
        {
            get { throw new NotImplementedException(); }
        }
    }
}
