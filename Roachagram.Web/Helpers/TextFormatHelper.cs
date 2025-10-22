using System.Net;
using System.Text.RegularExpressions;

namespace Roachagram.Web.Helpers
{
    /// <summary>
    /// Provides utility methods to format and decode text received from external sources (APIs, markdown, etc.).
    /// Methods perform lightweight transformations such as unescaping unicode sequences, decoding HTML entities,
    /// converting markdown-style bold markers to HTML bold tags, capitalizing words inside quotes, and converting
    /// hash-prefixed sections to bold HTML.
    /// </summary>
    public static class TextFormatHelper
    {
        /// <summary>
        /// Decodes a raw string returned by an API by:
        /// 1. Unescaping Unicode escape sequences (using <see cref="Regex.Unescape(string)"/>).
        /// 2. Decoding HTML entities (using <see cref="WebUtility.HtmlDecode(string)"/>).
        /// 3. Converting newline characters ('\n') to HTML line breaks ("&lt;br&gt;").
        /// </summary>
        /// <param name="raw">The raw input string from the API. This value must not be <c>null</c>.</param>
        /// <returns>
        /// A string containing HTML-friendly content where unicode escapes and HTML entities are decoded
        /// and newline characters are replaced with "&lt;br&gt;".
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="raw"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The method intentionally performs a simple transformation pipeline and does not sanitize HTML beyond decoding entities.
        /// If the caller needs to prevent HTML injection, perform additional sanitization after calling this method.
        /// </remarks>
        public static string DecodeApiString(string raw)
        {
            // Step 1: Decode Unicode escape sequences
            string unicodeDecoded = Regex.Unescape(raw);

            // Step 2: Decode HTML entities
            string htmlDecoded = WebUtility.HtmlDecode(unicodeDecoded);

            // Step 3: Replace \n with <br> or wrap in <p> tags
            string formatted = htmlDecoded.Replace("\n", "<br>");
        
            return formatted;
        }

        /// <summary>
        /// Replaces markdown bold markers of the form <c>**text**</c> with HTML bold tags.
        /// Each word inside the bold markers is capitalized (first letter uppercase, rest lowercase).
        /// Example: <c>"this is **bold text** example"</c> -> <c>"this is &lt;b&gt;Bold Text&lt;/b&gt; example"</c>.
        /// </summary>
        /// <param name="input">The input string that may contain markdown-style bold sections. Must not be <c>null</c>.</param>
        /// <returns>The transformed string with markdown bold replaced by HTML &lt;b&gt; elements and words capitalized inside them.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The regex pattern used is <c>\*\*(.+?)\*\*</c> which matches the smallest content between pairs of double asterisks.
        /// If nested or malformed markdown is present, behavior follows the regex matching rules.
        /// </remarks>
        public static string ReplaceMarkdownBoldWithHtmlBold(string input)
        {
            // Replace **example** with <b>Example</b> (capitalize each word in bold)
            return Regex.Replace(input, @"\*\*(.+?)\*\*", match =>
            {
                string content = match.Groups[1].Value;
                string capitalizedContent = string.Join(" ", content.Split(' ')
                    .Select(word => char.ToUpper(word[0]) + word[1..].ToLower()));
                return $"<b>{capitalizedContent}</b>";
            });
        }

        /// <summary>
        /// Capitalizes each word contained within double quotes in the provided input.
        /// Example: <c>He said "hello world"</c> -> <c>He said "Hello World"</c>.
        /// </summary>
        /// <param name="input">The input string that may contain quoted segments. Must not be <c>null</c>.</param>
        /// <returns>The input string with each word inside double quotes capitalized.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The method targets double-quoted regions only and capitalizes words separated by spaces.
        /// Punctuation adjacent to words is preserved as-is.
        /// </remarks>
        public static string CapitalizeWordsInQuotes(string input)
        {
            // Match content within double quotes and capitalize each word
            return Regex.Replace(input, "\"(.*?)\"", match =>
            {
                string content = match.Groups[1].Value;
                string capitalizedContent = string.Join(" ", content.Split(' ')
                    .Select(word => char.ToUpper(word[0]) + word[1..].ToLower()));
                return $"\"{capitalizedContent}\"";
            });
        }

        /// <summary>
        /// Converts lines that start with three hashes (<c>###</c>) followed by a phrase that ends with a colon
        /// into a bolded phrase using HTML <c>&lt;b&gt;</c> tags.
        /// Example:
        /// <code>
        /// ### Section Title:
        /// Remaining text...
        /// </code>
        /// becomes
        /// <code>
        /// &lt;b&gt;Section Title:&lt;/b&gt;
        /// Remaining text...
        /// </code>
        /// </summary>
        /// <param name="input">The input text to process. Each line is evaluated independently. Must not be <c>null</c>.</param>
        /// <returns>The transformed text where matched section headers are wrapped with <c>&lt;b&gt;</c> tags.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The regex uses the <see cref="RegexOptions.Multiline"/> option so that the caret (^) anchors to the start of each line.
        /// The pattern <c>^###\s*([^\r\n:]+:)</c> captures the phrase up to and including the colon and excludes newline characters.
        /// </remarks>
        public static string BoldSectionAfterHashes(string input)
        {
            // Replace lines starting with ### and a phrase (ending with a colon) with bolded phrase
            return Regex.Replace(input, @"^###\s*([^\r\n:]+:)", m => $"<b>{m.Groups[1].Value}</b>", RegexOptions.Multiline);
        }

        /// <summary>
        /// Restores the original input by removing any single quotes.
        /// </summary>
        public static string RemoveSingleQuotes(string input)
        {
            return input.Replace("'", "");
        }

        /// <summary>
        /// Restores an original input fragment inside an output string that may have had its spaces removed.
        /// The method generates a compact lookup token by removing spaces from <paramref name="input"/> and trimming it,
        /// then replaces occurrences of that token in <paramref name="output"/> with the original <paramref name="input"/>.
        /// </summary>
        /// <param name="input">The original input fragment (possibly containing spaces). Must not be <c>null</c>.</param>
        /// <param name="output">The output string in which a compacted version of <paramref name="input"/> may appear. Must not be <c>null</c>.</param>
        /// <returns>
        /// A new string where occurrences of the compacted <paramref name="input"/> in <paramref name="output"/>
        /// are replaced with the original <paramref name="input"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> or <paramref name="output"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This is a best-effort replacement that removes all spaces from <paramref name="input"/> before searching.
        /// It performs a simple string replacement and does not attempt to match word boundaries or perform case-insensitive matching.
        /// </remarks>
        public static string GetOriginalTextInput(string input, string output)
        {
            string lookupText = input.Replace(" ", "").Trim();
            return output.Replace(lookupText, input);
        }
    }
}
