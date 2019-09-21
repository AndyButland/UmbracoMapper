# Umbraco Mapper

## Background

Umbraco Mapper has been developed to support a more pure MVC approach to building Umbraco applications.

It supports Umbraco versions 7 and 8.

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
- Mapping of a **collection of IPublishedContent**  (or, for v8, a collection of nested content **IPublishedElement**) items - e.g. from a node query picker - to a custom view model's collection.
- Mapping of **XML** to a custom view model or collection.  This might be from a particular Umbraco data type (e.g. Related Links) or from a custom source of XML.
- Mapping of **JSON** to a custom view model or collection.  This might be from a particular Umbraco data type or from a custom source of JSON.
- Mapping of a **Dictionary** to a custom view model or collection.  

Conventions are used throughout the mapping process.  For example it's expected when mapping document type properties to custom view model fields that they will have the same name, with the former camel-cased (e.g. 'bodyText' will map to 'BodyText').  All methods contain an optional parameter though where these conventions can be overridden.

A couple of base types are provided to help create the necessary view models in your application.  It's not essential to use these, but they may provide some useful properties.

- **BaseNodeViewModel** - can be used as the base type for your view models, containing fields matching for standard IPublishedContent properties like Id, Name, DocumentTypeAlias and Url.
- **MediaFile** - a representation for an Umbraco media file, with properties including Url, Width, Height etc.  If single or multiple media pickers are defined on a document type and instances of `MediaFile` or `IEnumerable<MediaFile>` are defined on the view model, they will be automatically mapped.

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

From version 4.0 onwards - supporting Umbraco 8 - you can optionally provide a culture string, in order to map view model properties to the language variant indicated by the culture string:

    mapper.Map(CurrentPage, model, "en-GB");

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
      
The **propertySet** is a simple enum based flag to allow to you map just all native properties of IPublishedContent (Id, Name etc.) OR just all custom properties (all the ones put on document types).  The default if not provided is to map both. 

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
    
Or if you are also using the [Umbraco Core Property Editor Converters](https://our.umbraco.org/projects/developer-tools/umbraco-core-property-value-converters), more simply like this:

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

**MapRecursively** can be added to a field and if set to true, the property will be mapped recursively.  It will use Umbraco default camel-case naming convention (i.e. if assigned to a view model property called 'StarRating', it'll look for an Umbraco property called 'starRating').

**FallbackMethods** has been added from version 3 onwards, supporting the more flexible fall-back methods provided in version 8.  If provided, this will override any setting provided on **MapRecursively**.

To fall-back recursively, you would provide:

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
      { 
        { 
          "BodyText", new PropertyMapping 
            { 
              FallbackMethods = new List<int> { 2 }, 
            } 
        }, 
      });
	  
Via fall-back language:

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
      { 
        { 
          "BodyText", new PropertyMapping 
            { 
              FallbackMethods = new List<int> { 3 }, 
            } 
        }, 
      });
	  
And via language first, then recurively: 

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
      { 
        { 
          "BodyText", new PropertyMapping 
            { 
              FallbackMethods = new List<int> { 3, 2 }, 
            } 
        }, 
      });
	  
The "magic numbers" can (and should) be replaced by those defined in Umbraco 8's `Umbraco.Core.Models.PublishedContent.Fallback` struct, and the are also defined in the package, at `UmbracoMapper.Common.Constants, e.g. 

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping> 
      { 
        { 
          "BodyText", new PropertyMapping 
            { 
              FallbackMethods = Fallback.ToLanguage.ToArray() 
            } 
        }, 
      });


**MapFromPreValue** will map a single-value property from a prevalue (for example a radio button list).  Without this the prevalue's numeric Id will be mapped; by setting to true, the label from the prevalue will be mapped instead (normally more useful).  In Umbraco V8 this setting has no effect as the prevalue labels are mapped automatically.

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
    
To map recursively up the ancestors of the tree, use:

    [PropertyMapping(MapRecursively = true)]
    public int StarRating { get; set; }
	
Or - to support the more flexible fall-back methods provided via version 8:

    [PropertyMapping(FallbackMethods = new[] { Fallback.Ancestors })]
    public int StarRating { get; set; }
    
#### "Auto-mapping" related content

Version 1.5.0 introduced a new feature that would auto-map related and ancestor content to avoid having to explicitly make secondary mapping calls.  It works when you have a view model that itself contains a property that is a complex type - i.e. an instance of a class with one or more properties - or a collection of complex types.

