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

To override conventions for property mapping, you can provide a Dictionary of property mappings via the **propertyMappings** parameter (or with more recent versions, use attributes - see 'Mapping Using Attributes' below).  In this example we are mapping a document type field called 'bodyText' to a view model field called 'Copy':

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
	  
The **recursiveProperties** parameter allows you to pass a string array of Umbraco property aliases that should be mapped recursively (i.e. if not found on the current page, look on the parent, and the parent's parent, and so on until it is found).  See below for an attribute based alternative to using this parameter.

The **propertySet**	is a simple enum based flag to allow to you map just all native properties of IPublishedContent (Id, Name etc.) OR just all custom properties (all the ones put on document types).  The default if not provided is to map both. 

##### From Collections of IPublishedContent

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

#### Further Property Mapping Overrides	
	  
Another PropertyMapping field allows you to map properties from **related content**.  Say for example your document type contains a content picker - the value of this will be an integer representing the Id of another IPublished content instance (or the IPublishedContent itself if you have the [Umbraco Core Property Editor Converters](https://our.umbraco.org/projects/developer-tools/umbraco-core-property-value-converters) installed).  You can provide an override here to tell the mapper to map from a particular property of that instance instead.  

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
	  
Yet another couple of PropertyMapping fields allow you to **concatenate** two or more source properties to a single string property on your view model.  You pass through an array of properties to map to and a separation string like this.  The following example would map the firstName ("Fred") and lastName ("Bloggs") properties to a single concatenated string ("Bloggs, Fred"):

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
	  
Similarly you can **coalesce** (take the first non null, empty or whitespace) source property from a list:	

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
	  { 
		{ 
		  "Title", new PropertyMapping 
		    { 
				SourcePropertiesForCoalescing = new string[] { "heading", "Name" },
		    } 
		}, 
	  });  
	  
The **MapIfPropertyMatches** field allows you to define a condition for when the mapping operation occurs.  In this example, we want to map a string containing a URL to a related page, only if the page is intended to be linked to:

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
	  
The **StringValueFormatter** is a field that can be set to a function to transform the mapped value.  For example, you could use this to format a date field to a string with a particular date format.  This simple example shows how to apply a transformation function that converts the mapped value to upper case:

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
	  {
		{
			"Heading", new PropertyMapping 
				{ 
					StringValueFormatter = x => {
						return x.ToUpper();
					}
				} 
		},	  	  
	  });	
	  
**DefaultValue** is a field that if set will provide a value to any properties that aren't mapped.  It works by checking the value of the property after the mapping operation is complete.  If it's null or the default value for the type (e.g. 0 for an integer), and a default value has been provided, the property value will be set to this default.

**Ignore** can be added to a field and if set to true, it will not be mapped and retain it's default or previously set value

**DictionaryKey** can be added with a string value of a dictionary key, which will be mapped to it's value

#### Mapping Using Attributes

A newer feature that has been added to the package is the ability to configure your mappings using attributes on the view model, instead of passing in these overrides to the default mapping behaviour via the Dictionary parameter of the Map() method.		  

There's a single attribute called **PropertyMapping** that has properties that can be set to configure most of the property mappings described above.

So for example instead of configuring a mapping like this:

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
	  
You can do this on your view model:

	[PropertyMapping(SourceProperty = "Name", LevelsAbove = 1)]
	public string ParentPage { get; set; }	

Simplifying the mapping call to:

    mapper.MapCollection(CurrentPage.Children, model.Comments);	  
	
In addition to the use of this attribute to replace the Dictionary parameter, it can also be used instead of the recursiveProperties string array that is used to pass a list of Umbraco document type aliases that should be mapped recursively.  It will use Umbraco default camel-case naming convention (i.e. if assigned to a view model property called 'StarRating', it'll look for an Umbraco property called 'starRating').

It's used like this:

	[PropertyMapping(MapRecursively = true)]
	public int StarRating { get; set; }
	
#### "Auto-mapping" related content

Version 1.5.0 introduced a new feature that would auto-map related and ancestor content to avoid having to explicitly make secondary mapping calls.  It works when you have a view model that itself contains a property that is a complex type - i.e. an instance of a class with one or more properties - or a collection of complex types.

If in the standard mapping operation that property is mapped to a single or multiple content/node picker AND you have the [Umbraco Core Property Editor Converters](https://our.umbraco.org/projects/developer-tools/umbraco-core-property-value-converters) installed (required so we get back an IPublishedContent or IEnumerable<IPublishedContent>), OR you use the **LevelsAbove** property mapping attribute field to indicate that the mapping should be made from a parent node, Umbraco Mapper will automatically make further mapping operations for that related or ancestor content to the complex type on your view model.

To take an example illustrating all three types of auto-mapping (single related content, multiple related content and parent content), say you have a view model that looks like this:

    public class NewsLandingPageViewModel
    {
        public NewsLandingPageViewModel()
        {
			NewsCategory = new Category();
            TopStory = new NewsStory();
            OtherStories = new List<NewsStory>();
        }

        public string Heading { get; set; }
		
		[PropertyMapping(LevelsAbove = 1)]
		public Category NewsCategory { get; set; }

        public NewsStory TopStory { get; set; }

        public IEnumerable<NewsStory> OtherStories { get; set; }
		
		public class Category
		{
			public string Title { get; set; }		
		}		
		
		public class NewsStory
		{
			public string Headline { get; set; }		
			
			public DateTime StoryDate { get; set; }	

			public IHtmlString BodyText { get; set; }			
		}
	}	
	
And you were mapping from a content node based on document type that contained the following properties:

 - A text string with an alias of **heading**
 - A single item content picker with an alias of **topStory**
 - A multiple item content picker with an alias of **otherStories**
 
And that those picker fields were selecting fields of a document type containing:

 - A text string with an alias of **headline**
 - A date picker content picker with an alias of **storyDate**
 - A rich text editor an alias of **bodyText**
 
And that the content node had a parent that indicated the news category and was based on a document type containing:

 - A text string with an alias of **title**
 
You could map the whole lot with a single call to:

    var model = new NewsLandingPageViewModel();
    mapper.Map(CurrentPage, model);
	
> Note: there's a small backward compatibility issue introduced with this feature.  Given mapping related content previously required a second call to a mapping operation, if those calls are in place those related fields will be mapped twice - once by the explicit call and once by the auto-mapping.  In the case of mapping to a collection you would end up in that case with twice as many values in the collection as you'd expect, with each one repeated.  The explicit call can of course now be removed which would resolve this issue.  

> If that wasn't done though, to avoid the unwanted doubling behaviour, any call to map a collection has been set by default to clear the destination collection before carrying out the mapping.  In most cases that's likely what is required.  However if you have a case where you do want a collection to be left intact before mapping - perhaps it having been part-populated from another source - you can set the newly introduced optional parameter **clearCollectionBeforeMapping** to false.
 
#### From Other Sources	  

Some Umbraco 6 data types store XML and Umbraco 7 ones JSON.  This can be mapped to a custom collection on the view model.  The example below uses the related links data type from version 6.  Note the need to provide an override here to ensure the correct root node is passed to the mapping method.

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
	
The custom mapping method you pass must be a delegate that matches one of the following two signatures.

Firstly for mapping from IPublishedContent:

    delegate object CustomMapping(IUmbracoMapper mapper, IPublishedContent content, string propertyName, bool recursive);

And secondly when mapping from a dictionary object value:

	delegate object CustomObjectMapping(IUmbracoMapper mapper, object value);
	
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
        public static object MapGeoCoordinate(IUmbracoMapper mapper, IPublishedContent contentToMapFrom, string propertyName, bool recursive) 
        {
            return GetGeoCoordinate(contentToMapFrom.GetPropertyValue<string>(propertyName, recursive, null));
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

A custom mapping method can be restricted to a single property, rather than all properties of the given type, by passing the **propertyName** parameter to the **AddCustomMapping** method.

## Classes, Properties and Methods

### IUmbracoMapper / UmbracoMapper

The primary mapping component.

#### Properties

**AssetsRootUrl** (string) - If set allows the population of mapped MediaFile's **DomainWithUrl** property with an absolute URL.  Useful only in the context where a CDN is used for distributing media files rather than them being served from the web server via relative links.

#### Methods

Full signature of delegates are as follows:

    object CustomMapping(IUmbracoMapper mapper,
                         IPublishedContent content,
                         string propertyName,
                         bool recursive)

Full signature of mapping methods are as follows:

	IUmbracoMapper AddCustomMapping(string propertyTypeFullName,
                                    CustomMapping mapping,
                                    string propertyName = null);

    IUmbracoMapper Map<T>(IPublishedContent content, 
        T model, 
        Dictionary<string, PropertyMapping> propertyMappings = null,
        string[] recursiveProperties = null,
		PropertySet propertySet = PropertySet.All);

    IUmbracoMapper Map<T>(XElement xml, 
        T model,
        Dictionary<string, PropertyMapping> propertyMappings = null);

    IUmbracoMapper Map<T>(Dictionary<string, object> dictionary,
        T model,
        Dictionary<string, PropertyMapping> propertyMappings = null);

    IUmbracoMapper Map<T>(string json,
        T model,
        Dictionary<string, PropertyMapping> propertyMappings = null);

    IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection, 
        IList<T> modelCollection,
        Dictionary<string, PropertyMapping> propertyMappings = null,
        string[] recursiveProperties = null,
		PropertySet propertySet = PropertySet.All, 
		bool clearCollectionBeforeMapping = true) where T : new();

    IUmbracoMapper MapCollection<T>(XElement xml, IList<T> modelCollection, 
        Dictionary<string, PropertyMapping> propertyMappings = null, 
        string groupElementName = "Item", 
        bool createItemsIfNotAlreadyInList = true, 
        string sourceIdentifyingPropName = "Id", 
        string destIdentifyingPropName = "Id") where T : new();

    IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries, 
        IList<T> modelCollection, 
        Dictionary<string, PropertyMapping> propertyMappings = null, 
        bool createItemsIfNotAlreadyInList = true, 
        string sourceIdentifyingPropName = "Id", 
        string destIdentifyingPropName = "Id") where T : new();

    IUmbracoMapper MapCollection<T>(string json, IList<T> modelCollection, 
        Dictionary<string, PropertyMapping> propertyMappings = null,
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

**SourcePropertiesForConcatenation** (string\[\]) - This property can contain a string array of multiple source properties to map from.  If the destination property is a string the results will be concatenated.

**ConcatenationSeperator** (string) - Used in conjunction with SourcePropertiesForConcatenation to define the separating string between the concatenated items.

**SourcePropertiesForCoalescing** (string\[\]) - This property can contain a string array of multiple source properties to map from.  If the destination property is a string the result will be the first non null, empty or whitespace property found..

**MapIfPropertyMatches** (KeyValuePair<string, string>) - if provided, mapping is only carried out if the property provided in the key contains the value provided in the value.

**StringValueFormatter** (Func<string, string>) - If provided, carries out the formatting transformation provided in the function on the mapped value.

**DefaultValue** (object) - If provided, sets a default value for a property to be used if the mapped value cannot be found.

**Ignore** (bool) - can be added to a field and if set to true, it will not be mapped and retain it's default or previously set value

**DictionaryKey** (string) - if set property will be mapped from the given Umbraco dictionary key

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
- 1.4.0
    - Refactored the custom mapping function into a declared delegate
    - Added optional property name when adding a custom mapping
    - Restricted the generic type parameter on all mapping functions to be a class
- 1.4.1
    - Added support for string coalescing two or more source properties to a single destination one
- 1.4.2
	- Added support for nullable properties
- 1.4.3
	- Fixed bug with coalescing of property values
- 1.4.4
	- Added option to map only custom or only native properties
- 1.4.5
	- Added ability to provide a StringValueFormatter function to a property mapping, to transform the mapped value
- 1.4.6
	- Added ability to configure mappings using attributes
- 1.4.7
	- Added support for Archetype mapping of picked items, by checking for instances of IPublishedContent or IEnumerable<IPublishedContent> when mapping dictionary based collections
- 1.4.8
	- Added support for custom mappings on dictionary mapping operations
- 1.4.9
	- Added support for mapping [object to object when mapping collections](http://our.umbraco.org/projects/developer-tools/umbraco-mapper/bugs,-questions,-suggestions/56541-Mapping-collection-custom-mapper)
	- Added DefaultValue to property mapping
- 1.4.10
	- Further additions to support for mapping object to object added 1.4.9
- 1.4.11
	- Handled special case conversion of string "1" (from Archetype) to boolean true
- 1.4.12
	- Fixed issue with [use of recursive and source property attributes on same property](http://our.umbraco.org/projects/developer-tools/umbraco-mapper/bugs,-questions,-suggestions/60295-Property-Mapping-issue)
- 1.4.13
	- Added Ignore property to property mapping [to allow for the omission of some fields from mapping](http://our.umbraco.org/projects/developer-tools/umbraco-mapper/bugs,-questions,-suggestions/60525-PropertySets)
- 1.4.14
	- Fixed DefaultValue property mapping [to correctly handle empty strings](http://our.umbraco.org/projects/developer-tools/umbraco-mapper/bugs,-questions,-suggestions/60708-Property-Mapping-DefaultValue)
- 1.4.15
	- Performance improvements
- 1.4.16
	- Added dependency details to NuGet package
- 1.4.17
	- Fixed bug found with mapping null values from dictionaries to strings
- 1.4.18
	- Set mapping of empty date values to nullable DateTime to null rather than DateTime.MinValue
- 1.5.0
	- Implemented the "auto-mapping" feature for related content
- 1.5.1
	- Added support for mapping from dictionary values
- 1.5.2
	- Fixed issue introduced in 1.5.0 that prevented mapping of properties defined as IHtmlString
	
## Credits

Thanks to Ali Taheri and Neil Cumpstey at [Zone](http://www.thisiszone.com) for code, reviews and testing.

## License

Copyright &copy; 2015 Andy Butland, Zone and [other contributors](https://github.com/AndyButland/UmbracoMapper/graphs/contributors)

Licensed under the [MIT License](License.md)