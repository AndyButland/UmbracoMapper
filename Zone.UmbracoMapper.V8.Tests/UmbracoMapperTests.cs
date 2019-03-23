namespace Zone.UmbracoMapper.V8.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Umbraco.Core.Composing;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Zone.UmbracoMapper.Common;
    using Zone.UmbracoMapper.V8;
    using Zone.UmbracoMapper.V8.Extensions;
    using Zone.UmbracoMapper.V8.Tests.Attributes;
    using Current = Umbraco.Core.Composing.Current;

    [TestClass]
    public class UmbracoMapperTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Current.Factory = new Mock<IFactory>().Object;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Current.Reset();
        }

        #region Tests - Single Maps From IPublishedContent

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel2();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> { { "Author", new PropertyMapping { SourceProperty = "CreatorName", } } });

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
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> { { "Name", new PropertyMapping { StringValueFormatter = x => { return x.ToUpper(); }, } } });

            // Assert
            Assert.AreEqual("TEST CONTENT", model.Name);
        }
        
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesWithDifferentNamesUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel2WithAttribute();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

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
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> { { "SomeOtherProperty", new PropertyMapping { SourceProperty = "SomeOtherSourceProperty", } } });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("A.N. Editor", model.Author);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_DoesNotMapIgnoredNativePropertiesUsingDictionary()
        {
            // Arrange
            var model = new SimpleViewModel();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> { { "Name", new PropertyMapping { Ignore = true, } } });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.IsNull(model.Name);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_DoesNotMapIgnoredNativePropertiesUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel1b();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.IsNull(model.Name);
        }
        
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithMatchingNames()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("This is the body text", model.BodyText);
            Assert.AreEqual("<p>This is the body text</p>", model.BodyTextAsHtmlString.ToString());
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithDifferentNames()
        {
            // Arrange
            var model = new SimpleViewModel4();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> { { "BodyCopy", new PropertyMapping { SourceProperty = "bodyText", } } });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("This is the body text", model.BodyCopy);
        }        

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithStringFormatter()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> { { "BodyText", new PropertyMapping { StringValueFormatter = x => { return x.ToUpper(); } } } });

            // Assert
            Assert.AreEqual("THIS IS THE BODY TEXT", model.BodyText);
        }        
        
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithDifferentNamesUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel4WithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("This is the body text", model.BodyCopy);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_DoesNotMapIgnoredCustomPropertiesUsingDictionary()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> { { "BodyText", new PropertyMapping { Ignore = true, } } });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.IsNull(model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_DoesNotMapIgnoredCustomPropertiesUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel3bWithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.IsNull(model.BodyText);
        }

        /// <remarks>
        /// Failing test for bug report: 
        /// http://our.umbraco.org/projects/developer-tools/umbraco-mapper/bugs,-questions,-suggestions/60295-Property-Mapping-issue
        /// </remarks>
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithDifferentNamesUsingAttributeAndRecursiveProperty()
        {
            // Arrange
            var model = new SimpleViewModel4bWithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("This is the body text", model.BodyCopy);
        }        
        
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithConcatenation()
        {
            // Arrange
            var model = new SimpleViewModel5();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> 
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

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithConcatenationUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel5WithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("Test content,This is the body text", model.HeadingAndBodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithCoalescingAndFirstItemAvailable()
        {
            // Arrange
            var model = new SimpleViewModel5();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> 
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

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithCoalescingAndFirstItemAvailableUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel5WithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("This is the summary text", model.SummaryText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithCoalescingAndFirstItemNotAvailable()
        {
            // Arrange
            var model = new SimpleViewModel5();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> 
                { 
                    { 
                        "SummaryText", 
                        new PropertyMapping 
                        { 
                            SourcePropertiesForCoalescing = new string[] { "emptyText", "bodyText" }, 
                        } 
                    } 
                });

            // Assert
            Assert.AreEqual("This is the body text", model.SummaryText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithCoalescingAndFirstItemNotAvailableUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel5bWithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("This is the body text", model.SummaryText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithMatchingCondition()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> 
                { 
                    { 
                        "BodyText", 
                        new PropertyMapping 
                        { 
                            MapIfPropertyMatches = new KeyValuePair<string, string>("testText", "Test value")
                        } 
                    } 
                });

            // Assert
            Assert.AreEqual("This is the body text", model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsCustomPropertiesWithNonMatchingCondition()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> 
                { 
                    { 
                        "BodyText", 
                        new PropertyMapping 
                        { 
                            MapIfPropertyMatches = new KeyValuePair<string, string>("summaryText", "Another value")
                        } 
                    } 
                });

            // Assert
            Assert.IsNull(model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesFromParentNode()
        {
            // Arrange
            var model = new SimpleViewModel7();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> 
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
        public void UmbracoMapper_MapFromIPublishedContent_DoesNotMapWhenLevelRequestedDoesNotExist()
        {
            // Arrange
            var model = new SimpleViewModel7();
            var content = MockPublishedContent(mockParent: false);
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping>
                {
                    {
                        "ParentId",
                        new PropertyMapping
                        {
                            SourceProperty = "Id",
                            LevelsAbove = 2
                        }
                    }
                });

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual(0, model.ParentId);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNativePropertiesFromParentNodeUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel7WithAttribute();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual(1001, model.ParentId);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNullParent()
        {
            // Arrange
            var model = new SimpleViewModel7WithAttribute();
            var content = MockPublishedContent(mockParent: false);
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual(0, model.ParentId);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingCustomMapping()
        {
            // Arrange
            var model = new SimpleViewModel8();
            var content = MockPublishedContent();
            var mapper = GetMapper();
            mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinate);

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.IsNotNull(model.GeoCoordinate);
            Assert.AreEqual((decimal)5.5, model.GeoCoordinate.Latitude); 
            Assert.AreEqual((decimal)10.5, model.GeoCoordinate.Longitude);                
            Assert.AreEqual(7, model.GeoCoordinate.Zoom);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingCustomMappingWithMatchingPropertyCondition()
        {
            // Arrange
            var model = new SimpleViewModel8();
            var content = MockPublishedContent();
            var mapper = GetMapper();
            mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinate, "GeoCoordinate");

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.IsNotNull(model.GeoCoordinate);
            Assert.AreEqual((decimal)5.5, model.GeoCoordinate.Latitude);
            Assert.AreEqual((decimal)10.5, model.GeoCoordinate.Longitude);
            Assert.AreEqual(7, model.GeoCoordinate.Zoom);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingCustomMappingWithNonMatchingPropertyCondition()
        {
            // Arrange
            var model = new SimpleViewModel8();
            var content = MockPublishedContent();
            var mapper = GetMapper();
            mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinate, "AnotherProperty");

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.IsNull(model.GeoCoordinate);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingCustomMappingWhenProvidedViaDictionary()
        {
            // Arrange
            var model = new SimpleViewModel8();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping>
                {
                    {
                        "GeoCoordinate",
                        new PropertyMapping
                        {
                            CustomMapping = MapGeoCoordinate,
                        }
                    }
                });

            // Assert
            Assert.IsNotNull(model.GeoCoordinate);
            Assert.AreEqual((decimal)5.5, model.GeoCoordinate.Latitude);
            Assert.AreEqual((decimal)10.5, model.GeoCoordinate.Longitude);
            Assert.AreEqual(7, model.GeoCoordinate.Zoom);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingCustomMappingWhenProvidedViaAttribute()
        {
            // Arrange
            var model = new SimpleViewModel8a();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.IsNotNull(model.GeoCoordinate);
            Assert.AreEqual((decimal)5.5, model.GeoCoordinate.Latitude);
            Assert.AreEqual((decimal)10.5, model.GeoCoordinate.Longitude);
            Assert.AreEqual(7, model.GeoCoordinate.Zoom);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingCustomMappingOverridingExistingMapping()
        {
            // Arrange
            var model = new SimpleViewModel8();
            var content = MockPublishedContent();
            var mapper = GetMapper();
            mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinate);

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping>
                {
                    {
                        "GeoCoordinate",
                        new PropertyMapping
                        {
                            CustomMapping = MapInversedGeoCoordinate,
                        }
                    }
                });

            // Assert
            Assert.IsNotNull(model.GeoCoordinate);
            Assert.AreEqual((decimal)-5.5, model.GeoCoordinate.Latitude);
            Assert.AreEqual((decimal)-10.5, model.GeoCoordinate.Longitude);
            Assert.AreEqual(7, model.GeoCoordinate.Zoom);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingMapFromAttribute()
        {
            // Arrange
            var model = new SimpleViewModel9();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("Child item", model.Child.Name);
        }

        // Test for issue #13
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingMapFromAttributeAndRespectsConcatenationParameters()
        {
            // Arrange
            SimpleMapFromForContatenateStringAttribute.ResetCallCount();
            var model = new SimpleViewModel9a();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("Test 1,Test 2", model.Test);
        }

        // Test for issue #13
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingMapFromAttributeAndRespectsCoalesceParameter()
        {
            // Arrange
            SimpleMapFromForContatenateStringAttribute.ResetCallCount();
            var model = new SimpleViewModel9b();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("Test 2", model.Test);
        }

        // Test for issue #13
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsUsingMapFromAttributeAndRespectsCoalesceParameter2()
        {
            // Arrange
            SimpleMapFromForContatenateStringAttribute.ResetCallCount();
            var model = new SimpleViewModel9c();
            var content = MockPublishedContent();
            var mapper = GetMapper();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("This is the summary text", model.Test);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsOnlyNativeProperties()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, propertySet: PropertySet.Native);

            // Assert
            Assert.AreEqual(1000, model.Id);
            Assert.AreEqual("Test content", model.Name);
            Assert.IsNull(model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsOnlyCustomProperties()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, propertySet: PropertySet.Custom);

            // Assert
            Assert.AreEqual(0, model.Id);
            Assert.IsNull(model.Name);
            Assert.AreEqual("This is the body text", model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithStringDefaultValue()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent(bodyTextValue: null);

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> 
            { 
                { 
                    "BodyText", 
                    new PropertyMapping 
                    { 
                        DefaultValue = "Default body text",
                    } 
                },
                { 
                    "BodyText2", 
                    new PropertyMapping 
                    { 
                        DefaultValue = "Default body text 2",
                    } 
                } 
            });

            // Assert
            Assert.AreEqual("Default body text", model.BodyText);
            Assert.AreEqual("Default body text 2", model.BodyText2);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithStringDefaultValueUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel3WithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent(bodyTextValue: null);

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("Default body text", model.BodyText);
            Assert.AreEqual("Default body text 2", model.BodyText2);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithIntegerDefaultValue()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping> 
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

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithIntegerDefaultValueUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel3WithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual(99, model.NonMapped);
        }

        /// <remarks>
        /// Failing test for issue: 
        /// https://github.com/AndyButland/UmbracoMapper/issues/10
        /// </remarks>
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_DoesNotApplyIntegerDefaultValueWhenZeroMapped()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping>
            {
                {
                    "MappedFromZero",
                    new PropertyMapping
                    {
                        SourceProperty = "zeroInt",
                        DefaultValue = 1,
                    }
                }
            });

            // Assert
            Assert.AreEqual(0, model.MappedFromZero);
        }

        /// <remarks>
        /// Failing test for issue: 
        /// https://github.com/AndyButland/UmbracoMapper/issues/10
        /// </remarks>
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_DoesNotApplyBooleanDefaultValueWhenFalseMapped()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, new Dictionary<string, PropertyMapping>
            {
                {
                    "MappedFromFalse",
                    new PropertyMapping
                    {
                        SourceProperty = "falseBool",
                        DefaultValue = true,
                    }
                }
            });

            // Assert
            Assert.IsFalse(model.MappedFromFalse);
        }

        /// <remarks>
        /// Failing test for issue: 
        /// https://github.com/AndyButland/UmbracoMapper/issues/1
        /// </remarks>
        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsNullToNullableDateTimeWithNoValue()
        {
            // Arrange
            var model = new SimpleViewModel4bWithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.IsFalse(model.DateTime.HasValue);
        }          

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_AutomapsRelatedIPublishedContent()
        {
            // Arrange
            var model = new SimpleViewModel3WithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("This is the body text", model.BodyText);
            Assert.AreEqual("This is the sub-heading", model.SubModelValue.SubHeading);
            Assert.AreEqual(2, model.SubModelValues.Count);
            Assert.AreEqual("This is the sub-heading", model.SubModelValues.First().SubHeading);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithCustomPropertyValueGetter()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model, 
                new Dictionary<string, PropertyMapping> { { "BodyText", new PropertyMapping { PropertyValueGetter = typeof(SuffixAddingPropertyValueGetter), } } });

            // Assert
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("This is the body text...", model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithCustomPropertyValueGetterUsingAttribute()
        {
            // Arrange
            var model = new SimpleViewModel3cWithAttribute();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("This is the body text...", model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithCustomDefaultPropertyValueGetterProvidedInConstructor()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper(new SuffixAddingPropertyValueGetter());
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("This is the body text...", model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithCustomDefaultPropertyValueGetterProvidedInProperty()
        {
            // Arrange
            var model = new SimpleViewModel3();
            var mapper = GetMapper();
            ((UmbracoMapper)mapper).DefaultPropertyValueGetter = new SuffixAddingPropertyValueGetter();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model);

            // Assert
            Assert.AreEqual("Test content", model.Name);
            Assert.AreEqual("This is the body text...", model.BodyText);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContent_MapsWithCustomPropertyValueGetterReturningComplexType()
        {
            // Arrange
            var model = new SimpleViewModel8();
            var mapper = GetMapper();
            var content = MockPublishedContent();

            // Act
            mapper.Map(content.Object, model,
                new Dictionary<string, PropertyMapping> { { "GeoCoordinate", new PropertyMapping { PropertyValueGetter = typeof(ComplexTypeReturningPropertyValueGetter), } } });

            // Assert
            Assert.AreEqual("Test content", model.Name);
            Assert.IsNotNull(model.GeoCoordinate);
            Assert.AreEqual(1.9M, model.GeoCoordinate.Latitude);
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
            Assert.IsTrue(model.IsMember);
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

        /// <remarks>
        /// Failing test for bug report: (Zone, PCUK, Codebase: #267)
        /// </remarks>
        [TestMethod]
        public void UmbracoMapper_MapFromDictionary_MapsNullToStringWithoutError()
        {
            // Arrange
            var model = new SimpleViewModel6();
            var dictionary = GetDictionaryForSingle();
            var mapper = GetMapper();

            // Act
            mapper.Map(dictionary, model);

            // Assert
            Assert.IsNull(model.TwitterUserName);
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
            // Arrange
            var model = new List<SimpleViewModel3>();
            var mapper = GetMapper();
            var content = new List<IPublishedContent> { MockPublishedContent().Object, MockPublishedContent(id: 1001).Object };

            // Act
            mapper.MapCollection(content, model);

            // Assert
            Assert.AreEqual(2, model.Count);
            Assert.AreEqual(1000, model[0].Id);
            Assert.AreEqual("This is the body text", model[0].BodyText);
            Assert.AreEqual(1001, model[1].Id);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContentToCollection_MapsUsingCustomMapping()
        {
            // Arrange
            var model = new List<SimpleViewModel8>();
            var content = new List<IPublishedContent> { MockPublishedContent().Object, MockPublishedContent(id: 1001).Object };
            var mapper = GetMapper();
            mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinate);

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

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContentToCollectionWithoutParentObject_MapsUsingCustomMapping()
        {
            // Arrange
            var model = new List<GeoCoordinate>();
            var content = new List<IPublishedContent> { MockPublishedContent().Object, MockPublishedContent(id: 1001).Object };
            var mapper = GetMapper();
            mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinateForCollection);

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

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContentToCollectionWithoutParentObject_MapsUsingCustomObjectMapping()
        {
            // Arrange
            var model = new List<GeoCoordinate>();
            var content = new List<IPublishedContent> { MockPublishedContent().Object, MockPublishedContent(id: 1001).Object };
            var mapper = GetMapper();
            mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, MapGeoCoordinateForCollectionFromObject);

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

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContentToCollection_WithClearCollection_ClearsCollectionBeforeMapping()
        {
            // Arrange
            var model = new List<SimpleViewModel3>();
            var mapper = GetMapper();
            var content = new List<IPublishedContent> { MockPublishedContent().Object, MockPublishedContent(id: 1001).Object };

            // Act
            mapper.MapCollection(content, model);
            mapper.MapCollection(content, model);

            // Assert
            Assert.AreEqual(2, model.Count);
        }

        [TestMethod]
        public void UmbracoMapper_MapFromIPublishedContentToCollection_WithoutClearCollection_DoesNotClearCollectionBeforeMapping()
        {
            // Arrange
            var model = new List<SimpleViewModel3>();
            var mapper = GetMapper();
            var content = new List<IPublishedContent> { MockPublishedContent().Object, MockPublishedContent(id: 1001).Object };

            // Act
            mapper.MapCollection(content, model);
            mapper.MapCollection(content, model, clearCollectionBeforeMapping: false);

            // Assert
            Assert.AreEqual(4, model.Count);
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

        private static IUmbracoMapper GetMapper(IPropertyValueGetter propertyValueGetter = null)
        {
            return propertyValueGetter == null ? new UmbracoMapper() : new UmbracoMapper(propertyValueGetter);
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
                { "GeoCoordinate", "5.5,10.5,7" },
                { "IsMember", "1" },
                { "TwitterUserName", null }
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

        #region Custom mappings

        internal static object MapGeoCoordinate(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propName, bool isRecursive) 
        {
            return GetGeoCoordinate(contentToMapFrom.Value(propName, fallback: isRecursive.ToRecuriveFallback()).ToString());
        }

        private static object MapInversedGeoCoordinate(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propName, bool isRecursive)
        {
            var value = GetGeoCoordinate(contentToMapFrom.Value(propName, fallback: isRecursive.ToRecuriveFallback()).ToString());
            value.Latitude = -value.Latitude;
            value.Longitude = -value.Longitude;
            return value;
        }

        private static object MapGeoCoordinateForCollection(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propName, bool isRecursive)
        {
            return GetGeoCoordinate(contentToMapFrom.Value("geoCoordinate").ToString());
        }

        private static object MapGeoCoordinateForCollectionFromObject(IUmbracoMapper mapper, object value)
        {
            return GetGeoCoordinate(((IPublishedContent)value).Value("geoCoordinate").ToString());
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
                if (parts.Length == 3)
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

        #region Mocks

        private Mock<IPublishedContent> MockPublishedContent(int id = 1000, 
            string bodyTextValue = "This is the body text",
            bool recursiveCall = false,
            bool mockParent = true)
        {
            var summaryTextPropertyMock = new Mock<IPublishedProperty>();
            summaryTextPropertyMock.Setup(c => c.Alias).Returns("summaryText");
            summaryTextPropertyMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            summaryTextPropertyMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns("This is the summary text");

            var emptyTextPropertyMock = new Mock<IPublishedProperty>();
            emptyTextPropertyMock.Setup(c => c.Alias).Returns("emptyText");
            emptyTextPropertyMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            emptyTextPropertyMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(null);

            var bodyTextPropertyMock = new Mock<IPublishedProperty>();
            bodyTextPropertyMock.Setup(c => c.Alias).Returns("bodyText");
            bodyTextPropertyMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            bodyTextPropertyMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(bodyTextValue);

            var bodyText2PropertyMock = new Mock<IPublishedProperty>();
            bodyText2PropertyMock.Setup(c => c.Alias).Returns("bodyText2");
            bodyText2PropertyMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            bodyText2PropertyMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(string.Empty);

            var bodyTextAsHtmlStringPropertyMock = new Mock<IPublishedProperty>();
            bodyTextAsHtmlStringPropertyMock.Setup(c => c.Alias).Returns("bodyTextAsHtmlString");
            bodyTextAsHtmlStringPropertyMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            bodyTextAsHtmlStringPropertyMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns("<p>This is the body text</p>");

            var testTextPropertyMock = new Mock<IPublishedProperty>();
            testTextPropertyMock.Setup(c => c.Alias).Returns("testText");
            testTextPropertyMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            testTextPropertyMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns("Test value");

            var geoCoordinatePropertyMock = new Mock<IPublishedProperty>();
            geoCoordinatePropertyMock.Setup(c => c.Alias).Returns("geoCoordinate");
            geoCoordinatePropertyMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            geoCoordinatePropertyMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns("5.5,10.5,7");

            var childContentMock = new Mock<IPublishedProperty>();
            childContentMock.Setup(c => c.Alias).Returns("child");
            childContentMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            childContentMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(1001);

            var nonMappedContentMock = new Mock<IPublishedProperty>();
            nonMappedContentMock.Setup(c => c.Alias).Returns("nonMapped");
            nonMappedContentMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            nonMappedContentMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(string.Empty);

            var zeroIntContentMock = new Mock<IPublishedProperty>();
            zeroIntContentMock.Setup(c => c.Alias).Returns("zeroInt");
            zeroIntContentMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            zeroIntContentMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(0);

            var falseBoolContentMock = new Mock<IPublishedProperty>();
            falseBoolContentMock.Setup(c => c.Alias).Returns("falseBool");
            falseBoolContentMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            falseBoolContentMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var dateTimeContentMock = new Mock<IPublishedProperty>();
            dateTimeContentMock.Setup(c => c.Alias).Returns("dateTime");
            dateTimeContentMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            dateTimeContentMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns("1/1/0001 12:00:00 AM");

            var subHeadingContentMock = new Mock<IPublishedProperty>();
            subHeadingContentMock.Setup(c => c.Alias).Returns("subHeading");
            subHeadingContentMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            subHeadingContentMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns("This is the sub-heading");

            var subModelValueContentMock = new Mock<IPublishedProperty>();
            var subModelValuesContentMock = new Mock<IPublishedProperty>();
            if (!recursiveCall)
            {
                subModelValueContentMock.Setup(c => c.Alias).Returns("subModelValue");
                subModelValueContentMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
                subModelValueContentMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(MockPublishedContent(recursiveCall: true).Object);

                subModelValuesContentMock.Setup(c => c.Alias).Returns("subModelValue");
                subModelValuesContentMock.Setup(c => c.HasValue(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
                subModelValuesContentMock.Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(new List<IPublishedContent> {MockPublishedContent(recursiveCall: true).Object, MockPublishedContent(recursiveCall: true).Object});
            }

            var parentContentMock = new Mock<IPublishedContent>();
            parentContentMock.Setup(c => c.Id).Returns(1001);

            var contentMock = new Mock<IPublishedContent>();
            contentMock.Setup(c => c.Id).Returns(id);
            if (mockParent)
            {
                contentMock.Setup(c => c.Parent).Returns(parentContentMock.Object);
            }
            else
            {
                contentMock.Setup(c => c.Parent).Returns((IPublishedContent)null);
            }
            contentMock.Setup(c => c.Name).Returns("Test content");
            contentMock.Setup(c => c.CreatorName).Returns("A.N. Editor");
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "summaryText"))).Returns(summaryTextPropertyMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "emptyText"))).Returns(emptyTextPropertyMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "bodyText"))).Returns(bodyTextPropertyMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "bodyText2"))).Returns(bodyText2PropertyMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "bodyTextAsHtmlString"))).Returns(bodyTextAsHtmlStringPropertyMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "testText"))).Returns(testTextPropertyMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "geoCoordinate"))).Returns(geoCoordinatePropertyMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "child"))).Returns(childContentMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "nonMapped"))).Returns(nonMappedContentMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "zeroInt"))).Returns(zeroIntContentMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "falseBool"))).Returns(falseBoolContentMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "dateTime"))).Returns(dateTimeContentMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "subHeading"))).Returns(subHeadingContentMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "subModelValue"))).Returns(subModelValueContentMock.Object);
            contentMock.Setup(c => c.GetProperty(It.Is<string>(x => x == "subModelValues"))).Returns(subModelValuesContentMock.Object);

            return contentMock;
        }

        #endregion
    }
}
