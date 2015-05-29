namespace Zone.UmbracoMapper
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Web;
    using System.Xml.Linq;
    using Helpers;
    using Newtonsoft.Json.Linq;
    using Umbraco.Core.Models;
    using Umbraco.Web;

    public class UmbracoMapper : IUmbracoMapper
    {
        #region Fields

        /// <summary>
        /// Provides a cache of view model settable properties, only need to use reflection once for each view model within the 
        /// application lifetime
        /// </summary>
        private static ConcurrentDictionary<string, IList<PropertyInfo>> _settableProperties = new ConcurrentDictionary<string, IList<PropertyInfo>>();

        private readonly Dictionary<string, CustomMapping> _customMappings;
        private readonly Dictionary<string, CustomObjectMapping> _customObjectMappings;

        #endregion

        #region Constructor

        public UmbracoMapper()
        {
            _customMappings = new Dictionary<string, CustomMapping>();
            _customObjectMappings = new Dictionary<string, CustomObjectMapping>();
            EnableCaching = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the root URL from where assets are served from in order to populate
        /// absolute URLs for media files (and support CDN delivery)
        /// </summary>
        public string AssetsRootUrl { get; set; }

        /// <summary>
        /// Gets or sets a flag enabling caching.  On by default.
        /// </summary>
        public bool EnableCaching { get; set; }

        #endregion

        #region Interface methods

        /// <summary>
        /// Allows the mapper to use a custom mapping for a specified type from IPublishedContent
        /// </summary>
        /// <param name="propertyTypeFullName">Full name of the property type to map to</param>
        /// <param name="mapping">Mapping function</param>
        /// <param name="propertyName">Restricts this custom mapping to properties of this name</param>
        public IUmbracoMapper AddCustomMapping(string propertyTypeFullName, CustomMapping mapping, string propertyName = null)
        {
            var key = propertyName == null ? propertyTypeFullName : string.Concat(propertyTypeFullName, ".", propertyName);
            _customMappings[key] = mapping;
            return this;
        }

        /// <summary>
        /// Allows the mapper to use a custom mapping for a specified type from an object
        /// </summary>
        /// <param name="propertyTypeFullName">Full name of the property type to map to</param>
        /// <param name="mapping">Mapping function</param>
        /// <param name="propertyName">Restricts this custom mapping to properties of this name</param>
        public IUmbracoMapper AddCustomMapping(string propertyTypeFullName, CustomObjectMapping mapping, string propertyName = null)
        {
            var key = propertyName == null ? propertyTypeFullName : string.Concat(propertyTypeFullName, ".", propertyName);
            _customObjectMappings[key] = mapping;
            return this;
        }

        /// <summary>
        /// Maps an instance of IPublishedContent to the passed view model based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="content">Instance of IPublishedContent</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <param name="propertySet">Set of properties to map</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(IPublishedContent content,
                                     T model,
                                     Dictionary<string, PropertyMapping> propertyMappings = null,
                                     string[] recursiveProperties = null,
                                     PropertySet propertySet = PropertySet.All)
            where T : class
        {
            if (content != null)
            {
                // Ensure model is not null
                if (model == null)
                {
                    throw new ArgumentNullException("model", "Object to map to cannot be null");
                }

                // Property mapping overrides can be passed via the dictionary or via attributes on the view model.
                // The subequent mapping code uses the dictionary only, so we need to reflect on the view model
                // and update the dictionary to include keys provided via the attributes.
                propertyMappings = EnsurePropertyMappingsAndUpdateFromModel(model, propertyMappings);                

                // Similarly, the recursive properties can be passed via string array or attribute
                recursiveProperties = EnsureRecursivePropertiesAndUpdateFromModel(model, recursiveProperties, propertyMappings);

                // Loop through all settable properties on model
                foreach (var property in SettableProperties(model))
                {
                    // Check if property has been marked as ignored, if so, don't attempt to map
                    if (IsPropertyIgnored(propertyMappings, property.Name))
                    {
                        continue;
                    }

                    // Get content to map from (check if we want to map to content at a level above the currently passed node)
                    var contentToMapFrom = GetContentToMapFrom(content, propertyMappings, property.Name);

                    // Check if we have a string value formatter passed
                    var stringValueFormatter = GetStringValueFormatter(propertyMappings, property.Name);

                    // Check if we are looking to concatenate or coalesce more than one source property
                    var multipleMappingOperation = GetMultiplePropertyMappingOperation(propertyMappings, property.Name);
                    switch (multipleMappingOperation)
                    {
                        case MultiplePropertyMappingOperation.Concatenate:

                            // Loop through all the source properties requested for concatenation
                            var concatenationSeperator = propertyMappings[property.Name].ConcatenationSeperator;
                            if (concatenationSeperator == null)
                            {
                                concatenationSeperator = string.Empty;
                            }

                            var isFirst = true;
                            foreach (var sourceProp in propertyMappings[property.Name].SourcePropertiesForConcatenation)
                            {
                                // Call the mapping function, passing in each source property to use, and flag to contatenate
                                // on all but the first
                                propertyMappings[property.Name].SourceProperty = sourceProp;
                                MapContentProperty<T>(model, property, contentToMapFrom, propertyMappings, recursiveProperties,
                                    concatenateToExistingValue: !isFirst, concatenationSeperator: concatenationSeperator, stringValueFormatter: stringValueFormatter, propertySet: propertySet);
                                isFirst = false;
                            }

                            break;
                        case MultiplePropertyMappingOperation.Coalesce:

                            // Loop through all the source properties requested for coalescing
                            foreach (var sourceProp in propertyMappings[property.Name].SourcePropertiesForCoalescing)
                            {
                                // Call the mapping function, passing in each source property to use, and flag to coalesce
                                // on all but the first
                                propertyMappings[property.Name].SourceProperty = sourceProp;
                                MapContentProperty<T>(model, property, contentToMapFrom, propertyMappings, recursiveProperties,
                                    coalesceWithExistingValue: true, stringValueFormatter: stringValueFormatter, propertySet: propertySet);
                            }

                            break;
                        default:

                            // Map the single property
                            MapContentProperty<T>(model, property, contentToMapFrom, propertyMappings, recursiveProperties, stringValueFormatter: stringValueFormatter, propertySet: propertySet);
                            break;
                    }

                    // If property value not set, and default value passed, use it
                    SetDefaultValueIfProvided(model, propertyMappings, property);
                }
            }

            return this;
        }

        /// <summary>
        /// Maps content held in XML to the passed view model based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="xml">XML fragment to map from</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(XElement xml,
                                     T model,
                                     Dictionary<string, PropertyMapping> propertyMappings = null)
            where T : class
        {
            if (xml != null)
            {
                // Ensure model is not null
                if (model == null)
                {
                    throw new ArgumentNullException("model", "Object to map to cannot be null");
                }

                // Property mapping overrides can be passed via the dictionary or via attributes on the view model.
                // The subequent mapping code uses the dictionary only, so we need to reflect on the view model
                // and update the dictionary to include keys provided via the attributes.
                propertyMappings = EnsurePropertyMappingsAndUpdateFromModel(model, propertyMappings);

                // Loop through all settable properties on model
                foreach (var property in SettableProperties(model))
                {
                    var propName = GetMappedPropertyName(property.Name, propertyMappings, false);

                    // If element with mapped name found, map the value (check case insensitively)
                    var mappedElement = GetXElementCaseInsensitive(xml, propName);

                    if (mappedElement != null)
                    {
                        // Check if we are looking for a child mapping
                        if (IsMappingFromChildProperty(propertyMappings, property.Name))
                        {
                            mappedElement = mappedElement.Element(propertyMappings[property.Name].SourceChildProperty);
                        }

                        if (mappedElement != null)
                        {
                            SetTypedPropertyValue(model, property, mappedElement.Value);
                        }
                    }
                    else
                    {
                        // Try to see if it's in an attribute too
                        var mappedAttribute = GetXAttributeCaseInsensitive(xml, propName);
                        if (mappedAttribute != null)
                        {
                            SetTypedPropertyValue(model, property, mappedAttribute.Value);
                        }
                    }

                    // If property value not set, and default value passed, use it
                    SetDefaultValueIfProvided(model, propertyMappings, property);
                }
            }

            return this;
        }

        /// <summary>
        /// Maps custom data held in a dictionary
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="dictionary">Dictionary of property name/value pairs</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(Dictionary<string, object> dictionary,
                                     T model,
                                     Dictionary<string, PropertyMapping> propertyMappings = null)
            where T : class
        {
            if (dictionary != null)
            {
                // Ensure model is not null
                if (model == null)
                {
                    throw new ArgumentNullException("model", "Object to map to cannot be null");
                }

                // Property mapping overrides can be passed via the dictionary or via attributes on the view model.
                // The subequent mapping code uses the dictionary only, so we need to reflect on the view model
                // and update the dictionary to include keys provided via the attributes.
                propertyMappings = EnsurePropertyMappingsAndUpdateFromModel(model, propertyMappings);

                // Loop through all settable properties on model
                foreach (var property in SettableProperties(model))
                {
                    var propName = GetMappedPropertyName(property.Name, propertyMappings);
                    
                    // If element with mapped name found, map the value
                    if (dictionary.ContainsKey(propName))
                    {
                        // First check to see if we have a custom dictionary mapping defined
                        var namedCustomMappingKey = GetNamedCustomMappingKey(property);
                        var unnamedCustomMappingKey = GetUnnamedCustomMappingKey(property); 
                        if (_customObjectMappings.ContainsKey(namedCustomMappingKey))
                        {
                            var value = _customObjectMappings[namedCustomMappingKey](this, dictionary[propName]);
                            if (value != null)
                            {
                                property.SetValue(model, value);
                            }
                        }
                        else if (_customObjectMappings.ContainsKey(unnamedCustomMappingKey))
                        {
                            var value = _customObjectMappings[unnamedCustomMappingKey](this, dictionary[propName]);
                            if (value != null)
                            {
                                property.SetValue(model, value);
                            }
                        }
                        else if (dictionary[propName] is IPublishedContent)
                        {
                            // Handle cases where the value object passed in the dictionary is actually an IPublishedContent
                            // - if so, we'll fully map from that
                            Map((IPublishedContent)dictionary[propName], property.GetValue(model), propertyMappings);
                        }
                        else if (dictionary[propName] is IEnumerable<IPublishedContent>)
                        {
                            // Handle cases where the value object passed in the dictionary is actually an IEnumerable<IPublishedContent> content
                            // - if so, we'll fully map from that
                            // Have to make this call using reflection as we don't know the type of the generic collection at compile time
                            var propertyValue = property.GetValue(model);
                            var collectionPropertyType = GetGenericCollectionType(property);
                            typeof(UmbracoMapper)
                                .GetMethod("MapCollectionOfIPublishedContent", BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(collectionPropertyType)
                                .Invoke(this, new object[] { (IEnumerable<IPublishedContent>)dictionary[propName], propertyValue, propertyMappings });
                        }
                        else
                        {
                            // Otherwise we just map the object as a simple type
                            var stringValue = dictionary[propName] != null ? dictionary[propName].ToString() : string.Empty;
                            SetTypedPropertyValue(model, property, stringValue);
                        }
                    }

                    // If property value not set, and default value passed, use it
                    SetDefaultValueIfProvided(model, propertyMappings, property);
                }
            }

            return this;
        }

        /// <summary>
        /// Maps information held in a JSON string
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="json">JSON string</param>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(string json,
                                     T model,
                                     Dictionary<string, PropertyMapping> propertyMappings = null)
            where T : class
        {
            if (!string.IsNullOrEmpty(json))
            {
                // Ensure model is not null
                if (model == null)
                {
                    throw new ArgumentNullException("model", "Object to map to cannot be null");
                }

                // Property mapping overrides can be passed via the dictionary or via attributes on the view model.
                // The subequent mapping code uses the dictionary only, so we need to reflect on the view model
                // and update the dictionary to include keys provided via the attributes.
                propertyMappings = EnsurePropertyMappingsAndUpdateFromModel(model, propertyMappings);

                // Parse JSON string to queryable object
                var jsonObj = JObject.Parse(json);

                // Loop through all settable properties on model
                foreach (var property in SettableProperties(model))
                {
                    var propName = GetMappedPropertyName(property.Name, propertyMappings, false);

                    // If element with mapped name found, map the value
                    var childPropName = string.Empty;
                    if (IsMappingFromChildProperty(propertyMappings, property.Name))
                    {
                        childPropName = propertyMappings[property.Name].SourceChildProperty;
                    }

                    var stringValue = GetJsonFieldCaseInsensitive(jsonObj, propName, childPropName);
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        SetTypedPropertyValue(model, property, stringValue);
                    }

                    // If property value not set, and default value passed, use it
                    SetDefaultValueIfProvided(model, propertyMappings, property);
                }
            }

            return this;
        }

        /// <summary>
        /// Maps a collection of IPublishedContent to the passed view model
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="contentCollection">Collection of IPublishedContent</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection,
                                               IList<T> modelCollection,
                                               Dictionary<string, PropertyMapping> propertyMappings = null,
                                               string[] recursiveProperties = null,
                                               PropertySet propertySet = PropertySet.All)
            where T : class, new()
        {
            if (contentCollection != null)
            {
                if (modelCollection == null)
                {
                    throw new ArgumentNullException("modelCollection", "Collection to map to can be empty, but not null");
                }

                // Check to see if the collection has any items already, if it does, clear it first (could have come about with an 
                // explicit mapping called before the auto-mapping feature was introduced.  In any case, assuming collection is empty
                // seems reasonable
                if (modelCollection.Any())
                {
                    modelCollection.Clear();
                }

                foreach (var content in contentCollection)
                {
                    var itemToCreate = new T();

                    // Check for custom mappings for the type itself (in the Map() method we'll check for custom mappings on each property)
                    var customMappingKey = itemToCreate.GetType().FullName;
                    if (_customObjectMappings.ContainsKey(customMappingKey))
                    {
                        itemToCreate = _customObjectMappings[customMappingKey](this, content) as T;
                    }
                    else if (_customMappings.ContainsKey(customMappingKey))
                    {
                        // Any custom mappings used here cannot be based on a single property, as we don't have a property to map to to work out what this should be.
                        // So we just pass an empty string into the custom mapping call
                        itemToCreate = _customMappings[customMappingKey](this, content, string.Empty, false) as T;
                    }
                    else
                    {
                        // Otherwise map the single content item as normal
                        Map<T>(content, itemToCreate, propertyMappings, recursiveProperties, propertySet);
                    }

                    modelCollection.Add(itemToCreate);
                }
            }

            return this;
        }

        /// <summary>
        /// Maps a collection of content held in XML to the passed view model collection based on conventions (and/or overrides)
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="xml">XML fragment to map from</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="groupElementName">Name of the element grouping each item in the XML (defaults to "Item")</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this XML element is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(XElement xml, IList<T> modelCollection,
                                               Dictionary<string, PropertyMapping> propertyMappings = null,
                                               string groupElementName = "item",
                                               bool createItemsIfNotAlreadyInList = true,
                                               string sourceIdentifyingPropName = "Id",
                                               string destIdentifyingPropName = "Id")
            where T : class, new()
        {
            if (xml != null)
            {
                if (modelCollection == null)
                {
                    throw new ArgumentNullException("modelCollection", "Collection to map to can be empty, but not null");
                }

                // Loop through each of the items defined in the XML
                foreach (var element in xml.Elements(groupElementName))
                {
                    // Check if item is already in the list by looking up provided unique key
                    T itemToUpdate = default(T);
                    if (TypeHasProperty(typeof(T), destIdentifyingPropName))
                    {
                        itemToUpdate = GetExistingItemFromCollection(modelCollection, destIdentifyingPropName, element.Element(sourceIdentifyingPropName).Value);
                    }

                    if (itemToUpdate != null)
                    {
                        // Item found, so map it
                        Map<T>(element, itemToUpdate, propertyMappings);
                    }
                    else
                    {
                        // Item not found, so create if that was requested
                        if (createItemsIfNotAlreadyInList)
                        {
                            var itemToCreate = new T();
                            Map<T>(element, itemToCreate, propertyMappings);
                            modelCollection.Add(itemToCreate);
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Maps a collection custom data held in an linked dictionary to a collection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="dictionaries">Collection of custom data containing a list of dictionary of property name/value pairs.  One of these keys provides a lookup for the existing collection.</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this dictionary key is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(IEnumerable<Dictionary<string, object>> dictionaries,
                                               IList<T> modelCollection,
                                               Dictionary<string, PropertyMapping> propertyMappings = null,
                                               bool createItemsIfNotAlreadyInList = true,
                                               string sourceIdentifyingPropName = "Id",
                                               string destIdentifyingPropName = "Id")
            where T : class, new()
        {
            if (dictionaries != null)
            {
                if (modelCollection == null)
                {
                    throw new ArgumentNullException("modelCollection", "Collection to map to can be empty, but not null");
                }

                // Loop through each of the items defined in the dictionary
                foreach (var dictionary in dictionaries)
                {
                    // Check if item is already in the list by looking up provided unique key
                    T itemToUpdate = default(T);
                    if (TypeHasProperty(typeof(T), destIdentifyingPropName))
                    {
                        itemToUpdate = GetExistingItemFromCollection(modelCollection, destIdentifyingPropName, dictionary[sourceIdentifyingPropName].ToString());
                    }

                    if (itemToUpdate != null)
                    {
                        // Item found, so map it
                        Map<T>(dictionary, itemToUpdate, propertyMappings);
                    }
                    else
                    {
                        // Item not found, so create if that was requested
                        if (createItemsIfNotAlreadyInList)
                        {
                            var itemToCreate = new T();
                            Map<T>(dictionary, itemToCreate, propertyMappings);
                            modelCollection.Add(itemToCreate);
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Maps a collection custom data held in a JSON string to a collection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="json">JSON string containing collection</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="rootElementName">Name of root element in JSON array</param>
        /// <param name="createItemsIfNotAlreadyInList">Flag indicating whether to create items if they don't already exist in the collection, or to just map to existing ones</param>
        /// <param name="sourceIdentifyingPropName">When updating existing items in a collection, this property name is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").</param>
        /// <param name="destIdentifyingPropName">When updating existing items in a collection, this dictionary key is considered unique and used for look-ups to identify and update the correct item (defaults to "Id").  Case insensitive.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(string json, IList<T> modelCollection,
                                               Dictionary<string, PropertyMapping> propertyMappings = null,
                                               string rootElementName = "items",
                                               bool createItemsIfNotAlreadyInList = true,
                                               string sourceIdentifyingPropName = "Id",
                                               string destIdentifyingPropName = "Id")
            where T : class, new()
        {
            if (!string.IsNullOrEmpty(json))
            {
                if (modelCollection == null)
                {
                    throw new ArgumentNullException("modelCollection", "Collection to map to can be empty, but not null");
                }

                // Loop through each of the items defined in the JSON
                var jsonObject = JObject.Parse(json);
                foreach (var element in jsonObject[rootElementName].Children())
                {
                    // Check if item is already in the list by looking up provided unique key
                    T itemToUpdate = default(T);
                    if (TypeHasProperty(typeof(T), destIdentifyingPropName))
                    {
                        itemToUpdate = GetExistingItemFromCollection(modelCollection, destIdentifyingPropName, element[sourceIdentifyingPropName].Value<string>());
                    }

                    if (itemToUpdate != null)
                    {
                        // Item found, so map it
                        Map<T>(element.ToString(), itemToUpdate, propertyMappings);
                    }
                    else
                    {
                        // Item not found, so create if that was requested
                        if (createItemsIfNotAlreadyInList)
                        {
                            var itemToCreate = new T();
                            Map<T>(element.ToString(), itemToCreate, propertyMappings);
                            modelCollection.Add(itemToCreate);
                        }
                    }
                }
            }

            return this;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Helper to ensure property mappings are not null even if not provided via the dictionary.
        /// Also to populate from attributes on the view model if that method is used for configuration of the mapping
        /// operation.
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">Instance of view model</param>
        /// <param name="propertyMappings">Set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Set of property mappings</returns>
        private Dictionary<string, PropertyMapping> EnsurePropertyMappingsAndUpdateFromModel<T>(T model, Dictionary<string, PropertyMapping> propertyMappings) where T : class
        {
            if (propertyMappings == null)
            {
                propertyMappings = new Dictionary<string, PropertyMapping>();
            }

            foreach (var property in SettableProperties(model))
            {
                var attribute = GetPropertyMappingAttribute(property);
                if (attribute != null)
                {
                    var propertyMapping = GetPropertyMappingAttributeAsPropertyMapping(attribute);
                    if (propertyMappings.ContainsKey(property.Name))
                    {
                        // Property mapping already exists on dictionary, so update the values not already set
                        // (if for some reason on both, dictionary takes priority)
                        if (!string.IsNullOrEmpty(propertyMappings[property.Name].SourceProperty))
                        {
                            propertyMappings[property.Name].SourceProperty = propertyMapping.SourceProperty;
                        }

                        if (propertyMappings[property.Name].LevelsAbove == 0)
                        {
                            propertyMappings[property.Name].LevelsAbove = propertyMapping.LevelsAbove;
                        }

                        if (!string.IsNullOrEmpty(propertyMappings[property.Name].SourceRelatedProperty))
                        {
                            propertyMappings[property.Name].SourceRelatedProperty = propertyMapping.SourceRelatedProperty;
                        }

                        if (!string.IsNullOrEmpty(propertyMappings[property.Name].SourceChildProperty))
                        {
                            propertyMappings[property.Name].SourceChildProperty = propertyMapping.SourceChildProperty;
                        }

                        if (propertyMappings[property.Name].SourcePropertiesForConcatenation == null)
                        {
                            propertyMappings[property.Name].SourcePropertiesForConcatenation = propertyMapping.SourcePropertiesForConcatenation;
                        }

                        if (!string.IsNullOrEmpty(propertyMappings[property.Name].ConcatenationSeperator))
                        {
                            propertyMappings[property.Name].ConcatenationSeperator = propertyMapping.ConcatenationSeperator;
                        }

                        if (propertyMappings[property.Name].SourcePropertiesForCoalescing == null)
                        {
                            propertyMappings[property.Name].SourcePropertiesForCoalescing = propertyMapping.SourcePropertiesForCoalescing;
                        }

                        if (propertyMappings[property.Name].MapIfPropertyMatches.Equals(default(KeyValuePair<string, string>)))
                        {
                            propertyMappings[property.Name].MapIfPropertyMatches = propertyMapping.MapIfPropertyMatches;
                        }

                        if (propertyMappings[property.Name].DefaultValue == null)
                        {
                            propertyMappings[property.Name].DefaultValue = propertyMapping.DefaultValue;
                        }

                        propertyMappings[property.Name].Ignore = propertyMapping.Ignore;
                    }
                    else
                    {
                        // Property mapping not found on dictionary, so add it
                        propertyMappings.Add(property.Name, propertyMapping);
                    }
                }
            }

            return propertyMappings;
        }

        /// <summary>
        /// Helper to recursive properties are not null even if not provided via the string array.
        /// Also to populate from attributes on the view model if that method is used for configuration of the mapping
        /// operation.
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">Instance of view model</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <param name="propertyMappings">Set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>String array of recursive properties</returns>
        private string[] EnsureRecursivePropertiesAndUpdateFromModel<T>(T model, string[] recursiveProperties, Dictionary<string, PropertyMapping> propertyMappings) where T : class
        {
            var recursivePropertiesAsList = new List<string>();
            if (recursiveProperties != null)
            {
                recursivePropertiesAsList.AddRange(recursiveProperties);
            }

            foreach (var property in SettableProperties(model))
            {
                var attribute = GetPropertyMappingAttribute(property);
                if (attribute != null && attribute.MapRecursively && !recursivePropertiesAsList.Contains(property.Name))
                {
                    var propName = GetMappedPropertyName(property.Name, propertyMappings, true);
                    recursivePropertiesAsList.Add(propName);
                }
            }

            return recursivePropertiesAsList.ToArray();
        }

        /// <summary>
        /// Helper to extract the PropertyMappingAttribute from a property of the model
        /// </summary>
        /// <param name="property">Property to check for attribute on</param>
        /// <returns>Instance of attribute if found, otherwise null</returns>
        private static PropertyMappingAttribute GetPropertyMappingAttribute(PropertyInfo property)
        {
            return Attribute.GetCustomAttribute(property, typeof(PropertyMappingAttribute), false) as PropertyMappingAttribute;
        }

        /// <summary>
        /// Helper to convert the values of a property mapping attribute to an instance of PropertyMapping
        /// </summary>
        /// <param name="attribute">Attribute added to a view model property</param>
        /// <returns>PropertyMapping instance</returns>
        private static PropertyMapping GetPropertyMappingAttributeAsPropertyMapping(PropertyMappingAttribute attribute)
        {
            return new PropertyMapping
            {
                SourceProperty = attribute.SourceProperty,
                LevelsAbove = attribute.LevelsAbove,
                SourceChildProperty = attribute.SourceChildProperty,
                SourceRelatedProperty = attribute.SourceRelatedProperty,
                ConcatenationSeperator = attribute.ConcatenationSeperator,
                SourcePropertiesForCoalescing = attribute.SourcePropertiesForCoalescing,
                SourcePropertiesForConcatenation = attribute.SourcePropertiesForConcatenation,
                DefaultValue = attribute.DefaultValue,
                Ignore = attribute.Ignore,
            };
        }

        /// <summary>
        /// Helper to get the settable properties from a model for mapping from the cache or the model object
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">Instance of view model</param>
        /// <returns>Enumerable of settable properties from the model</returns>
        private IList<PropertyInfo> SettableProperties<T>(T model) where T : class
        {
            if (EnableCaching)
            {
                var cacheKey = model.GetType().FullName;
                if (!_settableProperties.ContainsKey(cacheKey))
                {
                    _settableProperties[cacheKey] = SettablePropertiesFromObject(model);
                }

                return _settableProperties[cacheKey];
            }

            return SettablePropertiesFromObject(model);
        }

        /// <summary>
        /// Helper to get the settable properties from a model for mapping from the cache or the model object
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">Instance of view model</param>
        /// <returns>Enumerable of settable properties from the model</returns>
        private IList<PropertyInfo> SettablePropertiesFromObject<T>(T model) where T : class
        {
            return model.GetType().GetProperties()
                .Where(p => p.GetSetMethod() != null)
                .ToList();
        }

        /// <summary>
        /// Helper to get the named custom mapping key (based on type and name)
        /// </summary>
        /// <param name="property">PropertyInfo to map to</param>
        /// <returns>Name of custom mapping key</returns>
        private static string GetNamedCustomMappingKey(PropertyInfo property)
        {
            return string.Concat(property.PropertyType.FullName, ".", property.Name);
        }

        /// <summary>
        /// Helper to get the unnamed custom mapping key (based on type only)
        /// </summary>
        /// <param name="property">PropertyInfo to map to</param>
        /// <returns>Name of custom mapping key</returns>
        private static string GetUnnamedCustomMappingKey(PropertyInfo property)
        {
            return property.PropertyType.FullName;
        }

        /// <summary>
        /// Helper method to find the property name to map to based on conventions (and/or overrides)
        /// </summary>
        /// <param name="propName">Name of property to map to</param>
        /// <param name="propertyMappings">Set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="convertToCamelCase">Flag for whether to convert property name to camel casing before attempting mapping</param>
        /// <returns>Name of property to map from</returns>
        private static string GetMappedPropertyName(string propName, Dictionary<string, PropertyMapping> propertyMappings,
                                             bool convertToCamelCase = false)
        {
            var mappedName = propName;
            if (propertyMappings.ContainsKey(propName) &&
                !string.IsNullOrEmpty(propertyMappings[propName].SourceProperty))
            {
                mappedName = propertyMappings[propName].SourceProperty;
            }

            if (convertToCamelCase)
            {
                mappedName = CamelCase(mappedName);
            }

            return mappedName;
        }

        /// <summary>
        /// Gets the IPublishedContent to map from.  Normally this will be the one passed but it's possible to map at a level above the current node.
        /// </summary>
        /// <param name="content">Passed content to map from</param>
        /// <param name="propertyMappings">Dictionary of properties and levels to map from</param>
        /// <param name="propName">Name of property to map</param>
        /// <returns>Instance of IPublishedContent to map from</returns>
        private IPublishedContent GetContentToMapFrom(IPublishedContent content, Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            var contentToMapFrom = content;
            if (propertyMappings.ContainsKey(propName))
            {
                for (int i = 0; i < propertyMappings[propName].LevelsAbove; i++)
                {
                    contentToMapFrom = contentToMapFrom.Parent;
                }
            }

            return contentToMapFrom;
        }        

        /// <summary>
        /// Maps a given IPublished content field (either native or from document type) to property on view model
        /// </summary>
        /// <typeparam name="T">Type of view model</typeparam>
        /// <param name="model">Instance of view model</param>
        /// <param name="property">Property of view model to map to</param>
        /// <param name="contentToMapFrom">IPublished content to map from</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <param name="recursiveProperties">Optional list of properties that should be treated as recursive for mapping</param>
        /// <param name="concatenateToExistingValue">Flag for if we want to concatenate the value to the existing value</param>
        /// <param name="concatenationSeperator">If using concatenation, use this string to separate items</param>
        /// <param name="coalesceWithExistingValue">Flag for if we want to coalesce the value to the existing value</param>
        /// <param name="stringValueFormatter">A function for transformation of the string value, if passed</param>
        /// <param name="propertySet">Set of properties to map</param>
        private void MapContentProperty<T>(T model, PropertyInfo property, IPublishedContent contentToMapFrom,
                                           Dictionary<string, PropertyMapping> propertyMappings, string[] recursiveProperties = null,
                                           bool concatenateToExistingValue = false, string concatenationSeperator = "",
                                           bool coalesceWithExistingValue = false, 
                                           Func<string, string> stringValueFormatter = null, 
                                           PropertySet propertySet = PropertySet.All)
        {
            // First check to see if there's a condition that might mean we don't carry out the mapping
            if (IsMappingConditional(propertyMappings, property.Name) && !IsMappingSpecifiedAsFromRelatedProperty(propertyMappings, property.Name))
            {
                if (!IsMappingConditionMet(contentToMapFrom, propertyMappings[property.Name].MapIfPropertyMatches))
                {
                    return;
                }
            }

            // Set native IPublishedContent properties (using convention that names match exactly)
            var propName = GetMappedPropertyName(property.Name, propertyMappings);
            if (contentToMapFrom.GetType().GetProperty(propName) != null)
            {
                MapNativeContentProperty(model, property, contentToMapFrom, propName, concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue, stringValueFormatter, propertySet);
                return;
            }

            // Set custom properties (using convention that names match but with camelCasing on IPublishedContent 
            // properties, unless override provided)
            propName = GetMappedPropertyName(property.Name, propertyMappings, true);

            // Map properties, first checking for custom mappings
            var isRecursiveProperty = IsRecursiveProperty(recursiveProperties, propName);
            var namedCustomMappingKey = GetNamedCustomMappingKey(property);
            var unnamedCustomMappingKey = GetUnnamedCustomMappingKey(property);
            if (_customMappings.ContainsKey(namedCustomMappingKey))
            {
                var value = _customMappings[namedCustomMappingKey](this, contentToMapFrom, propName, isRecursiveProperty);
                if (value != null)
                {
                    property.SetValue(model, value);
                }
            }
            else if (_customMappings.ContainsKey(unnamedCustomMappingKey))
            {
                var value = _customMappings[unnamedCustomMappingKey](this, contentToMapFrom, propName, isRecursiveProperty);
                if (value != null)
                {
                    property.SetValue(model, value);
                }
            }
            else
            {
                // Otherwise map types we can handle
                var value = contentToMapFrom.GetPropertyValue(propName, isRecursiveProperty);
                if (value != null)
                {
                    // Check if we are mapping to a related IPublishedContent
                    if (IsMappingSpecifiedAsFromRelatedProperty(propertyMappings, property.Name))
                    {
                        // The value we have will either be:
                        //  - an Id of a related IPublishedContent
                        //  - or the related content itself (if the Umbraco Core Property Editor Converters are in use
                        //    and we have used a standard content picker)
                        //  - or a list of related content (if the Umbraco Core Property Editor Converters are in use
                        //    and we have used a multi-node picker with a single value)

                        // So, try single IPublishedContent first
                        var relatedContentToMapFrom = value as IPublishedContent;

                        // If not, try a list and take the first if it exists
                        if (relatedContentToMapFrom == null)
                        {
                            var listOfRelatedContent = value as IEnumerable<IPublishedContent>;
                            if (listOfRelatedContent != null && listOfRelatedContent.Any())
                            {
                                relatedContentToMapFrom = listOfRelatedContent.First();
                            }
                        }

                        // If it's not already IPublishedContent, now check using Id
                        if (relatedContentToMapFrom == null)
                        {
                            int relatedId;
                            if (int.TryParse(value.ToString(), out relatedId))
                            {
                                // Get the related content
                                var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
                                relatedContentToMapFrom = umbracoHelper.TypedContent(relatedId);
                            }
                        }

                        // If we have a related content item...
                        if (relatedContentToMapFrom != null)
                        {
                            // Check to see if there's a condition that might mean we don't carry out the mapping (on the related content)
                            if (IsMappingConditional(propertyMappings, property.Name))
                            {
                                if (!IsMappingConditionMet(relatedContentToMapFrom, propertyMappings[property.Name].MapIfPropertyMatches))
                                {
                                    return;
                                }
                            }

                            var relatedPropName = propertyMappings[property.Name].SourceRelatedProperty;

                            // Get the mapped field from the related content
                            if (relatedContentToMapFrom.GetType().GetProperty(relatedPropName) != null)
                            {
                                // Got a native field
                                MapNativeContentProperty(model, property, relatedContentToMapFrom, relatedPropName, concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue, stringValueFormatter, propertySet);
                            }
                            else
                            {
                                // Otherwise look at a doc type field
                                value = relatedContentToMapFrom.GetPropertyValue(relatedPropName);
                                if (value != null)
                                {
                                    // Map primitive types
                                    SetTypedPropertyValue(model, property, value.ToString(), concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue, stringValueFormatter, propertySet);
                                }
                            }
                        }
                    }
                    else if (!property.PropertyType.IsSimpleType())
                    {
                        // If Umbraco Core Property Editor Converters (or other known property converters) are installed, we can get back IPublishedContent 
                        // instances automatically.  If that's the case and we are mapping to a complex sub-type, we can "automap" it.
                        // We have to use reflection to do this as the type parameter for the sub-type on the model is only known at run-time.
                        // All mapping customisations are expected to to be implemented as attributes on the sub-type (as we can't pass them in
                        // in the dictionary)
                        if (value is IPublishedContent)
                        {
                            typeof (UmbracoMapper)
                                .GetMethod("MapIPublishedContent", BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(property.PropertyType)
                                .Invoke(this, new object[] {(IPublishedContent) value, property.GetValue(model)});
                        }
                        else if (value is IEnumerable<IPublishedContent> && property.PropertyType.GetInterface("IEnumerable") != null)
                        {
                            var collectionPropertyType = GetGenericCollectionType(property);
                            typeof (UmbracoMapper)
                                .GetMethod("MapCollectionOfIPublishedContent", BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(collectionPropertyType)
                                .Invoke(this, new object[] {(IEnumerable<IPublishedContent>)value, property.GetValue(model), null});                            
                        }
                    }
                    else
                    {
                        // Map primitive types
                        SetTypedPropertyValue(model, property, value.ToString(), concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue, stringValueFormatter, propertySet);
                    }
                }
            }
        }

        /// <summary>
        /// Helper to check if a mapping conditional applies
        /// </summary>
        /// <param name="contentToMapFrom">IPublished content to map from</param>
        /// <param name="mapIfPropertyMatches">Property alias and value to match</param>
        /// <returns>True if mapping condition is met</returns>
        private static bool IsMappingConditionMet(IPublishedContent contentToMapFrom, KeyValuePair<string, string> mapIfPropertyMatches)
        {
            var conditionalPropertyAlias = mapIfPropertyMatches.Key;
            var conditionalPropertyValue = contentToMapFrom.GetPropertyValue(conditionalPropertyAlias, false);
            return conditionalPropertyValue != null && conditionalPropertyValue.ToString().ToLowerInvariant() == mapIfPropertyMatches.Value.ToLowerInvariant();
        }

        /// <summary>
        /// Helper to check if particular property has a conditional mapping
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>True if mapping should be from child property</returns>
        private static bool IsMappingConditional(Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            return propertyMappings.ContainsKey(propName) &&
                   !string.IsNullOrEmpty(propertyMappings[propName].MapIfPropertyMatches.Key);
        }

        /// <summary>
        /// Helper to check if particular property should be mapped from a related property
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>True if mapping should be from child property</returns>
        private static bool IsMappingSpecifiedAsFromRelatedProperty(Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            return propertyMappings.ContainsKey(propName) &&
                   !string.IsNullOrEmpty(propertyMappings[propName].SourceRelatedProperty);
        }

        /// <summary>
        /// Helper to check if particular property should be mapped from a child property
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>True if mapping should be from child property</returns>
        private static bool IsMappingFromChildProperty(Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            return propertyMappings.ContainsKey(propName) &&
                   !string.IsNullOrEmpty(propertyMappings[propName].SourceChildProperty);
        }

        /// <summary>
        /// Helper to check if particular property has a default value
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>True if mapping should be from child property</returns>
        private static bool HasDefaultValue(Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            return propertyMappings.ContainsKey(propName) && propertyMappings[propName].DefaultValue != null;
        }
        
        /// <summary>
        /// Helper to retrieve the string value formatting function for the property mapping if available
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>Formatting function if found, otherwise null</returns>
        private static Func<string, string> GetStringValueFormatter(Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            if (propertyMappings != null &&
                propertyMappings.ContainsKey(propName) &&
                propertyMappings[propName].StringValueFormatter != null)
            {
                return propertyMappings[propName].StringValueFormatter;
            }

            return null;
        }

        /// <summary>
        /// Helper to check if particular property should be mapped from the concatenation or coalescing of more than one 
        /// source property
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>Type of multiple mapping operation</returns>
        private static MultiplePropertyMappingOperation GetMultiplePropertyMappingOperation(Dictionary<string, PropertyMapping> propertyMappings,
            string propName)
        {
            var result = MultiplePropertyMappingOperation.None;

            if (propertyMappings.ContainsKey(propName))
            {
                if (propertyMappings[propName].SourcePropertiesForConcatenation != null &&
                   propertyMappings[propName].SourcePropertiesForConcatenation.Any())
                {
                    result = MultiplePropertyMappingOperation.Concatenate;
                }
                else if (propertyMappings[propName].SourcePropertiesForCoalescing != null &&
                   propertyMappings[propName].SourcePropertiesForCoalescing.Any())
                {
                    result = MultiplePropertyMappingOperation.Coalesce;
                }
            }

            return result;
        }

        /// <summary>
        /// Helper to check if property is ignored
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <returns>True if ignored</returns>
        private static bool IsPropertyIgnored(Dictionary<string, PropertyMapping> propertyMappings, string propName)
        {
            return propertyMappings.ContainsKey(propName) && propertyMappings[propName].Ignore;
        }

        /// <summary>
        /// Helper to check whether given property is defined as recursive
        /// </summary>
        /// <param name="recursiveProperties">Array of recursive property names</param>
        /// <param name="propertyName">Name of property</param>
        /// <returns>True if in list of recursive properties</returns>
        private static bool IsRecursiveProperty(string[] recursiveProperties, string propertyName)
        {
            return recursiveProperties != null && recursiveProperties.Contains(propertyName);
        }

        /// <summary>
        /// Helper to map a native IPublishedContent property to a view model property
        /// </summary>
        /// <typeparam name="T">Type of view model to map to</typeparam>
        /// <param name="model">View model to map to</param>
        /// <param name="property">Property to map to</param>
        /// <param name="contentToMapFrom">IPublishedContent instance to map from</param>
        /// <param name="propName">Name of property to map from</param>
        /// <param name="concatenateToExistingValue">Flag for if we want to concatenate the value to the existing value</param>
        /// <param name="concatenationSeperator">If using concatenation, use this string to separate items</param>
        /// <param name="coalesceWithExistingValue">Flag for if we want to coalesce the value to the existing value</param>
        /// <param name="stringValueFormatter">A function for transformation of the string value, if passed</param>
        /// <param name="propertySet">Set of properties to map. This function will only run for All and Native</param>
        private static void MapNativeContentProperty<T>(T model, PropertyInfo property,
                                                        IPublishedContent contentToMapFrom, string propName,
                                                        bool concatenateToExistingValue = false, string concatenationSeperator = "",
                                                        bool coalesceWithExistingValue = false,
                                                        Func<string, string> stringValueFormatter = null, 
                                                        PropertySet propertySet = PropertySet.All)
        {
            if (propertySet != PropertySet.All && propertySet != PropertySet.Native)
            {
                return;
            }

            var value = contentToMapFrom.GetType().GetProperty(propName).GetValue(contentToMapFrom);

            // If we are mapping to a string, make sure to call ToString().  That way even if the source property is numeric, it'll be mapped.
            // Concatenation and coalescing only supported/makes sense in this case too.
            if (property.PropertyType.Name == "String")
            {
                var stringValue = value.ToString();
                if (concatenateToExistingValue)
                {
                    var prefixValueWith = property.GetValue(model).ToString() + concatenationSeperator;
                    property.SetValue(model, prefixValueWith + stringValue);
                }
                else if (coalesceWithExistingValue)
                {
                    // Check is existing value and only set if it's null, empty or whitespace
                    var existingValue = property.GetValue(model);
                    if (existingValue == null || string.IsNullOrWhiteSpace(existingValue.ToString()))
                    {
                        property.SetValue(model, stringValue);
                    }
                }
                else if (stringValueFormatter != null)
                {
                    property.SetValue(model, stringValueFormatter(stringValue));
                }
                else
                {
                    property.SetValue(model, stringValue);
                }
            }
            else
            {
                property.SetValue(model, value);
            }
        }

        /// <summary>
        /// Helper method to convert a string value to an appropriate type for setting via reflection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">View model to map to</param>
        /// <param name="property">Property to map to</param>
        /// <param name="stringValue">String representation of property value</param>
        /// <param name="concatenateToExistingValue">Flag for if we want to concatenate the value to the existing value</param>
        /// <param name="concatenationSeperator">If using concatenation, use this string to separate items</param>
        /// <param name="coalesceWithExistingValue">Flag for if we want to coalesce the value to the existing value</param>
        /// <param name="stringValueFormatter">A function for transformation of the string value, if passed</param>
        /// <param name="propertySet">Set of properties to map. This function will only run for All and Custom</param>
        private static void SetTypedPropertyValue<T>(T model, PropertyInfo property, string stringValue,
                                                     bool concatenateToExistingValue = false, string concatenationSeperator = "",
                                                     bool coalesceWithExistingValue = false,
                                                     Func<string, string> stringValueFormatter = null,
                                                     PropertySet propertySet = PropertySet.All)
        {
            if (propertySet != PropertySet.All && propertySet != PropertySet.Custom)
            {
                return;
            }

            var propertyTypeName = property.PropertyType.Name;
            var isNullable = false;
            if (propertyTypeName == "Nullable`1" && property.PropertyType.GenericTypeArguments.Length == 1)
            {
                propertyTypeName = property.PropertyType.GenericTypeArguments[0].Name;
                isNullable = true;
            }

            switch (propertyTypeName)
            {
                case "Boolean":
                    bool boolValue;
                    if (stringValue == "1")
                    {
                        // Special case: Archetype stores "1" for boolean true, so we'll handle that convention
                        property.SetValue(model, true);
                    }
                    else if (bool.TryParse(stringValue, out boolValue))
                    {
                        property.SetValue(model, boolValue);
                    }

                    break;
                case "Byte":
                    byte byteValue;
                    if (byte.TryParse(stringValue, out byteValue))
                    {
                        property.SetValue(model, byteValue);
                    }

                    break;
                case "Int16":
                    short shortValue;
                    if (short.TryParse(stringValue, out shortValue))
                    {
                        property.SetValue(model, shortValue);
                    }

                    break;
                case "Int32":
                    int intValue;
                    if (int.TryParse(stringValue, out intValue))
                    {
                        property.SetValue(model, intValue);
                    }

                    break;
                case "Int64":
                    long longValue;
                    if (long.TryParse(stringValue, out longValue))
                    {
                        property.SetValue(model, longValue);
                    }

                    break;
                case "Decimal":
                    decimal decimalValue;
                    if (decimal.TryParse(stringValue, out decimalValue))
                    {
                        property.SetValue(model, decimalValue);
                    }

                    break;
                case "Float":
                    float floatValue;
                    if (float.TryParse(stringValue, out floatValue))
                    {
                        property.SetValue(model, floatValue);
                    }

                    break;
                case "Double":
                    double doubleValue;
                    if (double.TryParse(stringValue, out doubleValue))
                    {
                        property.SetValue(model, doubleValue);
                    }

                    break;
                case "DateTime":
                    DateTime dateTimeValue;
                    if (DateTime.TryParse(stringValue, out dateTimeValue))
                    {
                        // Umbraco returns DateTime.MinValue if no date set.  If mapping to a nullable date time makes more sense
                        // to leave as null if this value is returned.
                        if (dateTimeValue > DateTime.MinValue || !isNullable)
                        {
                            property.SetValue(model, dateTimeValue);
                        }
                    }

                    break;
                case "IHtmlString":
                    var htmlString = new HtmlString(stringValue);
                    property.SetValue(model, htmlString);
                    break;
                case "String":

                    // Only supporting/makes sense to allow concatenation and coalescing for String type
                    if (concatenateToExistingValue)
                    {
                        var prefixValueWith = property.GetValue(model).ToString() + concatenationSeperator;
                        property.SetValue(model, prefixValueWith + stringValue);
                    }
                    else if (coalesceWithExistingValue)
                    {
                        // Check is existing value and only set if it's null, empty or whitespace
                        if (property.GetValue(model) == null || string.IsNullOrWhiteSpace(property.GetValue(model).ToString()))
                        {
                            property.SetValue(model, stringValue);
                        }
                    }
                    else if (stringValueFormatter != null)
                    {
                        property.SetValue(model, stringValueFormatter(stringValue));
                    }
                    else
                    {
                        property.SetValue(model, stringValue);
                    }

                    break;
            }
        }

        /// <summary>
        /// Helper to check if a given type has a property
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <param name="propName">Name of property</param>
        /// <returns>True of property exists on type</returns>
        private bool TypeHasProperty(Type type, string propName)
        {
            return type
                       .GetProperties()
                       .SingleOrDefault(p => p.Name == propName) != null;
        }

        /// <summary>
        /// Helper method to get an existing item from the model collection
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="modelPropertyName">Model property name to look up</param>
        /// <param name="valueToMatch">Property value to match on</param>
        /// <returns>Single instance of T if found in the collection</returns>
        private static T GetExistingItemFromCollection<T>(IList<T> modelCollection, string modelPropertyName, string valueToMatch) where T : new()
        {
            return modelCollection.SingleOrDefault(x => x.GetType()
                                                         .GetProperties()
                                                         .Single(p => p.Name == modelPropertyName)
                                                         .GetValue(x).ToString().ToLowerInvariant() == valueToMatch.ToLowerInvariant());
        }

        /// <summary>
        /// Helper method to convert a string into camel case
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Camel cased string</returns>
        private static string CamelCase(string input)
        {
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Helper to retrieve an XElement by name case insensitively
        /// </summary>
        /// <param name="xml">Xml fragment to search in</param>
        /// <param name="propName">Element name to look up</param>
        /// <returns>Matched XElement</returns>
        private static XElement GetXElementCaseInsensitive(XElement xml, string propName)
        {
            if (xml != null)
            {
                return xml.Elements().SingleOrDefault(s => string.Compare(s.Name.ToString(), propName, true) == 0);
            }

            return null;
        }

        /// <summary>
        /// Helper to retrieve an XAttribute by name case insensitively
        /// </summary>
        /// <param name="xml">Xml fragment to search in</param>
        /// <param name="propName">Element name to look up</param>
        /// <returns>Matched XAttribute</returns>
        private static XAttribute GetXAttributeCaseInsensitive(XElement xml, string propName)
        {
            return xml.Attributes().SingleOrDefault(s => string.Compare(s.Name.ToString(), propName, true) == 0);
        }

        /// <summary>
        /// Helper to retrieve a JSON field by name case insensitively
        /// </summary>
        /// <param name="jsonObj">JSON object to get field value from</param>
        /// <param name="propName">Property name to look up</param>
        /// <param name="childPropName">Child property name to look up</param>
        /// <returns>String value of JSON field</returns>
        private string GetJsonFieldCaseInsensitive(JObject jsonObj, string propName, string childPropName)
        {
            var token = GetJToken(jsonObj, propName);
            if (token != null)
            {
                if (!string.IsNullOrEmpty(childPropName))
                {
                    // Looking up on child object
                    var childToken = token[childPropName];

                    // If not found, try with lower case
                    if (childToken == null)
                    {
                        childToken = token[childPropName.ToLowerInvariant()];
                    }

                    // If still not found, try with camel case
                    if (childToken == null)
                    {
                        childToken = token[CamelCase(childPropName)];
                    }

                    if (childToken != null)
                    {
                        return (string)childToken;
                    }
                }
                else
                {
                    // Looking up directly on object
                    return (string)token;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Lookup for JSON property
        /// </summary>
        /// <param name="jsonObj">JSON object</param>
        /// <param name="propName">Property to get value for</param>
        /// <returns>JToken object, if found</returns>
        private JToken GetJToken(JObject jsonObj, string propName)
        {
            var token = jsonObj[propName];

            // If not found, try with lower case
            if (token == null)
            {
                token = jsonObj[propName.ToLowerInvariant()];
            }

            // If still not found, try with camel case
            if (token == null)
            {
                token = jsonObj[CamelCase(propName)];
            }

            return token;
        }

        /// <summary>
        /// Maps a collection of IPublishedContent to the passed view model
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="contentCollection">Collection of IPublishedContent</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        /// <remarks>
        /// This method is created purely to support making a call to mapping a collection via reflection, to avoid the ambiguous match exception caused 
        /// by having multiple overloads.
        /// </remarks>
        private IUmbracoMapper MapCollectionOfIPublishedContent<T>(IEnumerable<IPublishedContent> contentCollection,
            IList<T> modelCollection,
            Dictionary<string, PropertyMapping> propertyMappings) where T : class, new()
        {
            return MapCollection<T>(contentCollection, modelCollection, propertyMappings);
        }

        /// <summary>
        /// Maps a single IPublishedContent to the passed view model
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="content">Single IPublishedContent</param>
        /// <param name="model">Model to map to</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        /// <remarks>
        /// This method is created purely to support making a call to mapping a collection via reflection, to avoid the ambiguous match exception caused 
        /// by having multiple overloads.
        /// </remarks>
        private IUmbracoMapper MapIPublishedContent<T>(IPublishedContent content, T model) where T : class, new()
        {
            return Map<T>(content, model);
        }

        /// <summary>
        /// Helper to set the default value of a mapped property, if provided and mapping operation hasn't already found a value
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">View model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <param name="property">Property of view model to map to</param>
        private static void SetDefaultValueIfProvided<T>(T model, Dictionary<string, PropertyMapping> propertyMappings, PropertyInfo property) where T : class
        {
            if (HasDefaultValue(propertyMappings, property.Name) && IsNullOrDefault(property.GetValue(model)))
            {
                property.SetValue(model, propertyMappings[property.Name].DefaultValue);
            }
        }

        /// <summary>
        /// Helper to check if a given value (reference or value type) is null or the default value
        /// </summary>
        /// <typeparam name="T">Type of argument</typeparam>
        /// <param name="argument">Argument to check</param>
        /// <returns>True if null or default</returns>
        /// <remarks>See: http://stackoverflow.com/a/6553276/489433</remarks>
        private static bool IsNullOrDefault<T>(T argument)
        {
            // Deal with normal scenarios
            if (argument == null) return true;
            if (object.Equals(argument, default(T))) return true;

            // Handle empty string (we'll treat this as a default value for string even through strictly the default is null, as that's what Umbraco
            // will return for an empty string property value)
            var stringArgument = argument as string;
            if (string.IsNullOrEmpty(stringArgument))
            {
                return true;
            }

            // Deal with non-null nullables
            Type methodType = typeof(T);
            if (Nullable.GetUnderlyingType(methodType) != null) return false;

            // Deal with boxed value types
            Type argumentType = argument.GetType();
            if (argumentType.IsValueType && argumentType != methodType)
            {
                object obj = Activator.CreateInstance(argument.GetType());
                return obj.Equals(argument);
            }

            return false;
        }

        /// <summary>
        /// Helper to determine the type of a generic collection
        /// </summary>
        /// <param name="property">Property info for collection</param>
        /// <returns>Type of collection</returns>
        private static Type GetGenericCollectionType(PropertyInfo property)
        {
            return property.PropertyType.GetTypeInfo().GenericTypeArguments[0];
        }

        #endregion
    }
}
