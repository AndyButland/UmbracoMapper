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
        #region Test methods

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test Content", model.Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel2();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content, model, new Dictionary<string, string> { { "Author", "CreatorName" } });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test Content", model.Name);
            Assert.AreEqual("A.N. Editor", model.Author);
        }

        // TODO: get this failing test working (requires mock or stub of IPublishedContent and dependencies)
        // [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content, model);

            // Assert
            Assert.AreEqual("This is the body text", model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXml_MapsPropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel4();
            var xml = GetXmlForSingle();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model);

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(1234567890, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
            Assert.AreEqual((decimal)12.73, model.AverageScore);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXml_MapsPropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel4();
            var xml = GetXmlForSingle2();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model, new Dictionary<string, string> { { "Name", "Name2" }, { "RegisteredOn", "RegistrationDate" } });

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(1234567890, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXml_MapsPropertiesWithCaseInsensitiveMatchOnElementNames()
        {
            // Arrange
            var model = new SimpleViewModel4();
            var xml = GetXmlForSingle3();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model);

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(1234567890, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
            Assert.AreEqual((decimal)12.73, model.AverageScore);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionary_MapsPropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel4();
            var dictionary = GetDictionaryForSingle();
            var mapper = GetMapper();

            // Act
            mapper.Map(dictionary, model);

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(1234567890, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionary_MapsPropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel4();
            var dictionary = GetDictionaryForSingle2();
            var mapper = GetMapper();

            // Act
            mapper.Map(dictionary, model, new Dictionary<string, string> { { "Name", "Name2" }, { "RegisteredOn", "RegistrationDate" } });

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(1234567890, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXmlToCollection_MapsPropertiesForExistingEntriesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                    new Comment
                    {
                        Id = 2,                         
                    }
                }
            };

            var xml = GetXmlForCommentsCollection();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(xml, model.Comments);

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXmlToCollection_MapsPropertiesForExistingEntriesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                    new Comment
                    {
                        Id = 2,                         
                    }
                }
            };

            var xml = GetXmlForCommentsCollection2();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(xml, model.Comments, new Dictionary<string, string> { { "CreatedOn", "RecordedOn" } });

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXmlToCollection_MapsPropertiesWithCustomItemElementName()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                    new Comment
                    {
                        Id = 2,                         
                    }
                }
            };

            var xml = GetXmlForCommentsCollection4();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(xml, model.Comments, null, "Entry");

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXmlToCollection_DoesntMapNonExistingItemsUnlessRequestedToDoSo()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                }
            };

            var xml = GetXmlForCommentsCollection();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(xml, model.Comments, null, "Item", false);

            // Assert
            Assert.AreEqual(1, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXmlToCollection_MapsAdditionalItemsWhenRequestedToDoSo()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                }
            };

            var xml = GetXmlForCommentsCollection();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(xml, model.Comments);

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual(2, model.Comments[1].Id);
            Assert.AreEqual("Sally Smith", model.Comments[1].Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXmlToCollection_MapsWithDifferentLookUpPropertyNames()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                    new Comment
                    {
                        Id = 2,                         
                    },
                }
            };

            var xml = GetXmlForCommentsCollection3();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(xml, model.Comments, null, "Item", false, "Id", "Identifier");

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionaryToCollection_MapsPropertiesForExistingEntriesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                    new Comment
                    {
                        Id = 2,                         
                    }
                }
            };

            var dictionary = GetDictionaryForCommentsCollection();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(dictionary, model.Comments);

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionaryToCollection_MapsPropertiesForExistingEntriesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                    new Comment
                    {
                        Id = 2,                         
                    }
                }
            };

            var dictionary = GetDictionaryForCommentsCollection2();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(dictionary, model.Comments, new Dictionary<string, string> { { "CreatedOn", "RecordedOn" } });

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionaryToCollection_DoesntMapNonExistingItemsUnlessRequestedToDoSo()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                }
            };

            var dictionary = GetDictionaryForCommentsCollection();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(dictionary, model.Comments, null, false);

            // Assert
            Assert.AreEqual(1, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionaryToCollection_MapsAdditionalItemsWhenRequestedToDoSo()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                }
            };

            var dictionary = GetDictionaryForCommentsCollection();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(dictionary, model.Comments, null, true);

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual(2, model.Comments[1].Id);
            Assert.AreEqual("Sally Smith", model.Comments[1].Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionaryToCollection_MapsWithDifferentLookUpPropertyNames()
        {
            // Arrange
            var model = new SimpleViewModelWithCollection
            {
                Id = 1,
                Name = "Test name",
                Comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = 1,                         
                    },
                    new Comment
                    {
                        Id = 2,                         
                    },
                }
            };

            var dictionary = GetDictionaryForCommentsCollection3();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(dictionary, model.Comments, null, false, "Id", "Identifier");

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        #endregion

        #region Test helpers

        private IUmbracoMapper GetMapper()
        {
            return new UmbracoMapper();
        }

        private XElement GetXmlForSingle()
        {
            return new XElement("Item",
                new XElement("Id", 1),
                new XElement("Name", "Test name"),
                new XElement("Age", "21"),
                new XElement("FacebookId", "1234567890"),
                new XElement("RegisteredOn", "2013-04-13"),
                new XElement("AverageScore", "12.73"));
        }

        private XElement GetXmlForSingle2()
        {
            return new XElement("Item",
                new XElement("Id", 1),
                new XElement("Name2", "Test name"),
                new XElement("Age", "21"),
                new XElement("FacebookId", "1234567890"),
                new XElement("RegistrationDate", "2013-04-13"));
        }

        private XElement GetXmlForSingle3()
        {
            return new XElement("item",
                new XElement("id", 1),
                new XElement("name", "Test name"),
                new XElement("age", "21"),
                new XElement("facebookId", "1234567890"),
                new XElement("registeredOn", "2013-04-13"),
                new XElement("averageScore", "12.73"));
        }

        private XElement GetXmlForCommentsCollection()
        {
            return new XElement("Items",
                new XElement("Item",
                    new XElement("Id", 1),
                    new XElement("Name", "Fred Bloggs"),
                    new XElement("Text", "Fred's comment"),
                    new XElement("CreatedOn", "2013-04-13 09:30")),
                new XElement("Item",
                    new XElement("Id", 2),
                    new XElement("Name", "Sally Smith"),
                    new XElement("Text", "Sally's comment"),
                    new XElement("CreatedOn", "2013-04-13 10:30")));
        }

        private XElement GetXmlForCommentsCollection2()
        {
            return new XElement("Items",
                new XElement("Item",
                    new XElement("Id", 1),
                    new XElement("Name", "Fred Bloggs"),
                    new XElement("Text", "Fred's comment"),
                    new XElement("RecordedOn", "2013-04-13 09:30")),
                new XElement("Item",
                    new XElement("Id", 2),
                    new XElement("Name", "Sally Smith"),
                    new XElement("Text", "Sally's comment"),
                    new XElement("RecordedOn", "2013-04-13 10:30")));
        }

        private XElement GetXmlForCommentsCollection3()
        {
            return new XElement("Items",
                new XElement("Item",
                    new XElement("Identifier", 1),
                    new XElement("Name", "Fred Bloggs"),
                    new XElement("Text", "Fred's comment"),
                    new XElement("CreatedOn", "2013-04-13 09:30")),
                new XElement("Item",
                    new XElement("Identifier", 2),
                    new XElement("Name", "Sally Smith"),
                    new XElement("Text", "Sally's comment"),
                    new XElement("CreatedOn", "2013-04-13 10:30")));
        }

        private XElement GetXmlForCommentsCollection4()
        {
            return new XElement("Entries",
                new XElement("Entry",
                    new XElement("Id", 1),
                    new XElement("Name", "Fred Bloggs"),
                    new XElement("Text", "Fred's comment"),
                    new XElement("CreatedOn", "2013-04-13 09:30")),
                new XElement("Entry",
                    new XElement("Id", 2),
                    new XElement("Name", "Sally Smith"),
                    new XElement("Text", "Sally's comment"),
                    new XElement("CreatedOn", "2013-04-13 10:30")));
        }

        private Dictionary<string, object> GetDictionaryForSingle()
        {
            return new Dictionary<string, object> 
            { 
                { "Id", 1 }, 
                { "Name", "Test name" },
                { "Age", 21 },
                { "FacebookId", 1234567890 },
                { "RegisteredOn", new DateTime(2013, 4, 13) },
            };
        }

        private Dictionary<string, object> GetDictionaryForSingle2()
        {
            return new Dictionary<string, object> 
            { 
                { "Id", 1 }, 
                { "Name2", "Test name" },
                { "Age", 21 },
                { "FacebookId", 1234567890 },
                { "RegistrationDate", new DateTime(2013, 4, 13) },
            };
        }

        private IEnumerable<Dictionary<string, object>> GetDictionaryForCommentsCollection()
        {
            return new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object>
                    {
                    { "Id", 1 }, 
                    { "Name", "Fred Bloggs" },
                    { "Text", "Fred's comment" },
                    { "CreatedOn", new DateTime(2013, 4, 13, 9, 30, 0) },
                },
                new Dictionary<string, object>
                    {
                    { "Id", 2 }, 
                    { "Name", "Sally Smith" },
                    { "Text", "Sally's comment" },
                    { "CreatedOn", new DateTime(2013, 4, 13, 10, 30, 0) },
                },
            };
        }

        private IEnumerable<Dictionary<string, object>> GetDictionaryForCommentsCollection2()
        {
            return new List<Dictionary<string, object>>
            { 
                new Dictionary<string, object>
                    {
                    { "Id", 1 }, 
                    { "Name", "Fred Bloggs" },
                    { "Text", "Fred's comment" },
                    { "RecordedOn", new DateTime(2013, 4, 13, 9, 30, 0) },
                },
                new Dictionary<string, object>
                    {
                    { "Id", 2 }, 
                    { "Name", "Sally Smith" },
                    { "Text", "Sally's comment" },
                    { "RecordedOn", new DateTime(2013, 4, 13, 10, 30, 0) },
                },
            };
        }

        private IEnumerable<Dictionary<string, object>> GetDictionaryForCommentsCollection3()
        {
            return new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object>
                    {
                    { "Identifier", 1 }, 
                    { "Name", "Fred Bloggs" },
                    { "Text", "Fred's comment" },
                    { "CreatedOn", new DateTime(2013, 4, 13, 9, 30, 0) },
                },
                new Dictionary<string, object>
                    {
                    { "Identifier", 2 }, 
                    { "Name", "Sally Smith" },
                    { "Text", "Sally's comment" },
                    { "CreatedOn", new DateTime(2013, 4, 13, 10, 30, 0) },
                },
            };
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

        private class SimpleViewModel4 : SimpleViewModel
        {
            public byte Age { get; set; }

            public long FacebookId { get; set; }

            public decimal AverageScore { get; set; }

            public DateTime RegisteredOn { get; set; }
        }

        private class SimpleViewModelWithCollection : SimpleViewModel
        {
            public IList<Comment> Comments { get; set; }
        }

        private class Comment
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Text { get; set; }

            public DateTime CreatedOn { get; set; }
        }

        #endregion
    }
}