If in the standard mapping operation that property is mapped to a single or multiple content/node picker or an instance of nested content, AND you are either running V8 or have the [Umbraco Core Property Editor Converters](https://our.umbraco.org/projects/developer-tools/umbraco-core-property-value-converters) installed (required so we get back an IPublishedContent or IEnumerable<IPublishedContent>), OR you use the **LevelsAbove** property mapping attribute field to indicate that the mapping should be made from a parent node, Umbraco Mapper will automatically make further mapping operations for that related or ancestor content to the complex type on your view model.

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
	
> When working with Umbraco 8 (Umbraco Mapper package version 4.0 and above), the first signature is slightly different, as we have support of more flexible fall-back methods (such as via language variant, in addition to recursive retrieval up the content free) and we can map from IPublishedElement (e.g. as used with Nested Content):

    delegate object CustomMapping(IUmbracoMapper mapper, IPublishedElement content, string propertyName, Fallback fallback);

> For V8, if you need to access properties available on IPublishedContent, but not IPublishedElement, you can cast to the former like this:

    var publishedContent = contentToMapFrom as IPublishedContent;
    if (publishedContent != null)
    {
        // Can now use e.g. publishedContent.Name;
    }
    
Here'an example, mapping from the [Google Maps data type](http://our.umbraco.org/projects/backoffice-extensions/google-maps-datatype) (which stores it's data as three values - lat, long, zoom - in CSV format):

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

For the finest control, it's also possible to provided a custom mapping method via the optional Dictionary object provided for a mapping operation.  If this is done, the provided mapping will be used in preference to any globally registerd custom mapping method that would otherwise match the type and/or property name.

    mapper.Map(CurrentPage, model,
      new Dictionary<string, PropertyMapping>
      {
          {
            "GeoCoordinate", new PropertyMapping
                {
                    CustomMapping = CustomMappings.MapGeoCoordinate
                }
          },
      });
      
This can also be achieved using an attribute on the view mode, which, if in place, means the Dictionary object doesn't need to be passed:

    [PropertyMapping(
        CustomMappingType = typeof(CustomMappings), 
        CustomMappingMethod = nameof(CustomMappings.MapGeoCoordinate))]
    public GeoCoordinate GeoCoordinate { get; set; }
    
    mapper.Map(CurrentPage, model);


### Using IMapFromAttribute

Custom mappings allow you to define one way to map a given complex type on your view model.  But there's another, even more generic approach you can take from version 1.6.0 thanks to a pull request from [Robin Herd](https://github.com/21robin12).  

To explain with an example, suppose we have two document types: Article Page and Author. An article has two content pickers: one for the author of the article, and another for a related article. We want to map all the properties from those content items onto the view model for our article page.

View models look like this:

    public class ArticlePageViewModel : BaseNodeViewModel
    {
        [MapFromContentPicker]
        public ArticlePageViewModel RelatedArticle { get; set; }

        [MapFromContentPicker]
        public AuthorModel Author { get; set; }

        public IHtmlString ArticleText { get; set; }
    }
    
    public class AuthorModel : BaseNodeViewModel
    {
        public string Title { get; set; }

        public string PhoneNumber { get; set; }
    }
    
