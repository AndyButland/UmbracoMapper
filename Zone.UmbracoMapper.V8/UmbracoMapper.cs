namespace Zone.UmbracoMapper.V8
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using Newtonsoft.Json.Linq;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web.Composing;
    using Zone.UmbracoMapper.Common;
    using Zone.UmbracoMapper.Common.Attributes;
    using Zone.UmbracoMapper.Common.BaseDestinationTypes;
    using Zone.UmbracoMapper.Common.Helpers;
    using Zone.UmbracoMapper.V8.Attributes;
    using Zone.UmbracoMapper.V8.Extensions;

    public class UmbracoMapper : UmbracoMapperBase, IUmbracoMapper
    {
        private readonly Dictionary<string, CustomMapping> _customMappings;
        private readonly Dictionary<string, CustomObjectMapping> _customObjectMappings;

        public UmbracoMapper() 
            : this(new DefaultPropertyValueGetter())
        {
        }

        public UmbracoMapper(IPropertyValueGetter propertyValueGetter)
        {
            DefaultPropertyValueGetter = propertyValueGetter;
            _customMappings = new Dictionary<string, CustomMapping>();
            _customObjectMappings = new Dictionary<string, CustomObjectMapping>();

            InitializeDefaultCustomMappings();
            EnableCaching = true;
        }

        /// <summary>
        /// Defines the default method for retrieving values from a property
        /// </summary>
        public IPropertyValueGetter DefaultPropertyValueGetter { get; set; }

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
        /// <param name="propertySet">Set of properties to map</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper Map<T>(IPublishedContent content,
                                     T model,
                                     Dictionary<string, PropertyMapping> propertyMappings = null,
                                     PropertySet propertySet = PropertySet.All)
            where T : class
        {
            if (content == null)
            {
                return this;
            }

            // Ensure model is not null
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), "Object to map to cannot be null");
            }

            // Property mapping overrides can be passed via the dictionary or via attributes on the view model.
            // The subequent mapping code uses the dictionary only, so we need to reflect on the view model
            // and update the dictionary to include keys provided via the attributes.
            propertyMappings = EnsurePropertyMappingsAndUpdateFromModel(model, propertyMappings);      
                
            // Cast dictionary to base type, which can then be used by methods in the "common" referenced project.
            var propertyMappingsBase = propertyMappings.AsBaseDictionary();

            // Loop through all settable properties on model
            foreach (var property in SettableProperties(model))
            {
                // Check if property has been marked as ignored, if so, don't attempt to map
                if (propertyMappingsBase.IsPropertyIgnored(property.Name))
                {
                    continue;
                }

                // Check if mapping from a dictionary value
                if (propertyMappingsBase.IsMappingFromDictionaryValue(property.Name))
                {
                    SetValueFromDictionary(model, property, propertyMappings[property.Name].DictionaryKey);
                    continue;
                }

                // Get content to map from (check if we want to map to content at a level above the currently passed node) and also
                // get the level above the current node that we are mapping from
                var contentToMapFrom = GetContentToMapFrom(content, propertyMappings, property.Name, out int levelsAbove);

                // Check if property is a complex type and is being asked to be mapped from a higher level in the node tree.
                // If so, we need to trigger a separate mapping operation for this.
                if (!property.PropertyType.IsSimpleType() && levelsAbove > 0)
                {
                    typeof(UmbracoMapper)
                        .GetMethod("MapIPublishedContent", BindingFlags.NonPublic | BindingFlags.Instance)
                        .MakeGenericMethod(property.PropertyType)
                        .Invoke(this, new[] { contentToMapFrom, property.GetValue(model) });
                    continue;
                }

                // Check if we have a string value formatter passed
                var stringValueFormatter = propertyMappingsBase.GetStringValueFormatter(property.Name);

                // If default value passed, set it.  If a mapping is completed it'll be overwritten.
                SetDefaultValueIfProvided(model, propertyMappings.AsBaseDictionary(), property);

                // Check if we are looking to concatenate or coalesce more than one source property
                var multipleMappingOperation = propertyMappingsBase.GetMultiplePropertyMappingOperation(property.Name);
                switch (multipleMappingOperation)
                {
                    case MultiplePropertyMappingOperation.Concatenate:

                        // Loop through all the source properties requested for concatenation
                        var concatenationSeperator = propertyMappings[property.Name].ConcatenationSeperator ?? string.Empty;

                        var isFirst = true;
                        foreach (var sourceProp in propertyMappings[property.Name].SourcePropertiesForConcatenation)
                        {
                            // Call the mapping function, passing in each source property to use, and flag to contatenate
                            // on all but the first
                            propertyMappings[property.Name].SourceProperty = sourceProp;
                            MapContentProperty(model, property, contentToMapFrom, propertyMappings, propertyMappingsBase,
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
                            MapContentProperty(model, property, contentToMapFrom, propertyMappings, propertyMappingsBase,
                                coalesceWithExistingValue: true, stringValueFormatter: stringValueFormatter, propertySet: propertySet);
                        }

                        break;
                    default:

                        // Map the single property
                        MapContentProperty(model, property, contentToMapFrom, propertyMappings, propertyMappingsBase, stringValueFormatter: stringValueFormatter, propertySet: propertySet);
                        break;
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
            if (xml == null)
            {
                return this;
            }

            // Ensure model is not null
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), "Object to map to cannot be null");
            }

            // Property mapping overrides can be passed via the dictionary or via attributes on the view model.
            // The subequent mapping code uses the dictionary only, so we need to reflect on the view model
            // and update the dictionary to include keys provided via the attributes.
            propertyMappings = EnsurePropertyMappingsAndUpdateFromModel(model, propertyMappings);

            // Cast dictionary to base type, which can then be used by methods in the "common" referenced project.
            var propertyMappingsBase = propertyMappings.AsBaseDictionary();

            MapFromXml(xml, model, propertyMappingsBase);

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
            if (dictionary == null)
            {
                return this;
            }

            // Ensure model is not null
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), "Object to map to cannot be null");
            }

            // Property mapping overrides can be passed via the dictionary or via attributes on the view model.
            // The subequent mapping code uses the dictionary only, so we need to reflect on the view model
            // and update the dictionary to include keys provided via the attributes.
            propertyMappings = EnsurePropertyMappingsAndUpdateFromModel(model, propertyMappings);

            // Cast dictionary to base type, which can then be used by methods in the "common" referenced project.
            var propertyMappingsBase = propertyMappings.AsBaseDictionary();

            // Loop through all settable properties on model
            foreach (var property in SettableProperties(model))
            {
                var propName = GetMappedPropertyName(property.Name, propertyMappingsBase);
                    
                // If element with mapped name found, map the value
                if (dictionary.ContainsKey(propName))
                {
                    // First check to see if property is marked with an attribute that implements IMapFromAttribute - if so, use that
                    var mapFromAttribute = GetMapFromAttribute(property);
                    if (mapFromAttribute != null)
                    {
                        mapFromAttribute.SetPropertyValue(dictionary[propName], property, model, this);
                        continue;
                    }

                    // Then check to see if we have a custom dictionary mapping defined
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
                SetDefaultValueIfProvided(model, propertyMappings.AsBaseDictionary(), property);
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
            if (string.IsNullOrEmpty(json))
            {
                return this;
            }

            // Ensure model is not null
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), "Object to map to cannot be null");
            }

            // Property mapping overrides can be passed via the dictionary or via attributes on the view model.
            // The subequent mapping code uses the dictionary only, so we need to reflect on the view model
            // and update the dictionary to include keys provided via the attributes.
            propertyMappings = EnsurePropertyMappingsAndUpdateFromModel(model, propertyMappings);

            // Cast dictionary to base type, which can then be used by methods in the "common" referenced project.
            var propertyMappingsBase = propertyMappings.AsBaseDictionary();

            MapFromJson(json, model, propertyMappingsBase);

            return this;
        }

        /// <summary>
        /// Maps a collection of IPublishedContent to the passed view model
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="contentCollection">Collection of IPublishedContent</param>
        /// <param name="modelCollection">Collection from view model to map to</param>
        /// <param name="propertyMappings">Optional set of property mappings, for use when convention mapping based on name is not sufficient.  Can also indicate the level from which the map should be made above the current content node.  This allows you to pass the level in above the current content for where you want to map a particular property.  E.g. passing { "heading", 1 } will get the heading from the node one level up.</param>
        /// <param name="propertySet">Set of properties to map</param>
        /// <param name="clearCollectionBeforeMapping">Flag indicating whether to clear the collection mapping too before carrying out the mapping</param>
        /// <returns>Instance of IUmbracoMapper</returns>
        public IUmbracoMapper MapCollection<T>(IEnumerable<IPublishedContent> contentCollection,
                                               IList<T> modelCollection,
                                               Dictionary<string, PropertyMapping> propertyMappings = null,
                                               PropertySet propertySet = PropertySet.All, 
                                               bool clearCollectionBeforeMapping = true)
            where T : class, new()
        {
            if (contentCollection == null)
            {
                return this;
            }

            if (modelCollection == null)
            {
                throw new ArgumentNullException(nameof(modelCollection), "Collection to map to can be empty, but not null");
            }

            // Check to see if the collection has any items already, if it does, clear it first (could have come about with an 
            // explicit mapping called before the auto-mapping feature was introduced).  In any case, assuming collection is empty
            // seems reasonable so this is the default behaviour.
            if (clearCollectionBeforeMapping && modelCollection.Any())
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
                    Map<T>(content, itemToCreate, propertyMappings, propertySet);
                }

                modelCollection.Add(itemToCreate);
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
            if (xml == null)
            {
                return this;
            }

            if (modelCollection == null)
            {
                throw new ArgumentNullException(nameof(modelCollection), "Collection to map to can be empty, but not null");
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
                    if (!createItemsIfNotAlreadyInList)
                    {
                        continue;
                    }

                    var itemToCreate = new T();
                    Map(element, itemToCreate, propertyMappings);
                    modelCollection.Add(itemToCreate);
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
            if (dictionaries == null)
            {
                return this;
            }

            if (modelCollection == null)
            {
                throw new ArgumentNullException(nameof(modelCollection), "Collection to map to can be empty, but not null");
            }

            // Loop through each of the items defined in the dictionary
            foreach (var dictionary in dictionaries)
            {
                // Check if item is already in the list by looking up provided unique key
                var itemToUpdate = default(T);
                if (TypeHasProperty(typeof(T), destIdentifyingPropName))
                {
                    itemToUpdate = GetExistingItemFromCollection(modelCollection, destIdentifyingPropName, dictionary[sourceIdentifyingPropName].ToString());
                }

                if (itemToUpdate != null)
                {
                    // Item found, so map it
                    Map(dictionary, itemToUpdate, propertyMappings);
                }
                else
                {
                    // Item not found, so create if that was requested
                    if (!createItemsIfNotAlreadyInList)
                    {
                        continue;
                    }

                    var itemToCreate = new T();
                    Map(dictionary, itemToCreate, propertyMappings);
                    modelCollection.Add(itemToCreate);
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
            if (string.IsNullOrEmpty(json))
            {
                return this;
            }

            if (modelCollection == null)
            {
                throw new ArgumentNullException(nameof(modelCollection), "Collection to map to can be empty, but not null");
            }

            // Loop through each of the items defined in the JSON
            var jsonObject = JObject.Parse(json);
            foreach (var element in jsonObject[rootElementName].Children())
            {
                // Check if item is already in the list by looking up provided unique key
                var itemToUpdate = default(T);
                if (TypeHasProperty(typeof(T), destIdentifyingPropName))
                {
                    itemToUpdate = GetExistingItemFromCollection(modelCollection, destIdentifyingPropName, element[sourceIdentifyingPropName].Value<string>());
                }

                if (itemToUpdate != null)
                {
                    // Item found, so map it
                    Map(element.ToString(), itemToUpdate, propertyMappings);
                }
                else
                {
                    // Item not found, so create if that was requested
                    if (!createItemsIfNotAlreadyInList)
                    {
                        continue;
                    }

                    var itemToCreate = new T();
                    Map(element.ToString(), itemToCreate, propertyMappings);
                    modelCollection.Add(itemToCreate);
                }
            }

            return this;
        }

        /// <summary>
        /// Sets up the default mappings of known types that will be handled automatically
        /// </summary>
        private void InitializeDefaultCustomMappings()
        {
            InitializeDefaultCustomMappingForMediaFile();
            InitializeDefaultCustomMappingForMediaFileCollection();
        }

        /// <summary>
        /// If a custom mapping hasn't already been provided, sets up the default mappings of single instances of <see cref="MediaFile"/> that will be handled automatically
        /// </summary>
        private void InitializeDefaultCustomMappingForMediaFile()
        {
            var customMappingKey = typeof(MediaFile).FullName;
            if (!_customMappings.ContainsKey(customMappingKey))
            {
                AddCustomMapping(customMappingKey, PickedMediaMapper.MapMediaFile);
            }
        }

        /// <summary>
        /// If a custom mapping hasn't already been provided, sets up the default mappings of collections of <see cref="MediaFile"/> that will be handled automatically
        /// </summary>
        private void InitializeDefaultCustomMappingForMediaFileCollection()
        {
            var customMappingKey = typeof(IEnumerable<MediaFile>).FullName;
            if (!_customMappings.ContainsKey(customMappingKey))
            {
                AddCustomMapping(customMappingKey, PickedMediaMapper.MapMediaFileCollection);
            }
        }

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

            var propertyMappingsBase = propertyMappings.AsBaseDictionary();

            foreach (var property in SettableProperties(model))
            {
                var attribute = GetPropertyMappingAttribute(property);
                if (attribute == null)
                {
                    continue;
                }

                var propertyMapping = GetPropertyMappingAttributeAsPropertyMapping(attribute);
                if (propertyMappings.ContainsKey(property.Name))
                {
                    MapPropertyMappingValuesFromAttributeToDictionaryIfNotAlreadySet(propertyMappingsBase, property, propertyMapping);

                    if (propertyMappings[property.Name].CustomMapping == null)
                    {
                        propertyMappings[property.Name].CustomMapping = propertyMapping.CustomMapping;
                    }
                }
                else
                {
                    // Property mapping not found on dictionary, so add it
                    propertyMappings.Add(property.Name, propertyMapping);
                }
            }

            return propertyMappings;
        }

        /// <summary>
        /// Helper to convert the values of a property mapping attribute to an instance of PropertyMapping
        /// </summary>
        /// <param name="attribute">Attribute added to a view model property</param>
        /// <returns>PropertyMapping instance</returns>
        private static PropertyMapping GetPropertyMappingAttributeAsPropertyMapping(PropertyMappingAttribute attribute)
        {
            var mapping = new PropertyMapping();
            MapBasePropertyValues(attribute, mapping);
            mapping.CustomMapping = InstantiateCustomMappingDelegateFromAttributeFields(attribute);
            return mapping;
        }

        private static CustomMapping InstantiateCustomMappingDelegateFromAttributeFields(PropertyMappingAttribute attribute)
        {
            if (attribute.CustomMappingType == null || string.IsNullOrEmpty(attribute.CustomMappingMethod))
            {
                return null;
            }

            var customMappingMethod = GetCustomMappingMethod(attribute.CustomMappingType, attribute.CustomMappingMethod);
            if (customMappingMethod == null)
            {
                return null;
            }

            return (CustomMapping)Delegate.CreateDelegate(typeof(CustomMapping), customMappingMethod);
        }

        /// <summary>
        /// Helper to retrieve an attribute derived from IMapFromAttribute from a property
        /// </summary>
        /// <param name="property">Property to retrieve the attribute from</param>
        /// <returns>IMapFromAttribute marked on the property, or null if no such attribute is found</returns>
        private static IMapFromAttribute GetMapFromAttribute(PropertyInfo property)
        {
            return (IMapFromAttribute)property.GetCustomAttributes(false)
                .FirstOrDefault(x => x is IMapFromAttribute);
        }

        /// <summary>
        /// Gets the IPublishedContent to map from.  Normally this will be the one passed but it's possible to map at a level above the current node.
        /// </summary>
        /// <param name="content">Passed content to map from</param>
        /// <param name="propertyMappings">Dictionary of properties and levels to map from</param>
        /// <param name="propName">Name of property to map</param>
        /// <param name="levelsAbove">Output parameter indicating the levels above the current node we are mapping from</param>
        /// <returns>Instance of IPublishedContent to map from</returns>
        private static IPublishedContent GetContentToMapFrom(IPublishedContent content, 
                                                             IReadOnlyDictionary<string, PropertyMapping> propertyMappings, 
                                                             string propName, 
                                                             out int levelsAbove)
        {
            levelsAbove = 0;
            var contentToMapFrom = content;
            if (!propertyMappings.ContainsKey(propName))
            {
                return contentToMapFrom;
            }

            levelsAbove = propertyMappings[propName].LevelsAbove;
            for (var i = 0; i < levelsAbove; i++)
            {
                contentToMapFrom = contentToMapFrom.Parent;
                if (contentToMapFrom == null)
                {
                    break;
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
                                           Dictionary<string, PropertyMapping> propertyMappings,
                                           Dictionary<string, PropertyMappingBase> propertyMappingsBase, 
                                           string[] recursiveProperties = null,
                                           bool concatenateToExistingValue = false, string concatenationSeperator = "",
                                           bool coalesceWithExistingValue = false, 
                                           Func<string, string> stringValueFormatter = null, 
                                           PropertySet propertySet = PropertySet.All)
        {
            // If the content we are mapping from is null then we can't map it
            if (contentToMapFrom == null)
            {
                return;
            }

            // Get the property value getter for this view model property
            var propertyValueGetter = GetPropertyValueGetter(property.Name, propertyMappings);

            // First check to see if there's a condition that might mean we don't carry out the mapping
            if (propertyMappingsBase.IsMappingConditional(property.Name) && 
                !propertyMappingsBase.IsMappingSpecifiedAsFromRelatedProperty(property.Name) && 
                !IsMappingConditionMet(contentToMapFrom, propertyValueGetter, propertyMappings[property.Name].MapIfPropertyMatches))
            {
                return;
            }

            // Set native IPublishedContent properties (using convention that names match exactly)
            var propName = GetMappedPropertyName(property.Name, propertyMappingsBase);
            if (contentToMapFrom.GetType().GetProperty(propName) != null)
            {
                MapNativeContentProperty(model, property, contentToMapFrom, propName, concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue, stringValueFormatter, propertySet);
                return;
            }
            
            // Set custom properties (using convention that names match but with camelCasing on IPublishedContent 
            // properties, unless override provided)
            propName = GetMappedPropertyName(property.Name, propertyMappingsBase, true);

            // Check to see if property should be mapped recursively
            var isRecursiveProperty = propertyMappingsBase.IsMappingRecursive(property.Name);

            // Check to see if property is marked with an attribute that implements IMapFromAttribute - if so, use that
            var mapFromAttribute = GetMapFromAttribute(property);
            if (mapFromAttribute != null)
            {
                SetValueFromMapFromAttribute(model, property, contentToMapFrom, mapFromAttribute, propName, isRecursiveProperty, propertyValueGetter, concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue);
                return;
            }

            // Map properties, first checking for custom mappings
            var namedCustomMappingKey = GetNamedCustomMappingKey(property);
            var unnamedCustomMappingKey = GetUnnamedCustomMappingKey(property);
            if (HasProvidedCustomMapping(propertyMappings, property.Name, out CustomMapping providedCustomMapping))
            {
                SetValueFromCustomMapping(model, property, contentToMapFrom, providedCustomMapping, propName, isRecursiveProperty);
            }
            else if (_customMappings.ContainsKey(namedCustomMappingKey))
            {
                SetValueFromCustomMapping(model, property, contentToMapFrom, _customMappings[namedCustomMappingKey], propName, isRecursiveProperty);
            }
            else if (_customMappings.ContainsKey(unnamedCustomMappingKey))
            {
                SetValueFromCustomMapping(model, property, contentToMapFrom, _customMappings[unnamedCustomMappingKey], propName, isRecursiveProperty);
            }
            else
            {
                // Otherwise map types we can handle
                var value = GetPropertyValue(contentToMapFrom, propertyValueGetter, propName, isRecursiveProperty);
                if (value == null)
                {
                    return;
                }

                // Check if we are mapping to a related IPublishedContent
                if (propertyMappingsBase.IsMappingSpecifiedAsFromRelatedProperty(property.Name))
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
                        relatedContentToMapFrom = listOfRelatedContent?.FirstOrDefault();
                    }

                    // If it's not already IPublishedContent, now check using Id
                    if (relatedContentToMapFrom == null && int.TryParse(value.ToString(), out int relatedId))
                    {
                        relatedContentToMapFrom = Current.UmbracoContext.ContentCache.GetById(relatedId);
                    }

                    // If we have a related content item...
                    if (relatedContentToMapFrom == null)
                    {
                        return;
                    }

                    // Check to see if there's a condition that might mean we don't carry out the mapping (on the related content)
                    if (propertyMappingsBase.IsMappingConditional(property.Name) &&
                        !IsMappingConditionMet(relatedContentToMapFrom, propertyValueGetter, propertyMappings[property.Name].MapIfPropertyMatches))
                    {
                        return;
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
                        value = GetPropertyValue(relatedContentToMapFrom, propertyValueGetter, relatedPropName);
                        if (value != null)
                        {
                            // Map primitive types
                            SetTypedPropertyValue(model, property, value.ToString(), concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue, stringValueFormatter, propertySet);
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
                        typeof(UmbracoMapper)
                            .GetMethod("MapIPublishedContent", BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(property.PropertyType)
                            .Invoke(this, new[] { (IPublishedContent)value, property.GetValue(model) });
                    }
                    else if (value is IEnumerable<IPublishedContent> && property.PropertyType.GetInterface("IEnumerable") != null)
                    {
                        var collectionPropertyType = GetGenericCollectionType(property);
                        typeof(UmbracoMapper)
                            .GetMethod("MapCollectionOfIPublishedContent", BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(collectionPropertyType)
                            .Invoke(this, new[] { (IEnumerable<IPublishedContent>)value, property.GetValue(model), null });
                    }
                    else if (value.GetType().IsAssignableFrom(property.PropertyType))
                    {
                        // We could also have an instance of IPropertyValueGetter in use here.
                        // If that returns a complex type and it matches the type of the view model, 
                        // we can set it here.
                        // See: https://our.umbraco.com/projects/developer-tools/umbraco-mapper/bugs-questions-suggestions/92608-setting-complex-model-properties-using-custom-ipropertyvaluegetter
                        property.SetValue(model, value);
                    }
                }
                else
                {
                    // Map primitive types
                    SetTypedPropertyValue(model, property, value.ToString(), concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue, stringValueFormatter, propertySet);
                }
            }
        }

        /// <summary>
        /// Helper method to set a property value using an <see cref="IMapFromAttribute"/>
        /// </summary>
        /// <typeparam name="T">Type of view model</typeparam>
        /// <param name="model">Instance of view model</param>
        /// <param name="property">Property of view model to map to</param>
        /// <param name="contentToMapFrom">IPublished content to map from</param>
        /// <param name="mapFromAttribute">Instance of <see cref="IMapFromAttribute"/> to use for mapping</param>
        /// <param name="propName">Name of property to map to</param>
        /// <param name="isRecursiveProperty">Flag for if property should be mapped of recursively properties</param>
        /// <param name="propertyValueGetter">Type implementing <see cref="IPropertyValueGetter"/> with method to get property value</param>
        /// <param name="concatenateToExistingValue">Flag for if we want to concatenate the value to the existing value</param>
        /// <param name="concatenationSeperator">If using concatenation, use this string to separate items</param>
        /// <param name="coalesceWithExistingValue">Flag for if we want to coalesce the value to the existing value</param>
        private void SetValueFromMapFromAttribute<T>(T model, PropertyInfo property, IPublishedContent contentToMapFrom,
                                                     IMapFromAttribute mapFromAttribute, string propName,
                                                     bool isRecursiveProperty, IPropertyValueGetter propertyValueGetter,
                                                     bool concatenateToExistingValue, string concatenationSeperator,
                                                     bool coalesceWithExistingValue)
        {
            var value = GetPropertyValue(contentToMapFrom, propertyValueGetter, propName, isRecursiveProperty);

            // If mapping to a string, we should manipulate the mapped value to make use of the concatenation and coalescing 
            // settings, if provided.
            // So we need to save the current value.
            var currentStringValue = string.Empty;
            if (AreMappingToString(property))
            {
                currentStringValue = GetStringValueOrEmpty(model, property);
            }
            
            mapFromAttribute.SetPropertyValue(value, property, model, this);

            if (!AreMappingToString(property))
            {
                return;
            }

            SetStringValueFromMapFromAttribute(model, property, concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue, currentStringValue);
        }

        /// <summary>
        /// Helper to check if particular property has a provided CustomMapping
        /// </summary>
        /// <param name="propertyMappings">Dictionary of mapping convention overrides</param>
        /// <param name="propName">Name of property to map to</param>
        /// <param name="customMapping">Provided custom mapping returned via out parameter</param>
        /// <returns>True if mapping should be from child property</returns>
        private static bool HasProvidedCustomMapping(IReadOnlyDictionary<string, PropertyMapping> propertyMappings, 
                                                     string propName, 
                                                     out CustomMapping customMapping)
        {
            if (propertyMappings.ContainsKey(propName) && propertyMappings[propName].CustomMapping != null)
            {
                customMapping = propertyMappings[propName].CustomMapping;
                return true;
            }

            customMapping = null;
            return false;
        }

        /// <summary>
        /// Helper method to set a property value using a CustomMapping
        /// </summary>
        /// <typeparam name="T">Type of view model</typeparam>
        /// <param name="model">Instance of view model</param>
        /// <param name="property">Property of view model to map to</param>
        /// <param name="contentToMapFrom">IPublished content to map from</param>
        /// <param name="customMapping">Instance of <see cref="CustomMapping"/> to use for mapping</param>
        /// <param name="propName">Name of property to map to</param>
        /// <param name="isRecursiveProperty">Flag for whether to map property recurisively</param>
        private void SetValueFromCustomMapping<T>(T model, PropertyInfo property, IPublishedContent contentToMapFrom,
                                                  CustomMapping customMapping, string propName, bool isRecursiveProperty)
        {
            var value = customMapping(this, contentToMapFrom, propName, isRecursiveProperty);
            if (value != null)
            {
                property.SetValue(model, value);
            }
        }

        /// <summary>
        /// Helper method to get the type to use to retrieve property values
        /// </summary>
        /// <param name="propName">Name of property to map to</param>
        /// <param name="propertyMappings">Set of property mappings, for use when convention mapping based on name is not sufficient</param>
        /// <returns>Name of property to map from</returns>
        private IPropertyValueGetter GetPropertyValueGetter(string propName, IReadOnlyDictionary<string, PropertyMapping> propertyMappings)
        {
            if (!propertyMappings.ContainsKey(propName) || propertyMappings[propName].PropertyValueGetter == null)
            {
                return DefaultPropertyValueGetter;
            }

            var propertyValueGetterType = propertyMappings[propName].PropertyValueGetter;
            if (!typeof(IPropertyValueGetter).IsAssignableFrom(propertyValueGetterType))
            {
                throw new InvalidOperationException(
                    $"The type provided as the PropertyValueGetter for the property {propName} must implement IPropertyValueGetter");
            }

            return Activator.CreateInstance(propertyValueGetterType) as IPropertyValueGetter;
        }

        /// <summary>
        /// Wrapper for retrieving a property value to allow override of the method used instead of the standard GetPropertyValue
        /// </summary>
        /// <param name="content">IPublished content to map from</param>
        /// <param name="propertyValueGetter">Type implementing <see cref="IPropertyValueGetter"/> with method to get property value</param>
        /// <param name="propName">Property alias</param>
        /// <param name="recursive">Flag for whether property should be recursively mapped</param>
        /// <returns>Property value</returns>
        private static object GetPropertyValue(IPublishedContent content, 
                                               IPropertyValueGetter propertyValueGetter, 
                                               string propName, 
                                               bool recursive = false)
        {
            return propertyValueGetter.GetPropertyValue(content, propName, null, null, recursive.ToRecuriveFallback());
        }

        /// <summary>
        /// Helper to check if a mapping conditional applies
        /// </summary>
        /// <param name="contentToMapFrom">IPublished content to map from</param>
        /// <param name="propertyValueGetter">Type implementing <see cref="IPropertyValueGetter"/> with method to get property value</param>
        /// <param name="mapIfPropertyMatches">Property alias and value to match</param>
        /// <returns>True if mapping condition is met</returns>
        private static bool IsMappingConditionMet(IPublishedContent contentToMapFrom, 
                                                  IPropertyValueGetter propertyValueGetter, 
                                                  KeyValuePair<string, string> mapIfPropertyMatches)
        {
            var conditionalPropertyAlias = mapIfPropertyMatches.Key;
            var conditionalPropertyValue = GetPropertyValue(contentToMapFrom, propertyValueGetter, conditionalPropertyAlias, false);
            return conditionalPropertyValue != null && 
                string.Equals(conditionalPropertyValue.ToString(), mapIfPropertyMatches.Value, StringComparison.InvariantCultureIgnoreCase);
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

            MapNativeContentPropertyValue(model, property, 
                concatenateToExistingValue, concatenationSeperator, coalesceWithExistingValue, 
                stringValueFormatter, value);
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
#pragma warning disable S1144 // Unused private types or members should be removed (as is used, via refelection call)
        private IUmbracoMapper MapCollectionOfIPublishedContent<T>(IEnumerable<IPublishedContent> contentCollection,
                                                                   IList<T> modelCollection,
                                                                   Dictionary<string, PropertyMapping> propertyMappings) where T : class, new()
        {
            return MapCollection<T>(contentCollection, modelCollection, propertyMappings);
        }
#pragma warning restore S1144 // Unused private types or members should be removedB

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
#pragma warning disable S1144 // Unused private types or members should be removed (as is used, via refelection call)
        private IUmbracoMapper MapIPublishedContent<T>(IPublishedContent content, T model) where T : class, new()
        {
            return Map<T>(content, model);
        }
#pragma warning restore S1144 // Unused private types or members should be removed

        /// <summary>
        /// Helper to set the value of a property from a dictionary key value
        /// </summary>
        /// <typeparam name="T">View model type</typeparam>
        /// <param name="model">View model to map to</param>
        /// <param name="property">Property of view model to map to</param>
        /// <param name="dictionaryKey">Dictionary key</param>
        private static void SetValueFromDictionary<T>(T model, PropertyInfo property, string dictionaryKey) where T : class
        {
            property.SetValue(model, Current.UmbracoHelper.GetDictionaryValue(dictionaryKey));
        }
    }
}
