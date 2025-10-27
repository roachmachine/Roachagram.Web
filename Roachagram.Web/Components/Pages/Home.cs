using Microsoft.AspNetCore.Components;
using Roachagram.Web.Helpers;
using Roachagram.Web.Services;

namespace Roachagram.Web.Components.Pages
{
    public partial class Home
    {
        /// <summary>
        /// The default placeholder text displayed in the input field.
        /// </summary>
        private const string INPUT_PLACEHOLDER_TEXT_DEFAULT = "Enter words (max 20 chars)";

        /// <summary>
        /// The default text displayed on the submit button.
        /// </summary>
        private const string BUTTON_TEXT_DEFAULT = "Go!";

        // Injected HttpClient used as a fallback when IRoachagramAPIService isn't provided.
        [Inject] public HttpClient Http { get; set; } = default!;

        // Optional higher-level service that encapsulates API calls.
        [Inject] public IRoachagramAPIService? RoachagramApiService { get; set; }

        // Bound to the input field in the UI.
        protected string Input { get; set; } = string.Empty;

        // Raw (escaped) response that will be progressively revealed by the typewriter.
        protected string RoachagramResponse { get; set; } = string.Empty;

        // UI flag to indicate an in-flight request / loading state.
        protected bool IsLoading { get; set; }

        // Text that is progressively revealed by the typewriter effect.
        private string DisplayText { get; set; } = string.Empty;

        // Cancellation token source used to cancel any ongoing typing animation.
        private CancellationTokenSource? _typingCts;

        // Base speed in milliseconds per character; tuned small here for snappy display.
        private int BaseDelayMs { get; } = 1;

        /// <summary>
        /// Gets or sets the placeholder text for the input field.
        /// </summary>
        private string PlaceholderText { get; set; } = INPUT_PLACEHOLDER_TEXT_DEFAULT;

        /// <summary>
        /// Gets or sets the text displayed on the submit button.
        /// </summary>
        private string ButtonText { get; set; } = BUTTON_TEXT_DEFAULT;

        /// <summary>
        /// Handle form submission: validate input, call API (via service or HttpClient),
        /// sanitize and format the result, then start the typewriter animation.
        /// </summary>
        protected async Task OnSubmit()
        {
            var inputText = Input ?? string.Empty;

            // Do not proceed with empty or whitespace-only input.
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return;
            }

            // Enter loading state and clear previous response/input for the UI.
            IsLoading = true;
            RoachagramResponse = string.Empty;
            Input = string.Empty;
            PlaceholderText = $"Anagramming {inputText}...";
            ButtonText = "...";
            StateHasChanged();

            try
            {
                string result;

                // Prefer the injected high-level API service if available.
                if (RoachagramApiService != null)
                {
                    result = await RoachagramApiService.GetAnagramsAsync(inputText);
                }
                else
                {
                    // Fallback to direct HttpClient call to the API endpoint.
                    var endpoint = $"api/anagram?input={Uri.EscapeDataString(inputText)}";
                    var response = await Http.GetAsync(endpoint);
                    response.EnsureSuccessStatusCode();
                    result = await response.Content.ReadAsStringAsync();
                }

                // Apply a series of text transformations and sanitization:
                // - Decode any API-specific encodings
                // - Convert markdown-style bold to HTML <b>
                // - Bold section headings after hashes
                // - Reinsert the original input text where appropriate
                // - Sanitize to remove unsafe HTML/script content
                result = TextFormatHelper.DecodeApiString(result);
                result = TextFormatHelper.ReplaceMarkdownBoldWithHtmlBold(result);
                result = TextFormatHelper.BoldSectionAfterHashes(result);
                result = TextFormatHelper.GetOriginalTextInput(inputText, result);
                result = HtmlSanitizerService.Sanitize(result);

                // Escape backticks and script closing tags to avoid JS/template injection when rendered.
                var escaped = result.Replace("`", "\\`").Replace("</script>", "<\\/script>");

                RoachagramResponse = escaped;

                IsLoading = false;

                try
                {
                    // Cancel any existing typing animation and start a fresh one.
                    _typingCts?.Cancel();
                    _typingCts?.Dispose();
                    _typingCts = new CancellationTokenSource();
                    await StartTypewriterAsync(RoachagramResponse, _typingCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Typing was canceled by a newer request; this is expected so ignore.
                }
            }
            catch
            {
                // Generic fallback message on any error while fetching or processing the anagrams.
                var fallback = "An error occurred while fetching anagrams. Please try again.";
                RoachagramResponse = $"{fallback}";
            }
            finally
            {
                // Ensure UI state is reset regardless of success/failure.
                PlaceholderText = INPUT_PLACEHOLDER_TEXT_DEFAULT;
                ButtonText = BUTTON_TEXT_DEFAULT;
                IsLoading = false;
                Input = string.Empty;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Reveal the provided HTML (assumed sanitized) progressively.
        /// Treats HTML tags as atomic units so tags are not split by the animation.
        /// </summary>
        private async Task StartTypewriterAsync(string fullText, CancellationToken token)
        {
            DisplayText = string.Empty;

            // Iterate over the full text; when encountering an HTML tag ('<'), copy the full tag at once.
            for (int i = 0; i < fullText.Length; i++)
            {
                token.ThrowIfCancellationRequested();

                char c = fullText[i];

                if (c == '<')
                {
                    // Find the next '>' and copy the whole tag to DisplayText to avoid breaking tags.
                    int tagEnd = fullText.IndexOf('>', i);
                    if (tagEnd == -1)
                    {
                        // If there's no matching '>', append the rest of the text and exit.
                        DisplayText += fullText[i..];
                        i = fullText.Length; // exit loop
                        await InvokeAsync(StateHasChanged);
                        break;
                    }
                    else
                    {
                        string tag = fullText.Substring(i, tagEnd - i + 1);
                        DisplayText += tag;
                        i = tagEnd;
                        await InvokeAsync(StateHasChanged);

                        // Small pause after rendering a full tag for better pacing.
                        await Task.Delay(BaseDelayMs * 2, token);
                        continue;
                    }
                }

                // Append a single visible character and re-render.
                DisplayText += c;
                await InvokeAsync(StateHasChanged);

                // Apply variable delay: longer for punctuation and newlines for natural pacing.
                int delay = BaseDelayMs;
                if (c == '.' || c == '!' || c == '?') delay *= 8;
                if (c == ',' || c == ';' || c == ':') delay *= 4;
                if (c == '\n' || c == '\r') delay *= 6;

                await Task.Delay(delay, token);
            }

            // Ensure the entire text is shown at the end (in case of timing/cancellation subtlety).
            DisplayText = fullText;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called by the Blazor lifecycle / DI container when the component is disposed.
        /// Cancel and dispose the typing cancellation token source to free resources.
        /// </summary>
        public void Dispose()
        {
            _typingCts?.Cancel();
            _typingCts?.Dispose();
        }
    }
}
