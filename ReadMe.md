#Umbraco Mapper

## Background

Umbraco Mapper has been developed to support a more pure MVC approach to building Umbraco applications.

With MVC in Umbraco, there are broadly three approaches:

1. As with 'traditional' Umbraco using WebForms, place a lot of data access and business logic in the views
2. Within the view used for the page template, place an @Html.Action or @Html.Partial, to call other templates or controller actions.
3. Hijack the routes, and map the Umbraco (and other) content information to custom view models which are passed to the view.

Some may find 3) requires a bit more work up front, but the advantage of this approach is that the view code is exceptionally clean, and in fact need have no dependency on Umbraco at all.  It also follows a more traditional MVC route, where the controller is responsible for passing the information to the view in the particular form it requires it.

This work up front though to map the content from Umbraco and other sources is where Umbraco Mapper comes in.

## Summary of Operations

Umbraco Mapper provides an interface **IUmbracoMapper** and implementation **UmbracoMapper** that provides a number of methods for mapping various types of content to a custom view model.  Methods are chainable so a number of mapping operations can be requested using a fluent interface type syntax.

The operations supported are:

- Mapping of an Umbraco **IPublishedContent** to a custom view model.
- Mapping of a **collection of IPublishedContent** items - e.g. from a node query picker - to a custom view model's collection.
- Mapping of **XML** to a custom view model or collection.  This might be from a particular Umbraco data type (e.g. Related Links) or from a custom source of XML.
- Mapping of **JSON** to a custom view model or collection.  This might be from a particular Umbraco data type or from a custom source of JSON.
- Mapping of a **Dictionary** to a custom view model or collection.  

Conventions are used throughout the mapping process.  For example it's expected when mapping document type properties to custom view model fields that they will have the same name, with the former camel-cased (e.g. 'bodyText' will map to 'BodyText').  All methods contain an optional parameter though where these conventions can be overridden.

A couple of base types are provided to help create the necessary view models in your application.  It's not essential to use these, but they may provide some useful properties.

- **BaseNodeViewModel** - can be used as the base type for your view models, containing fields matching for standard IPublishedContent properties like Id, Name, DocumentTypeAlias and Url.
- **MediaFile** - a representation for an Umbraco media file, with properties including Url, Width, Height etc.

It is also possible to define custom mapping functions for property types not included in the default mapper. This could be used, for example, if you have custom Media types with properties not included in the default MediaFile class, or for mapping complex data types like the Google Maps type.

## Package Contents

The package has been provided as two separate downloads:

