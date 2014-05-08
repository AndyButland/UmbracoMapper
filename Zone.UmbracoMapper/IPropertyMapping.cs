namespace Zone.UmbracoMapper
{
    public interface IPropertyMapping
    {
        string SourceProperty { get; set; }

        int LevelsAbove { get; set; }
    }
}
