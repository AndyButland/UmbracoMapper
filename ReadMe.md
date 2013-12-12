#Umbraco Mapper

## Background

Umbraco Mapper has been developed to support a more pure MVC approach to building Umbraco applications.

With MVC in Umbraco, there are broadly three approaches:

1. As with 'traditional' Umbraco using WebForms, place a lot of presentation logic in the Views
2. Within the view used for the page template, place an @Html.Action or @Html.Partial, to call other templates or controller actions.
3. Hijack the routes, and map the Umbraco (and other) content information to custom view models which are passed to the view.

Some may find 3) requires a bit more work up front, but the advantage of this approach is that the view code is exceptionally clean, and in fact has no dependency on Umbraco at all.  It also follows a more traditional MVC route, where the controller is responsible for passing the information to the view in the particular form is requires it.

This work up front though to map the content from Umbraco and other sources is where Umbraco Mapper comes in.

## Summary of Operations

Umbraco Mapper provides an interface **IUmbracoMapper** and implementation **UmbracoMapper** that provides a number of methods for mapping various types of content to a custom view model.  Methods are chainable so a number of mapping operations can be requested using a fluent interface type syntax.

The operations supported are:

* Mapping of an Umbraco **IPublishedContent** to a custom view model.
* Mapping of a **collection of IPublishedContent** items - e.g. from a node query picker - to a custom view model's collection.
* Mapping of **XML** to a custom view model or collection.  This might be from a particular Umbraco data type (e.g. Related Links) or from a custom source of XML.
* Mapping of **JSON** to a custom view model or collection.  This might be from a particular Umbraco data type or from a custom source of JSON.
* Mapping of a **Dictionary** to a custom view model or collection.  

Conventions are used throughout.  For example it's expected when mapping document type properties to custom view model fields that they will have the same name, with the former camel-cased (e.g. 'bodyText' will map to 'BodyText').  All methods contain an optional parameter though where these conventions can be overridden.

## Prerequisites

To utilise Umbraco Mapper you should be following the model of Umbraco MVC application development that uses [route hijacking](http://our.umbraco.org/documentation/Reference/Mvc/custom-controllers "Hijacking Umbraco Routes within Umbraco Documentation").

You also may want to implement an IoC container such as Ninject as [described here](http://ismailmayat.wordpress.com/2013/07/25/umbraco-6-mvc-and-dependency-injection/ "Using Ninject with Umbraco").  Though this isn't necessary, if preferred you can just instantiate an instance of UmbracoMapper directly.

## Examples

Given an instance of UmbracoMapper you can map a the properties of a particular page to a custom view model using conventions like this:

    var mapper = new UmbracoMapper();
    var model = new UberDocTypeViewModel();
    mapper.Map(CurrentPage, model);

To override conventions for property mapping, you can provide a Dictionary of property mappings.  In this example we are mapping a document type field called 'bodyText' to a view model field called 'Copy':

    mapper.Map(CurrentPage, model,
      new Dictionary<string, string> { { "Copy", "bodyText" }, });

To map a collection use the following method.  This example maps the child nodes of the current page to a custom collection called 'Comments' on the view model.

    mapper.MapToCollection(CurrentPage.Children, model.Comments);

You can also map any other collection if IPublishedContent, e.g. that built up from a node picker and query:

    var countryIds = CurrentPage.GetPropertyValue<string>("countries");
    var countryNodes = Umbraco.TypedContent(countryIds.Split(','));
    mapper.MapCollection(countryNodes, model.Countries);

Some Umbraco data types store XML.  This can be mapped to a custom collection on the view model.  The example below uses the related links data type.  Note the need to provide an override here to ensure the correct root node is passed to the mapping method.

    var sr = new StringReader(CurrentPage.GetPropertyValue<string>("relatedLinks"));
    var relatedLinksXml = XElement.Load(sr);
    mapper.MapCollection(relatedLinksXml, model.RelatedLinks, null, "link")

You can also map XML, JSON and Dictionaries that may have come from other sources.  This example maps a JSON array:

    var json = @"{ 'items': [{ 'Name': 'United Kingdom' }, { 'Name': 'Italy' }]}";
    mapper.MapCollection(json, model.Countries);

All mapping methods return an instance of the mapper itself, meaning operations can be chained in a fluent interface style.  E.g.

For more examples, including details of how the controllers are set up, see the controller class **UberDocTypeController.cs** in the test web application.  Or the file **UmbracoMapperTests.cs** in the unit test project. 

    mapper.Map(CurrentPage, model)
          .MapToCollection(CurrentPage.Children, model.Comments);

## Mapping to Media Files

A representation of a media file is provided with Umbraco Mapper within the class **MediaFile**.  This can be used as a property on your custom view model.

An automated mapping is provided for media properties using [DAMP](http://our.umbraco.org/projects/backoffice-extensions/digibiz-advanced-media-picker "DAMP").  This means if you have a property on your view model of type MediaFile and a document type property with a DAMP database, the media information will be automatically mapped.

## Properties and Methods

### Properties

**AssetsRootUrl** - if set allows the population of mapped MediaFile's **DomainWithUrl** property with an absolute URL.  Useful only in the context where a CDN is used for distributing media files rather than them being served from the web server via relative links.

### Methods

Full signature of mapping methods are as follows:

    IUmbracoMapper Map<T>(IPublishedContent content, 
        T model, 
        Dictionary<string, string> propertyNameMappings = null,
        string[] recursiveProperties = null);

    IUmbracoMapper Map<T>(XElement xml, 
        T model,
        Dictionary<string, string> propertyNameMappings = null);

    IUmbracoMapper Map<T>(Dictionary<string, object> dictionary,
        T model,
        Dictionary<string, string> propertyNameMappings = null);

    IUmbracoMapper Map<T>(string json,
        T model,
        Dictionary<string, string> propertyNameMappings = null);

    IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection, 
        IList<T> modelCollection,
        Dictionary<string, string> propertyNameMappings = null,
        string[] recursiveProperties = null) where T : new();

    IUmbracoMapper MapCollection<T>(XElement xml, IList<T> modelCollection, 
        Dictionary<string, string> propertyNameMappings = null, 
        string groupElementName = "Item", 
        bool createItemsIfNotAlreadyInList = true, 
        string sourceIdentifyingPropName = "Id", 
        string destIdentifyingPropName = "Id") where T : new();

    IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries, 
        IList<T> modelCollection, 
        Dictionary<string, string> propertyNameMappings = null, 
        bool createItemsIfNotAlreadyInList = true, 
        string sourceIdentifyingPropName = "Id", 
        string destIdentifyingPropName = "Id") where T : new();

    IUmbracoMapper MapCollection<T>(string json, IList<T> modelCollection, 
        Dictionary<string, string> propertyNameMappings = null,
        string rootElementName = "items", 
        bool createItemsIfNotAlreadyInList = true, 
        string sourceIdentifyingPropName = "Id", 
        string destIdentifyingPropName = "Id") where T : new();



##Credits

Thanks to Ali Taheri and Neil Cumpstey at [Zone](http://www.thisiszone.com) for code, reviews and testing.