- **Zone.UmbracoMapper.dll** - this is the core mapping package.  It has no dependencies other than on .Net and the core Umbraco binaries (6.1.6).
- **Zone.UmbracoMapper.DampCustomMapping.dll** - is an add-on package with another dll that provides a custom mapping for [DAMP](http://our.umbraco.org/projects/backoffice-extensions/digibiz-advanced-media-picker "DAMP") models to media files, and is only required if you want to use that functionality.  It has a dependency on **DAMP Property Editor Value Converter v1.2** and the Umbraco Mapper itself.

## Expected Use Cases

Where we utilise and test Umbraco Mapper we are following the model of Umbraco MVC application development that uses [route hijacking](http://our.umbraco.org/documentation/Reference/Mvc/custom-controllers "Hijacking Umbraco Routes within Umbraco Documentation").

We also implement an IoC container such as Ninject as [described here](http://ismailmayat.wordpress.com/2013/07/25/umbraco-6-mvc-and-dependency-injection/ "Using Ninject with Umbraco").  

Neither of these steps are strictly necessary though, if preferred you can just instantiate an instance of UmbracoMapper directly.

## Examples

### Mapping Operations

#### From IPublishedContent

Given an instance of UmbracoMapper you can map a the properties of a particular page to a custom view model using conventions like this:

    var mapper = new UmbracoMapper();
    var model = new UberDocTypeViewModel();
    mapper.Map(CurrentPage, model);
	
This will map all view model properties it can find an exact name match for, either from the standard IPublishedContent properties (like Id, Name etc.), or from the document type fields.

To override conventions for property mapping, you can provide a Dictionary of property mappings.  In this example we are mapping a document type field called 'bodyText' to a view model field called 'Copy':

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
	  { 
		{ 
		  "Copy", new PropertyMapping 
		    { 
			  SourceProperty = "bodyText", 
		    } 
		}, 
	  });

To map a collection use the following method.  This example maps the child nodes of the current page to a custom collection called 'Comments' on the view model, again using the default name mapping conventions.

    mapper.MapToCollection(CurrentPage.Children, model.Comments);
	
You can also override here both the property names as before, and the level at which the mapping is made.  So if for example you have one property on your view model that you want to get from the parent node, you can do this:

    mapper.MapCollection(CurrentPage.Children, model.Comments, 
      new Dictionary<string, PropertyMapping> 
	  { 
		{ 
		  "ParentPage", new PropertyMapping 
		    { 
			  SourceProperty = "Name",
			  LevelsAbove = 1,
		    } 
		}, 
	  });	
	  
You can also map any other collection of IPublishedContent, e.g. that built up from a node picker and query:

    var countryIds = CurrentPage.GetPropertyValue<string>("countries");
    var countryNodes = Umbraco.TypedContent(countryIds.Split(','));
    mapper.MapCollection(countryNodes, model.Countries);
	
Or if you are also using the [Umbraco Core Property Editor Converters](http://our.umbraco.org/projects/developer-tools/umbraco-core-property-editor-converters), more simply like this:

    var countryNodes = CurrentPage.GetPropertyValue<IEnumerable<IPublishedContent>>("countries");
    mapper.MapCollection(countryNodes, model.Countries);	  
	  
Another PropertyMapping field allows you to map properties from related content.  Say for example your document type contains a content picker - the value of this will be an integer representing the Id of another IPublished content instance (or the IPublishedContent itself if you have the [Umbraco Core Property Editor Converters](http://our.umbraco.org/projects/developer-tools/umbraco-core-property-editor-converters) installed).  You can provide an override here to tell the mapper to map from a particular property of that instance instead.  

The following example maps a string property on the view model called 'LinkToPage' to the 'Url' property of an IPublishedContent picked using a content picker for the current page.

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
	  { 
		{ 
		  "LinkToPage", new PropertyMapping 
		    { 
			  SourceRelatedProperty = "Url", 
		    } 
		}, 
	  });
	  
Yet another couple of PropertyMapping fields allow you to concatenate two or more source properties to a single string property on your view model.  You pass through an array of properties to map to and a separation string like this.  The following example would map the firstName ("Fred") and lastName ("Bloggs") properties to a single concatenated string ("Bloggs, Fred"):

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
	  { 
		{ 
		  "Name", new PropertyMapping 
		    { 
				SourcePropertiesForConcatenation = new string[] { "firstName", "lastName" },
                ConcatenationSeperator = ", ",
		    } 
		}, 
	  });
	  
The MapIfPropertyMatches field allows you to define a condition for when the mapping operation occurs.  In this example, we want to map a string containing a URL to a related page, only if the page is intended to be linked to:

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
	  {
		{
			"LinkToPage", new PropertyMapping
				{
					SourceProperty = "relatedPage",
					SourceRelatedProperty = "Url",
					MapIfPropertyMatches = new KeyValuePair<string, string>("allowPageLink", "1"),
				}
		},	  	  
	  });	  
 	  
#### From Other Sources	  

Some Umbraco data types store XML.  This can be mapped to a custom collection on the view model.  The example below uses the related links data type.  Note the need to provide an override here to ensure the correct root node is passed to the mapping method.

    var sr = new StringReader(CurrentPage.GetPropertyValue<string>("relatedLinks"));
    var relatedLinksXml = XElement.Load(sr);
    mapper.MapCollection(relatedLinksXml, model.RelatedLinks, null, "link")

You can also map XML, JSON and Dictionaries that may have come from other sources.  This example maps a JSON array:

    var json = @"{ 'items': [{ 'Name': 'United Kingdom' }, { 'Name': 'Italy' }]}";
    mapper.MapCollection(json, model.Countries);
	
Similar to the use of the PropertyMapping.SourceRelatedProperty property for IPublished content, you can pass an override to map to the immediate child of the XML or JSON, thus allowing you to flatten your view model.  The property is called SourceChildProperty.

#### Chaining Mapping Operations	  

All mapping methods return an instance of the mapper itself, meaning operations can be chained in a fluent interface style.  E.g.

    mapper.Map(CurrentPage, model)
          .MapToCollection(CurrentPage.Children, model.Comments);
		  
#### Further Examples		  
		  
For more examples, including details of how the controllers are set up, see the controller class **UberDocTypeController.cs** in the test web application.  Or the file **UmbracoMapperTests.cs** in the unit test project. 		  
		  
### Custom Mappings		  

For additional flexibility when you want to map to a custom view model type that's been created in your project, it is possible to add custom mapping functions to the mapper, e.g.:

	mapper.AddCustomMapping(typeof(Image).FullName, CustomMappings.GetImage);
    ...
    public static object GetImage(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propName, bool isRecursive) {}
	
The add-on package provides this method, **DampMapper.MapMediaFile**, for the purposes of mapping from the DAMP model to the standard MediaFile class provided with the mapper. If you want to use this in your project you'll just need to add it to the custom mappings like this:

	mapper.AddCustomMapping(typeof(MediaFile).FullName, DampMapper.MapMediaFile);
	
Here's another example, this time mapping from the [Google Maps data type](http://our.umbraco.org/projects/backoffice-extensions/google-maps-datatype) (which stores it's data as three values - lat, long, zoom - in CSV format):

    mapper.AddCustomMapping(typeof(GeoCoordinate).FullName, CustomMappings.MapGeoCoordinate);
	...
    public class GeoCoordinate
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public int Zoom { get; set; }
    }	
	...
	public class CustomMappings
    {
        public static object MapGeoCoordinate(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propName, bool isRecursive) 
        {
            return GetGeoCoordinate(contentToMapFrom.GetPropertyValue<string>(propName, isRecursive, null));
        }

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
    }	

