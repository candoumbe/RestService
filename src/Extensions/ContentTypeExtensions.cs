

namespace RestService
{
    /// <summary>
    /// Defines extension methods for the ContentType enum
    /// </summary>
    public static class ContentTypeExtensions
    {
        /// <summary>
        /// Gets the media format for the content type
        /// </summary>
        /// <param name="contentType">The content type item</param>
        public static string ToMediaFormat(this ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.Xml:
                    return "application/xml";
                case ContentType.Json:
                default:
                    return "application/json";
            }
        }
    }
}
