using Ganss.Xss;

namespace Roachagram.Web.Services
{
    public static class HtmlSanitizerService
    {
        private static readonly HtmlSanitizer _sanitizer = new();

        // Optional: tune allowed tags/attrs:
        // _sanitizer.AllowedTags.Add("span");
        // _sanitizer.AllowedAttributes.Add("class");

        public static string Sanitize(string html) => _sanitizer.Sanitize(html ?? string.Empty);
    }
}