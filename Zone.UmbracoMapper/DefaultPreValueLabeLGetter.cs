namespace Zone.UmbracoMapper.V7
{
    using Umbraco.Web;

    public class DefaultPreValueLabeLGetter : IPreValueLabelGetter
    {
        public string GetPreValueAsString(string preValueId)
        {
            int intValue;
            if (!int.TryParse(preValueId, out intValue))
            {
                return string.Empty;
            }

            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            return umbracoHelper.GetPreValueAsString(intValue);
        }
    }
}