## Classes, Properties and Methods

### IUmbracoMapper / UmbracoMapper

The primary mapping component.

#### Properties

**AssetsRootUrl** (string) - If set allows the population of mapped MediaFile's **DomainWithUrl** property with an absolute URL.  Useful only in the context where a CDN is used for distributing media files rather than them being served from the web server via relative links.

#### Methods

Full signature of mapping methods are as follows:

	IUmbracoMapper AddCustomMapping(string propertyTypeFullName,
		Func<IUmbracoMapper, IPublishedContent, string, object> mapperFunction);

    IUmbracoMapper Map<T>(IPublishedContent content, 
        T model, 
        Dictionary<string, PropertyMapping> propertyNameMappings = null,
        string[] recursiveProperties = null);

    IUmbracoMapper Map<T>(XElement xml, 
        T model,
        Dictionary<string, PropertyMapping> propertyNameMappings = null);

    IUmbracoMapper Map<T>(Dictionary<string, object> dictionary,
        T model,
        Dictionary<string, PropertyMapping> propertyNameMappings = null);

    IUmbracoMapper Map<T>(string json,
        T model,
        Dictionary<string, PropertyMapping> propertyNameMappings = null);

    IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection, 
        IList<T> modelCollection,
        Dictionary<string, PropertyMapping> propertyNameMappings = null,
        string[] recursiveProperties = null) where T : new();

    IUmbracoMapper MapCollection<T>(XElement xml, IList<T> modelCollection, 
        Dictionary<string, PropertyMapping> propertyNameMappings = null, 
        string groupElementName = "Item", 
        bool createItemsIfNotAlreadyInList = true, 
        string sourceIdentifyingPropName = "Id", 
        string destIdentifyingPropName = "Id") where T : new();

    IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries, 
        IList<T> modelCollection, 
        Dictionary<string, PropertyMapping> propertyNameMappings = null, 
        bool createItemsIfNotAlreadyInList = true, 
        string sourceIdentifyingPropName = "Id", 
        string destIdentifyingPropName = "Id") where T : new();

    IUmbracoMapper MapCollection<T>(string json, IList<T> modelCollection, 
        Dictionary<string, PropertyMapping> propertyNameMappings = null,
        string rootElementName = "items", 
        bool createItemsIfNotAlreadyInList = true, 
        string sourceIdentifyingPropName = "Id", 
        string destIdentifyingPropName = "Id") where T : new();		
		
