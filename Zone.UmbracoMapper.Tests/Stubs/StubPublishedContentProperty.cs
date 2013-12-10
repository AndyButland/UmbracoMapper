namespace Zone.UmbracoMapper.Tests.Stubs
{
    using System;
    using Umbraco.Core.Models;

    public class StubPublishedContentProperty : IPublishedContentProperty
    {
        private readonly string _alias;
        private readonly object _value;

        public StubPublishedContentProperty(string alias, object value)
        {
            _alias = alias;
            _value = value;
        }

        public string Alias
        {
            get { return _alias; }
        }

        public object Value
        {
            get { return _value; }
        }

        public Guid Version
        {
            get { throw new NotImplementedException(); }
        }
    }
}
