namespace Zone.UmbracoMapper.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.QualityTools.Testing.Fakes;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Umbraco.Core.Models;
    using Umbraco.Web;
    using Zone.UmbracoMapper.Tests.Stubs;

    [TestClass]
    public class UmbracoMapperTests
    {
        #region Tests - Single Maps From IPublishedContent

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
            Assert.AreEqual("Test content", model.Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel2();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content, model, new Dictionary<string, PropertyMapping> { { "Author", new PropertyMapping { SourceProperty = "CreatorName", } } });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("A.N. Editor", model.Author);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesWithStringFormatter()
        {
            // Arrange
            var model = new SimpleViewModel();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content, model, new Dictionary<string, PropertyMapping> { { "Name", new PropertyMapping { StringValueFormatter = x => { return x.ToUpper(); }, } } });

            // Assert
            Assert.AreEqual("TEST CONTENT", model.Name);
        }
        
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesWithDifferentNamesUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel2WithAttribute();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("A.N. Editor", model.Author);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesWithDifferentNamesUsingAttributeAndExistingDictionary()
        {
            // Arrange
            var model = new SimpleViewModel2WithAttribute();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content, model, new Dictionary<string, PropertyMapping> { { "SomeOtherProperty", new PropertyMapping { SourceProperty = "SomeOtherSourceProperty", } } });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("A.N. Editor", model.Author);
        }
        
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithMatchingNames()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }                        
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.AreEqual(1000, model.Id);
                Assert.AreEqual("Test content", model.Name);
                Assert.AreEqual("This is the body text", model.BodyText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithDifferentNames()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel4();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, new Dictionary<string, PropertyMapping> { { "BodyCopy", new PropertyMapping { SourceProperty = "bodyText", } } });

                // Assert
                Assert.AreEqual(1000, model.Id);
                Assert.AreEqual("Test content", model.Name);
                Assert.AreEqual("This is the body text", model.BodyCopy);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithStringFormatter()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, new Dictionary<string, PropertyMapping> { { "BodyText", new PropertyMapping { StringValueFormatter = x => { return x.ToUpper(); } } } });

                // Assert
                Assert.AreEqual("THIS IS THE BODY TEXT", model.BodyText);
            }
        }        
        
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithDifferentNamesUsingAttribute()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel4WithAttribute();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.AreEqual(1000, model.Id);
                Assert.AreEqual("Test content", model.Name);
                Assert.AreEqual("This is the body text", model.BodyCopy);
            }
        }
        
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithConcatenation()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel5();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, new Dictionary<string, PropertyMapping> 
                    { 
                        { 
                            "HeadingAndBodyText", 
                            new PropertyMapping 
                            { 
                                SourcePropertiesForConcatenation = new string[] { "Name", "bodyText" }, 
                                ConcatenationSeperator = ",",
                            } 
                        } 
                    });

                // Assert
                Assert.AreEqual("Test content,This is the body text", model.HeadingAndBodyText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithConcatenationUsingAttribute()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel5WithAttribute();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.AreEqual("Test content,This is the body text", model.HeadingAndBodyText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithCoalescingAndFirstItemAvailable()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel5();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "summaryText":
                                return "This is the summary text";
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, new Dictionary<string, PropertyMapping> 
                    { 
                        { 
                            "SummaryText", 
                            new PropertyMapping 
                            { 
                                SourcePropertiesForCoalescing = new string[] { "summaryText", "bodyText" }, 
                            } 
                        } 
                    });

                // Assert
                Assert.AreEqual("This is the summary text", model.SummaryText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithCoalescingAndFirstItemAvailableUsingAttribute()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel5WithAttribute();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "summaryText":
                                return "This is the summary text";
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.AreEqual("This is the summary text", model.SummaryText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithCoalescingAndFirstItemNotAvailable()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel5();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "summaryText":
                                return string.Empty;
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, new Dictionary<string, PropertyMapping> 
                    { 
                        { 
                            "SummaryText", 
                            new PropertyMapping 
                            { 
                                SourcePropertiesForCoalescing = new string[] { "summaryText", "bodyText" }, 
                            } 
                        } 
                    });

                // Assert
                Assert.AreEqual("This is the body text", model.SummaryText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithCoalescingAndFirstItemNotAvailableUsingAttribute()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel5WithAttribute();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "summaryText":
                                return string.Empty;
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.AreEqual("This is the body text", model.SummaryText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithMatchingCondition()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "summaryText":
                                return "Test value";
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, new Dictionary<string, PropertyMapping> 
                    { 
                        { 
                            "BodyText", 
                            new PropertyMapping 
                            { 
                                MapIfPropertyMatches = new KeyValuePair<string, string>("summaryText", "Test value")
                            } 
                        } 
                    });

                // Assert
                Assert.AreEqual("This is the body text", model.BodyText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithNonMatchingCondition()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "summaryText":
                                return "Another value";
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, new Dictionary<string, PropertyMapping> 
                    { 
                        { 
                            "BodyText", 
                            new PropertyMapping 
                            { 
                                MapIfPropertyMatches = new KeyValuePair<string, string>("summaryText", "Test value")
                            } 
                        } 
                    });

                // Assert
                Assert.IsNull(model.BodyText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesFromParentNode()
        {
            // Arrange
            var model = new SimpleViewModel7();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content, model, new Dictionary<string, PropertyMapping> 
                { 
                    { 
                        "ParentId", 
                        new PropertyMapping 
                        { 
                            SourceProperty = "Id",
                            LevelsAbove = 1
                        } 
                    } 
                });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual(1001, model.ParentId);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesFromParentNodeUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel7WithAttribute();
            var content = new StubPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual(1001, model.ParentId);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingCustomMapping()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel8();
                var content = new StubPublishedContent();
                var mapper = GetMapper();
                mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinate);

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "geoCoordinate":
                                return "5.5,10.5,7";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.IsNotNull(model.GeoCoordinate);
                Assert.AreEqual((decimal)5.5, model.GeoCoordinate.Latitude); 
                Assert.AreEqual((decimal)10.5, model.GeoCoordinate.Longitude);                
                Assert.AreEqual(7, model.GeoCoordinate.Zoom);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingCustomMappingWithMatchingPropertyCondition()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel8();
                var content = new StubPublishedContent();
                var mapper = GetMapper();
                mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinate, "GeoCoordinate");

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "geoCoordinate":
                                return "5.5,10.5,7";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.IsNotNull(model.GeoCoordinate);
                Assert.AreEqual((decimal)5.5, model.GeoCoordinate.Latitude);
                Assert.AreEqual((decimal)10.5, model.GeoCoordinate.Longitude);
                Assert.AreEqual(7, model.GeoCoordinate.Zoom);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingCustomMappingWithNonMatchingPropertyCondition()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel8();
                var content = new StubPublishedContent();
                var mapper = GetMapper();
                mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinate, "AnotherProperty");

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "geoCoordinate":
                                return "5.5,10.5,7";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.IsNull(model.GeoCoordinate);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsOnlyNativeProperties()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, propertySet: PropertySet.Native);

                // Assert
                Assert.AreEqual(1000, model.Id);
                Assert.AreEqual("Test content", model.Name);
                Assert.IsNull(model.BodyText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsOnlyCustomProperties()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, propertySet: PropertySet.Custom);

                // Assert
                Assert.AreEqual(0, model.Id);
                Assert.IsNull(model.Name);
                Assert.AreEqual("This is the body text", model.BodyText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithStringDefaultValue()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return null;
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model, new Dictionary<string, PropertyMapping> 
                { 
                    { 
                        "BodyText", 
                        new PropertyMapping 
                        { 
                            DefaultValue = "Default body text",
                        } 
                    } 
                });

                // Assert
                Assert.AreEqual("Default body text", model.BodyText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithStringDefaultValueUsingAttribute()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3WithAttribute();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return null;
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.AreEqual("Default body text", model.BodyText);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithIntegerDefaultValue()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        return string.Empty;
                    };

                // Act
                mapper.Map(content, model, new Dictionary<string, PropertyMapping> 
                { 
                    { 
                        "NonMapped", 
                        new PropertyMapping 
                        { 
                            DefaultValue = 99,
                        } 
                    } 
                });

                // Assert
                Assert.AreEqual(99, model.NonMapped);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithIntegerDefaultValueUsingAttribute()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new SimpleViewModel3WithAttribute();
                var mapper = GetMapper();
                var content = new StubPublishedContent();

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        return string.Empty;
                    };

                // Act
                mapper.Map(content, model);

                // Assert
                Assert.AreEqual(99, model.NonMapped);
            }
        }

        #endregion

        #region Tests - Single Maps From XML

        [TestMethod]
        public void UmbracoMapper_MapFromXml_MapsPropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var xml = GetXmlForSingle();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model);

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(123456789, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
            Assert.AreEqual((decimal)12.73, model.AverageScore);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXml_MapsPropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var xml = GetXmlForSingle2();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model, new Dictionary<string, PropertyMapping> { 
                { "Name", new PropertyMapping { SourceProperty = "Name2" } },
                { "RegisteredOn", new PropertyMapping { SourceProperty = "RegistrationDate" } } 
            });

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(123456789, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXml_MapsPropertiesWithCaseInsensitiveMatchOnElementNames()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var xml = GetXmlForSingle3();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model);

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(123456789, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
            Assert.AreEqual((decimal)12.73, model.AverageScore);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXml_MapsFromChildProperty()
        {
            // Arrange
            var model = new SimpleViewModel();
            var xml = GetXmlForSingle4();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model, new Dictionary<string, PropertyMapping> { { "Name", new PropertyMapping { SourceChildProperty = "FullName" } } });

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromXml_MapsDefaultValue()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var xml = GetXmlForSingle();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model, new Dictionary<string, PropertyMapping> { { "NonMapped", new PropertyMapping { DefaultValue = "Default text" } } });

            // Assert
            Assert.AreEqual("Default text", model.NonMapped);
        }

        #endregion

        #region Tests - Single Maps From Dictionary

        [TestMethod]
        public void UmbracoMapper_MapFromDictionary_MapsPropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var dictionary = GetDictionaryForSingle();
            var mapper = GetMapper();

            // Act
            mapper.Map(dictionary, model);

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(123456789, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionary_MapsPropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var dictionary = GetDictionaryForSingle2();
            var mapper = GetMapper();

            // Act
            mapper.Map(dictionary, model, new Dictionary<string, PropertyMapping> { 
                { "Name", new PropertyMapping { SourceProperty = "Name2" } },
                { "RegisteredOn", new PropertyMapping { SourceProperty = "RegistrationDate" } } 
            });

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(123456789, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionaryWithCustomMapping_MapsPropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel8();
            var dictionary = GetDictionaryForSingle();
            var mapper = GetMapper();
            mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinateFromObject);

            // Act
            mapper.Map(dictionary, model);

            // Assert
            Assert.IsNotNull(model.GeoCoordinate);
            Assert.AreEqual((decimal)5.5, model.GeoCoordinate.Latitude);
            Assert.AreEqual((decimal)10.5, model.GeoCoordinate.Longitude);
            Assert.AreEqual(7, model.GeoCoordinate.Zoom);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromDictionary_MapsDefaultValue()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var xml = GetXmlForSingle();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model, new Dictionary<string, PropertyMapping> { { "NonMapped", new PropertyMapping { DefaultValue = "Default text" } } });

            // Assert
            Assert.AreEqual("Default text", model.NonMapped);
        }

        #endregion

        #region Tests - Single Maps From JSON

        [TestMethod]
        public void UmbracoMapper_MapFromJson_MapsPropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var json = GetJsonForSingle();
            var mapper = GetMapper();

            // Act
            mapper.Map(json, model);

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(123456789, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
            Assert.AreEqual((decimal)12.73, model.AverageScore);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromJson_MapsPropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var json = GetJsonForSingle2();
            var mapper = GetMapper();

            // Act
            mapper.Map(json, model, new Dictionary<string, PropertyMapping> { 
                { "Name", new PropertyMapping { SourceProperty = "Name2" } },
                { "RegisteredOn", new PropertyMapping { SourceProperty = "RegistrationDate" } } 
            });

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(123456789, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromJson_MapsPropertiesWithCaseInsensitiveMatchOnElementNames()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var json = GetJsonForSingle3();
            var mapper = GetMapper();

            // Act
            mapper.Map(json, model);

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
            Assert.AreEqual(21, model.Age);
            Assert.AreEqual(123456789, model.FacebookId);
            Assert.AreEqual("13-Apr-2013", model.RegisteredOn.ToString("dd-MMM-yyyy"));
            Assert.AreEqual((decimal)12.73, model.AverageScore);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromJson_MapsFromChildProperty()
        {
            // Arrange
            var model = new SimpleViewModel();
            var json = GetJsonForSingle4();
            var mapper = GetMapper();

            // Act
            mapper.Map(json, model, new Dictionary<string, PropertyMapping> { { "Name", new PropertyMapping { SourceChildProperty = "fullName" } } });

            // Assert
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("Test name", model.Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromJson_MapsDefaultValue()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var xml = GetXmlForSingle();
            var mapper = GetMapper();

            // Act
            mapper.Map(xml, model, new Dictionary<string, PropertyMapping> { { "NonMapped", new PropertyMapping { DefaultValue = "Default text" } } });

            // Assert
            Assert.AreEqual("Default text", model.NonMapped);
        }

        #endregion

        #region Tests - Collection Maps From IPublishedContent

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContentToCollection_MapsCustomPropertiesWithMatchingNames()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new List<SimpleViewModel3>();
                var mapper = GetMapper();
                var content = new List<IPublishedContent> { new StubPublishedContent(1000), new StubPublishedContent(1001) };

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "bodyText":
                                return "This is the body text";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.MapCollection(content, model);

                // Assert
                Assert.AreEqual(2, model.Count);
                Assert.AreEqual(1000, model[0].Id);
                Assert.AreEqual("This is the body text", model[0].BodyText);
                Assert.AreEqual(1001, model[1].Id);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContentToCollection_MapsUsingCustomMapping()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new List<SimpleViewModel8>();
                var content = new List<IPublishedContent> { new StubPublishedContent(1000), new StubPublishedContent(1001) };
                var mapper = GetMapper();
                mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinate);

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "geoCoordinate":
                                return "5.5,10.5,7";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.MapCollection(content, model);

                // Assert
                Assert.AreEqual(2, model.Count);
                Assert.IsNotNull(model[0].GeoCoordinate);
                Assert.AreEqual((decimal)5.5, model[0].GeoCoordinate.Latitude);
                Assert.AreEqual((decimal)10.5, model[0].GeoCoordinate.Longitude);
                Assert.AreEqual(7, model[0].GeoCoordinate.Zoom);
                Assert.IsNotNull(model[1].GeoCoordinate);
                Assert.AreEqual((decimal)5.5, model[1].GeoCoordinate.Latitude);
                Assert.AreEqual((decimal)10.5, model[1].GeoCoordinate.Longitude);
                Assert.AreEqual(7, model[1].GeoCoordinate.Zoom);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContentToCollectionWithoutParentObject_MapsUsingCustomMapping()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new List<GeoCoordinate>();
                var content = new List<IPublishedContent> { new StubPublishedContent(1000), new StubPublishedContent(1001) };
                var mapper = GetMapper();
                mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinateForCollection);

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "geoCoordinate":
                                return "5.5,10.5,7";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.MapCollection(content, model);

                // Assert
                Assert.AreEqual(2, model.Count);
                Assert.IsNotNull(model[0]);
                Assert.AreEqual((decimal)5.5, model[0].Latitude);
                Assert.AreEqual((decimal)10.5, model[0].Longitude);
                Assert.AreEqual(7, model[0].Zoom);
                Assert.IsNotNull(model[1]);
                Assert.AreEqual((decimal)5.5, model[1].Latitude);
                Assert.AreEqual((decimal)10.5, model[1].Longitude);
                Assert.AreEqual(7, model[1].Zoom);
            }
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContentToCollectionWithoutParentObject_MapsUsingCustomObjectMapping()
        {
            // Using a shim of umbraco.dll
            using (ShimsContext.Create())
            {
                // Arrange
                var model = new List<GeoCoordinate>();
                var content = new List<IPublishedContent> { new StubPublishedContent(1000), new StubPublishedContent(1001) };
                var mapper = GetMapper();
                mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinateForCollectionFromObject);

                // - shim GetPropertyValue (an extension method on IPublishedContent so can't be mocked)
                Umbraco.Web.Fakes.ShimPublishedContentExtensions.GetPropertyValueIPublishedContentStringBoolean =
                    (doc, alias, recursive) =>
                    {
                        switch (alias)
                        {
                            case "geoCoordinate":
                                return "5.5,10.5,7";
                            default:
                                return string.Empty;
                        }
                    };

                // Act
                mapper.MapCollection(content, model);

                // Assert
                Assert.AreEqual(2, model.Count);
                Assert.IsNotNull(model[0]);
                Assert.AreEqual((decimal)5.5, model[0].Latitude);
                Assert.AreEqual((decimal)10.5, model[0].Longitude);
                Assert.AreEqual(7, model[0].Zoom);
                Assert.IsNotNull(model[1]);
                Assert.AreEqual((decimal)5.5, model[1].Latitude);
                Assert.AreEqual((decimal)10.5, model[1].Longitude);
                Assert.AreEqual(7, model[1].Zoom);
            }
        }

        #endregion
        
        #region Tests - Collection Maps From XML

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
            mapper.MapCollection(xml, model.Comments, new Dictionary<string, PropertyMapping> { { "CreatedOn", new PropertyMapping { SourceProperty = "RecordedOn" } } });

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
            mapper.MapCollection(xml, model.Comments, null, "Item", false, "Identifier", "Id");

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        #endregion

        #region Tests - Collection Maps From Dictionary

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
            mapper.MapCollection(dictionary, model.Comments, new Dictionary<string, PropertyMapping> { { "CreatedOn", new PropertyMapping { SourceProperty = "RecordedOn" } } });

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
            mapper.MapCollection(dictionary, model.Comments, null, false, "Identifier", "Id");

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        #endregion

        #region Tests - Collection Maps From JSON

        [TestMethod]
        public void UmbracoMapper_MapFromJsonToCollection_MapsPropertiesForExistingEntriesWithMatchingNames()
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

            var json = GetJsonForCommentsCollection();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(json, model.Comments);

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromJsonToCollection_MapsPropertiesForExistingEntriesWithDifferentNames()
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

            var json = GetJsonForCommentsCollection2();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(json, model.Comments, new Dictionary<string, PropertyMapping> { { "CreatedOn", new PropertyMapping { SourceProperty = "RecordedOn" } } });

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromJsonToCollection_DoesntMapNonExistingItemsUnlessRequestedToDoSo()
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

            var json = GetJsonForCommentsCollection();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(json, model.Comments, null, "items", false);

            // Assert
            Assert.AreEqual(1, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromJsonToCollection_MapsAdditionalItemsWhenRequestedToDoSo()
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

            var json = GetJsonForCommentsCollection();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(json, model.Comments);

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual(2, model.Comments[1].Id);
            Assert.AreEqual("Sally Smith", model.Comments[1].Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromJsonToCollection_MapsWithDifferentLookUpPropertyNames()
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

            var json = GetJsonForCommentsCollection3();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(json, model.Comments, null, "items", false, "Identifier", "Id");

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        [TestMethod]
        public void UmbracoMapper_MapFromJsonToCollection_MapsPropertiesWithCustomRootElementName()
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

            var json = GetJsonForCommentsCollection4();
            var mapper = GetMapper();

            // Act
            mapper.MapCollection(json, model.Comments, null, "entries");

            // Assert
            Assert.AreEqual(2, model.Comments.Count);
            Assert.AreEqual("Fred Bloggs", model.Comments[0].Name);
            Assert.AreEqual("Fred's comment", model.Comments[0].Text);
            Assert.AreEqual("Sally's comment", model.Comments[1].Text);
            Assert.AreEqual("13-Apr-2013 10:30", model.Comments[1].CreatedOn.ToString("dd-MMM-yyyy HH:mm"));
        }

        #endregion

        #region Test helpers

        private static IUmbracoMapper GetMapper()
        {
            return new UmbracoMapper();
        }

        private static XElement GetXmlForSingle()
        {
            return new XElement("Item",
                new XElement("Id", 1),
                new XElement("Name", "Test name"),
                new XElement("Age", "21"),
                new XElement("FacebookId", "123456789"),
                new XElement("RegisteredOn", "2013-04-13"),
                new XElement("AverageScore", "12.73"));
        }

        private static XElement GetXmlForSingle2()
        {
            return new XElement("Item",
                new XElement("Id", 1),
                new XElement("Name2", "Test name"),
                new XElement("Age", "21"),
                new XElement("FacebookId", "123456789"),
                new XElement("RegistrationDate", "2013-04-13"));
        }

        private static XElement GetXmlForSingle3()
        {
            return new XElement("item",
                new XElement("id", 1),
                new XElement("name", "Test name"),
                new XElement("age", "21"),
                new XElement("facebookId", "123456789"),
                new XElement("registeredOn", "2013-04-13"),
                new XElement("averageScore", "12.73"));
        }

        private static XElement GetXmlForSingle4()
        {
            return new XElement("Item",
                new XElement("Id", 1),
                new XElement("Name",
                    new XElement("Title", "Mr"),
                    new XElement("FullName", "Test name")));
        }

        private static XElement GetXmlForCommentsCollection()
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

        private static XElement GetXmlForCommentsCollection2()
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

        private static XElement GetXmlForCommentsCollection3()
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

        private static XElement GetXmlForCommentsCollection4()
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

        private static Dictionary<string, object> GetDictionaryForSingle()
        {
            return new Dictionary<string, object> 
            { 
                { "Id", 1 }, 
                { "Name", "Test name" },
                { "Age", 21 },
                { "FacebookId", 123456789 },
                { "RegisteredOn", new DateTime(2013, 4, 13) },
                { "GeoCoordinate", "5.5,10.5,7" }
            };
        }

        private static Dictionary<string, object> GetDictionaryForSingle2()
        {
            return new Dictionary<string, object> 
            { 
                { "Id", 1 }, 
                { "Name2", "Test name" },
                { "Age", 21 },
                { "FacebookId", 123456789 },
                { "RegistrationDate", new DateTime(2013, 4, 13) },
            };
        }

        private static IEnumerable<Dictionary<string, object>> GetDictionaryForCommentsCollection()
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

        private static IEnumerable<Dictionary<string, object>> GetDictionaryForCommentsCollection2()
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

        private static IEnumerable<Dictionary<string, object>> GetDictionaryForCommentsCollection3()
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

        private static string GetJsonForSingle()
        {
            return @"{
                    'Id': 1,
                    'Name': 'Test name',
                    'Age': 21,
                    'FacebookId': 123456789,
                    'RegisteredOn': '2013-04-13',
                    'AverageScore': 12.73
                }";
        }

        private static string GetJsonForSingle2()
        {
            return @"{
                    'Id': 1,
                    'Name2': 'Test name',
                    'Age': 21,
                    'FacebookId': 123456789,
                    'RegistrationDate': '2013-04-13',
                }";
        }

        private static string GetJsonForSingle3()
        {
            return @"{
                    'id': 1,
                    'name': 'Test name',
                    'age': 21,
                    'facebookId': 123456789,
                    'registeredOn': '2013-04-13',
                    'averageScore': 12.73
                }";
        }

        private static string GetJsonForSingle4()
        {
            return @"{
                    'id': 1,
                    'name': {
                        'title': 'Mr',
                        'fullName': 'Test name',
                    },
                    'age': 21,
                    'facebookId': 123456789,
                    'registeredOn': '2013-04-13',
                    'averageScore': 12.73
                }";
        }

        private static string GetJsonForCommentsCollection()
        {
            return @"{ 'items': [{
                    'Id': 1,
                    'Name': 'Fred Bloggs',
                    'Text': 'Fred\'s comment',
                    'CreatedOn': '2013-04-13 09:30'
                },
                {
                    'Id': 2,
                    'Name': 'Sally Smith',
                    'Text': 'Sally\'s comment',
                    'CreatedOn': '2013-04-13 10:30'
                }]}";
        }

        private static string GetJsonForCommentsCollection2()
        {
            return @"{ 'items': [{
                    'Id': 1,
                    'Name': 'Fred Bloggs',
                    'Text': 'Fred\'s comment',
                    'RecordedOn': '2013-04-13 09:30'
                },
                {
                    'Id': 2,
                    'Name': 'Sally Smith',
                    'Text': 'Sally\'s comment',
                    'RecordedOn': '2013-04-13 10:30'
                }]}";
        }

        private static string GetJsonForCommentsCollection3()
        {
            return @"{ 'items': [{
                    'Identifier': 1,
                    'Name': 'Fred Bloggs',
                    'Text': 'Fred\'s comment',
                    'CreatedOn': '2013-04-13 09:30'
                },
                {
                    'Identifier': 2,
                    'Name': 'Sally Smith',
                    'Text': 'Sally\'s comment',
                    'CreatedOn': '2013-04-13 10:30'
                }]}";
        }

        private static string GetJsonForCommentsCollection4()
        {
            return @"{ 'entries': [{
                    'Id': 1,
                    'Name': 'Fred Bloggs',
                    'Text': 'Fred\'s comment',
                    'CreatedOn': '2013-04-13 09:30'
                },
                {
                    'Id': 2,
                    'Name': 'Sally Smith',
                    'Text': 'Sally\'s comment',
                    'CreatedOn': '2013-04-13 10:30'
                }]}";
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

        private class SimpleViewModel2WithAttribute : SimpleViewModel
        {
            [PropertyMapping(SourceProperty = "CreatorName")]
            public string Author { get; set; }
        }

        private class SimpleViewModel3 : SimpleViewModel2
        {
            public string BodyText { get; set; }

            public int NonMapped { get; set; }
        }

        private class SimpleViewModel3WithAttribute : SimpleViewModel2
        {
            [PropertyMapping(DefaultValue = "Default body text")]
            public string BodyText { get; set; }

            [PropertyMapping(DefaultValue = 99)]
            public int NonMapped { get; set; }
        }

        private class SimpleViewModel4 : SimpleViewModel2
        {
            public string BodyCopy { get; set; }
        }

        private class SimpleViewModel4WithAttribute : SimpleViewModel2WithAttribute
        {
            [PropertyMapping(SourceProperty = "bodyText")]
            public string BodyCopy { get; set; }
        }

        private class SimpleViewModel5 : SimpleViewModel2
        {
            public string HeadingAndBodyText { get; set; }

            public string SummaryText { get; set; }
        }

        private class SimpleViewModel5WithAttribute : SimpleViewModel2WithAttribute
        {
            [PropertyMapping(SourcePropertiesForConcatenation = new string[] { "Name", "bodyText" }, ConcatenationSeperator = ",")]
            public string HeadingAndBodyText { get; set; }

            [PropertyMapping(SourcePropertiesForCoalescing = new string[] { "summaryText", "bodyText" })]
            public string SummaryText { get; set; }
        }
        
        private class SimpleViewModel6 : SimpleViewModel
        {
            public byte Age { get; set; }

            public long FacebookId { get; set; }

            public decimal AverageScore { get; set; }

            public DateTime RegisteredOn { get; set; }

            public string NonMapped { get; set; }
        }

        private class SimpleViewModel7 : SimpleViewModel
        {            
            public int ParentId { get; set; }
        }

        private class SimpleViewModel7WithAttribute : SimpleViewModel
        {
            [PropertyMapping(SourceProperty = "Id", LevelsAbove = 1)]
            public int ParentId { get; set; }
        }

        private class SimpleViewModel8 : SimpleViewModel
        {
            public GeoCoordinate GeoCoordinate { get; set; }
        }

        private class SimpleViewModelWithCollection : SimpleViewModel
        {
            public SimpleViewModelWithCollection()
            {
                Comments = new List<Comment>();
            }

            public IList<Comment> Comments { get; set; }
        }

        private class Comment
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Text { get; set; }

            public DateTime CreatedOn { get; set; }
        }

        private class GeoCoordinate
        {
            public decimal Longitude { get; set; }

            public decimal Latitude { get; set; }

            public int Zoom { get; set; }
        }

        #endregion

        #region Custom mappings

        private static object MapGeoCoordinate(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propName, bool isRecursive) 
        {
            return GetGeoCoordinate(contentToMapFrom.GetPropertyValue(propName, isRecursive).ToString());
        }

        private static object MapGeoCoordinateForCollection(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propName, bool isRecursive)
        {
            return GetGeoCoordinate(contentToMapFrom.GetPropertyValue("geoCoordinate", false).ToString());
        }

        private static object MapGeoCoordinateForCollectionFromObject(IUmbracoMapper mapper, object value)
        {
            return GetGeoCoordinate(((IPublishedContent)value).GetPropertyValue("geoCoordinate", false).ToString());
        }

        private static object MapGeoCoordinateFromObject(IUmbracoMapper mapper, object value)
        {
            return GetGeoCoordinate(value.ToString());
        }   

        /// <summary>
        /// Helper to map the Google Maps data type raw value to a GeoCoordinate instance
        /// </summary>
        /// <param name="csv">Raw value in CSV format (latitude,longitude,zoom)</param>
        /// <returns>Instance of GeoCoordinate</returns>
        private static GeoCoordinate GetGeoCoordinate(string csv)
        {
            if (!string.IsNullOrEmpty(csv))
            {
                var parts = csv.Split(',');
                if (parts != null && parts.Length == 3)
                {
                    return new GeoCoordinate
                    {
                        Latitude = decimal.Parse(parts[0]),
                        Longitude = decimal.Parse(parts[1]),
                        Zoom = int.Parse(parts[2]),
                    };
                }
            }

            return null;
        }

        #endregion
    }
}