### PropertyMapping

Class defining the override to the mapping convention for property to a particular type.

#### Properties

**SourceProperty** (string) - The name of the property on the source to map from.  If not passed, exact name match convention is used.

**LevelsAbove** (int) - Defines the number of levels above the current content to map the value from.  If not passed, 0 (the current level) is assumed.  Only for IPublishedContent mappings.

**SourceRelatedProperty** (string) - If passed, the source property is assumed to be a structure that has related content (e.g. a Content Picker that contains an integer Id for another IPublishedContent).  The mapping is then done from the named property of that child element. Only for IPublishedContent mappings.

**SourceChildProperty** (string) - If passed, the source property is assumed to be a structure that has child content.  The mapping is then done from the named field of that child element. Only for XML and JSON mappings.

**SourcePropertiesForConcatenation** (string[]) - This property can contain a string array of multiple source properties to map from.  If the destination property is a string the results will be concatenated.

**SourceChildProperty** (string) - Used in conjunction with SourcePropertiesForConcatenation to define the separating string between the concatenated items.

**MapIfPropertyMatches** (KeyValuePair<string, string>) - if provided, mapping is only carried out if the property provided in the key contains the value provided in the value.

### BaseNodeViewModel

Class representing an Umbraco node that can be used as the basis of any page view models in the client product.

### MediaFile

Class representing an Umbraco media item that can be used within page view models in the client product.
		
## Version History

- 1.0.2 - First public release
- 1.0.3
    - Made mapping to strings more flexible so source does not itself have to be a string.  Instead ToString() is called on whatever you are mapping to the string.
	- Add the propertyLevels optional parameter for when mapping from IPublished content.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.
- 1.1.0
	- Breaking change to interface of property mapping overrides, as refactored to use single dictionary with a complex **PropertyMapping** custom type
- 1.1.1
    - Improved null checking on mapping single items.
- 1.1.2
    - Added ability to define your own custom mappings for particular types.
- 1.2.0
    - Amended the custom mapping to support recursive properties (though in fact this won't work until 6.2, due to [this issue](http://issues.umbraco.org/issue/U4-1958 "Umbraco issue u4-1958"))
- 1.3.0 
	- Refactored the solution to remove the dependency on DAMP for the core mapper.  A second project and dll is provided that contains the mapping to DAMP models, and the client must now link this up if they want to use it via the custom mappings.
- 1.3.1
	- Added Level property to BaseNodeViewModel
- 1.3.2
	- Added SourceChildProperty to PropertyMapping class, to allow mapping to XML and JSON child elements 
	- Added SourceRelatedProperty to PropertyMapping class, to allow mapping to related IPublishedContent selected via a content picker 	
- 1.3.3
    - Amended related property mapping to handle case where the [Umbraco Core Property Editor Converters](http://our.umbraco.org/projects/developer-tools/umbraco-core-property-editor-converters) are installed, and we'll get back an actual IPublishedContent from related content instead of just the node Id
- 1.3.4
	- Added support for string concatenating two or more source properties to a single destination one
- 1.3.5
	- Further fix to related property mapping to handle case where Umbraco Core Property Editor Converters are installed and a multi-node tree picker is used
- 1.3.6
	- Added MapIfPropertyMatches to PropertyMapping
	
## Credits

Thanks to Ali Taheri and Neil Cumpstey at [Zone](http://www.thisiszone.com) for code, reviews and testing.
