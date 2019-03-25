namespace Zone.UmbracoMapper.Common.Extensions
{
    using System.Reflection;

    public static class TypeExtensions
    {
        public static MethodInfo GetMethodFromTypeAndMethodName(this IReflect type, string methodName)
        {
            return type.GetMethod(methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