We can implement IMapFromAttribute as follows:

    [AttributeUsage(AttributeTargets.Property)]
    public class MapFromContentPickerAttribute : Attribute, IMapFromAttribute
    {
        public void SetPropertyValue<T>(object fromObject, PropertyInfo property, T model, IUmbracoMapper mapper)
        {
            var method = GetType().GetMethod("GetInstance", BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = method.MakeGenericMethod(property.PropertyType);
            var item = genericMethod.Invoke(this, new[] { fromObject, mapper });
            property.SetValue(model, item);
        }

        private T GetInstance<T>(object fromObject, IUmbracoMapper mapper)
            where T : class
        {
            T instance = default(T);
            if (fromObject != null)
            {
                // Check first if already IPublishedContent (as core converters installed)
                var content = fromObject as IPublishedContent;
                if (content == null)
                {
                    // Otherwise handle if Id passed
                    int id;
                    if (int.TryParse(fromObject.ToString(), out id))
                    {
                        var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
                        content = umbracoHelper.TypedContent(id);
                    }
                }

                if (content != null)
                {
                    instance = Activator.CreateInstance<T>();
                    mapper.Map(content, instance);
                }

            }

            return instance;
        }
    }
    
...and we can apply this attribute to any property of any type which we wish to map in the same way.

Just to reiterate the difference between these two methods: custom mappings define a mapping for a particular C# type; IMapFromAttribute implementations define a mapping for a particular Umbraco data type and are reusable across multiple C# property types.

### Working with Vorto

[Vorto](https://our.umbraco.org/projects/backoffice-extensions/vorto/) is an Umbraco package that supports 1:1 translations in Umbraco.  It works by wrapping standard Umbraco data types and you retrieve values for IPublishedContent using those types not with `GetPropertyValue` but with a custom extension method `GetVortoValue`.

As of version 2.0.3 we've provided a way of replacing the default Umbraco method of retrieving property values that Umbraco Mapper uses with custom one, that can use this Vorto specific method.  Of course this technique can also be used for any other property editor that has a similar requirement to amend how the raw content values from Umbraco are retrieved.

If you have just a few properties on your view model that you wish to use a custom method for, you can decorate your properties like this:

    [PropertyMapping(PropertyValueGetter = typeof(MyPropertyValueGetter))]
    public string MyProperty { get; set; }
    
On the other hand, if you wanted to use this method for all mapping operations, you can set the `DefaultPropertyValueGetter` property on your `UmbracoMapper` instance itself: 

    mapper.DefaultPropertyValueGetter = new MyPropertyValueGetter();

Or provide the value in the overloaded constructor:

    var mapper = new UmbracoMapper(new MyPropertyValueGetter());
    
The type you use here must implement `IPropertyValueGetter`.  Here's an example we've used for working with Vorto, that falls back to the standard means of retrieving property values if the particular field is not a Vorto model:

    public class VortoPropertyGetter : IPropertyValueGetter
    {
        public object GetPropertyValue(IPublishedContent content, string alias, bool recursive)
        {
            if (content.HasVortoValue(alias))
            {
                return content.GetVortoValue(alias, recursive: recursive);
            }

            return content.GetPropertyValue(alias, recursive);
        }
    }
	
If using this technique when working with a version of the package supporting Umbraco 8, the signature is slightly different:

    object GetPropertyValue(IPublishedContent content, string alias, string culture, string segment, Fallback fallback)

## Classes, Properties and Methods

### IUmbracoMapper / UmbracoMapper

The primary mapping component.

#### Properties

**AssetsRootUrl** (string) - If set allows the population of mapped MediaFile's **DomainWithUrl** property with an absolute URL.  Useful only in the context where a CDN is used for distributing media files rather than them being served from the web server via relative links.

**DefaultPropertyValueGetter** (IPropertyValueGetter) - If set uses the provided type for retrieving property values from Umbraco, instead of the default which uses standard Umbraco GetPropertyValue() calls

#### Methods

Full signature of delegates are as follows:

    object CustomMapping(IUmbracoMapper mapper,
                         IPublishedContent content,
                         string propertyName,
                         bool recursive)
						 
And for versions targetting Umbraco 8:

    object CustomMapping(IUmbracoMapper mapper,
                         IPublishedElement content,
                         string propertyName,
                         Fallback fallback)			

Full signature of mapping methods are as follows:

    IUmbracoMapper AddCustomMapping(string propertyTypeFullName,
                                    CustomMapping mapping,
                                    string propertyName = null);

    IUmbracoMapper Map<T>(IPublishedContent content, 
        T model, 
		string culture, 	// versions supporting Umbraco 8 only
        Dictionary<string, PropertyMapping> propertyMappings = null,
        PropertySet propertySet = PropertySet.All);

    IUmbracoMapper Map<T>(IPublishedElement content, 
        T model, 
		string culture, 	// versions supporting Umbraco 8 only
        Dictionary<string, PropertyMapping> propertyMappings = null,
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
		string culture, 	// versions supporting Umbraco 8 only
        Dictionary<string, PropertyMapping> propertyMappings = null,
        PropertySet propertySet = PropertySet.All, 
        bool clearCollectionBeforeMapping = true) where T : new();

    IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedElement> contentCollection, 
        IList<T> modelCollection,
		string culture, 	// versions supporting Umbraco 8 only
        Dictionary<string, PropertyMapping> propertyMappings = null,
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

**PropertyValueGetter** (Type) - a type that must implement `IPropertyValueGetter` to be used when retrieving the property value from Umbraco.  A use case for this is to use Vorto, where we want to call GetVortoValue instead of GetPropertyValue.

**CustomMappingType** (Type) and **CustomMappingMethod** (string) - a type that must implement be of delegate CustomMapping, and a method to be used in preference to any named or unnamed custom mapping that might be registered globally.

### BaseNodeViewModel

Class representing an Umbraco node that can be used as the basis of any page view models in the client product.

### MediaFile

Class representing an Umbraco media item that can be used within page view models in the client product.

## Supported Umbraco Versions

The earliest version of Umbraco this package has been tested with is `6.1.6` although the expectation is that all versions from `6.0` should be supported.

The last version of the package strictly supporting out of the box Umbraco version 6 is `1.6.1`.

After that the references to Umbraco Core were updated to version 7.  The package still works though with Umbraco 6, so long as the JSON.Net dependency is updated to the version that ships with Umbraco 7, `6.0.8`.  This can be done with a NuGet command:

    PM> Install-Package Newtonsoft.Json -Version: 6.0.8
    
With that dependency updated Umbraco 6 *appears to me* to work unaffected, which is borne to some extent out by [other discussion](https://our.umbraco.org/forum/developers/api-questions/57394-Is-the-latest-version-of-JsonNET-compatible-with-Umbraco-616).
    
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
- 1.6.0
    - Added support for mapping generically from given Umbraco data types via IMapFromAttribute attribute.  Thanks to [Robin Herd](https://github.com/21robin12) for the PR
- 1.6.1
    - Added support for mapping single and multiple media to view model instances of MediaFile (using a custom mapping, but one that the mapper defines itself)
- 2.0.0
    - Upgraded Umbraco reference to version 7 allowing for unit testing without use of MS Fakes
    - Use on Umbraco 6 now requires an update to the JSON.Net dependency (see "Supported Umbraco Versions" above)
- 2.0.1
    - Fully removed dependency on MS Fakes
    - Handled case where trying to map parent of home page (issue #4) - thanks [richarth](https://github.com/richarth)
- 2.0.2
    - Added mapping attributes to `MediaFile`
- 2.0.3
    - Allowed the default method of retrieving property values from Umbraco to be overriden at the component or view model field level, supporting use with Vorto
- 2.0.4
    - Fixed issue when mapping to level above the current node, where the level is too high for the current position in the content tree - thanks [richarth](https://github.com/richarth)
- 2.0.5
    - Fixed issue with default mapping not being applied when primitive type default value is mapped (issue #10)
- 2.0.6
    - Handled case where IPropertyValueGetter is in use and returns a complex type - as reported and solution provided by [Olie here](https://our.umbraco.com/projects/developer-tools/umbraco-mapper/bugs-questions-suggestions/92608-setting-complex-model-properties-using-custom-ipropertyvaluegetter)
- 2.0.7
    - Added the option to provide a CustomMapping for a single property via the customisation dictionary
- 2.0.8
    - Implemented the above via the `PropertyMapping` attribute
- 2.0.9
    - Added support for the use of string concatenation and coalescing when using IMapFromAttribute
- 3.0.0
    - Bumped major version following internal refactoring into common project to support code re-use across Umbraco versions 7 and 8.
	  Considered a breaking change due to:
        - Removal of the `recuriveProperties` string array parameter (not needed/consistent as can use attribute or property mapping dictionary).
		- Change of the passing of a property specific custom mapping to a type and method rather than a `CustomMapping` instance (aligns with use on attribute, and improves internal code re-use as version specific Umbraco dependency is removed).
		- Addition of the FallBackMethods property mapping override and attribute.
        - Namespace changes.
- 3.0.1
    - Allowed the direct mapping of a List collection type to an IEnumerable rather than requiring the view model use a List too
- 3.0.2
    - Minor change to picked media mapper to resolve conflict between v6 and v7 method signature.  From this version on v6 is no longer supported.
- 3.0.3
	- Added MapFromPreValue attribute property to allow mapping from a prevalue label to a view model property.
	- Fixed error in mapping options passed from attribute to dictionary when both are in use.
- 4.0.0
    - Support of Umbraco version 8 (read more at this [blog post](https://web-matters.blogspot.com/2019/03/umbraco-mapper-new-releases-supporting-v8.html)
    - Changed mapping signature to allow the passing of a culture string, such that properties are mapping using the language variant indicated by the culture code.	
	- Added support for fallback methods using language as well as recursive calls to ancestors in the content tree
- 4.0.1
    - Allowed the direct mapping of a List collection type to an IEnumerable rather than requiring the view model use a List too
- 4.1.0
    - Upgraded dependencies for version targetting Umbraco V8 to use 8.1
- 4.2.0
    - Added support for mapping from nested content's IPublishedElement
- 4.2.1
	- Fixed error in mapping options passed from attribute to dictionary when both are in use.
	
## Credits

Thanks to Ali Taheri, Neil Cumpstey and Robin Herd at [Zone](http://www.zonedigital.com) for code, reviews and testing. 

## License

Copyright &copy; 2016-19 Andy Butland, Zone and [other contributors](https://github.com/AndyButland/UmbracoMapper/graphs/contributors)

Licensed under the [MIT License](License.md)
