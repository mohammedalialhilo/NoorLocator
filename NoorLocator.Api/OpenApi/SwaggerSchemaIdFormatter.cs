namespace NoorLocator.Api.OpenApi;

internal static class SwaggerSchemaIdFormatter
{
    public static string Format(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name.Replace("+", ".");
        }

        var genericTypeName = type.Name[..type.Name.IndexOf('`')];
        var genericArguments = type.GetGenericArguments()
            .Select(Format);

        return $"{genericTypeName}Of{string.Join("And", genericArguments)}";
    }
}
