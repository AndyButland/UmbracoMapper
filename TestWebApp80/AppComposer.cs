namespace TestWebApp80
{
    using Umbraco.Core;
    using Umbraco.Core.Composing;
    using Zone.UmbracoMapper.V8;

    public class AppComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Register<IUmbracoMapper, UmbracoMapper>();
        }
    }
}