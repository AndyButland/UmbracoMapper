namespace Zone.UmbracoMapper.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Zone.UmbracoMapper.Tests.Stubs;

    [TestClass]
    public class UmbracoMapperTests
    {
        private const string SiteUrl = "http://www.example.com";        

        #region Test methods

        [TestMethod]
        public void UmbracoMapper_MapsNativePropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map<SimpleViewModel>(content, model);            

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test Content", model.Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapsNativePropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel2();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map<SimpleViewModel2>(content, model, new Dictionary<string, string> { { "Author", "CreatorName" } });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test Content", model.Name);
            Assert.AreEqual("A.N. Editor", model.Author);
        }

        [TestMethod]
        public void UmbracoMapper_MapsCustomPropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map<SimpleViewModel3>(content, model);

            // Assert
            Assert.AreEqual("This is the body text", model.BodyText);
        }

        #endregion

        #region Test helpers

        private IUmbracoMapper GetMapper()
        {
            return new UmbracoMapper(SiteUrl);
        }

        private XElement GetXmlForSingle()
        {
            return new XElement("item",
                new XElement("id", 1),
                new XElement("name", "Test name")
            );
        }

        private XElement GetXmlForCollection()
        {
            return new XElement("items",
                new XElement("item",
                    new XElement("id", 1),
                    new XElement("name", "Test name 1")
                ),
                new XElement("item",
                    new XElement("id", 2),
                    new XElement("name", "Test name 2")
                ),
                new XElement("item",
                    new XElement("id", 3),
                    new XElement("name", "Test name 3")
                )
            );
        }

        #endregion

        #region Test model classes

        private class SimpleViewModel
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        private class SimpleViewModel2 : SimpleViewModel
        {
            public string Author { get; set; }
        }

        private class SimpleViewModel3 : SimpleViewModel2
        {
            public string BodyText { get; set; }
        }

        #endregion
    }
}
