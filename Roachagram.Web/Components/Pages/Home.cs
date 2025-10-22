using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Roachagram.Web.Helpers;
using Roachagram.Web.Services;

namespace Roachagram.Web.Components.Pages
{
    public partial class Home
    {
        [Inject] public HttpClient Http { get; set; } = default!;
        [Inject] public IRoachagramAPIService? RoachagramApiService { get; set; }
        protected string Input { get; set; } = string.Empty;
        protected string RoachagramResponse { get; set; } = string.Empty;
        protected bool IsLoading { get; set; }

        // Text that is progressively revealed by the typewriter
        private string DisplayText { get; set; } = string.Empty;

        private CancellationTokenSource? _typingCts;

        // Configurable speed (milliseconds per character)
        private int BaseDelayMs { get; } = 1;

        protected async Task OnSubmit()
        {
            var inputText = Input ?? string.Empty;

            if (string.IsNullOrWhiteSpace(inputText))
            {
                return;
            }

            IsLoading = true;
            RoachagramResponse = string.Empty;
            Input = string.Empty; 
            StateHasChanged();

            try
            {
                string result;

                if (RoachagramApiService != null)
                {
                    result = await RoachagramApiService.GetAnagramsAsync(inputText);
                }
                else
                {
                    var endpoint = $"api/anagram?input={Uri.EscapeDataString(inputText)}";
                    var response = await Http.GetAsync(endpoint);
                    response.EnsureSuccessStatusCode();
                    result = await response.Content.ReadAsStringAsync();
                }

                result = TextFormatHelper.DecodeApiString(result);
                result = TextFormatHelper.ReplaceMarkdownBoldWithHtmlBold(result);
                result = TextFormatHelper.BoldSectionAfterHashes(result);
                result = TextFormatHelper.GetOriginalTextInput(inputText, result);
                result = HtmlSanitizerService.Sanitize(result);

                var escaped = result.Replace("`", "\\`").Replace("</script>", "<\\/script>");

                RoachagramResponse = escaped;

                IsLoading = false;

                try
                {
                    _typingCts?.Cancel();
                    _typingCts?.Dispose();
                    _typingCts = new CancellationTokenSource();
                    await StartTypewriterAsync(RoachagramResponse, _typingCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Typing was canceled, do nothing
                }
            }
            catch
            {
                var fallback = "An error occurred while fetching anagrams. Please try again.";
                RoachagramResponse = $"{fallback}";
            }
            finally
            {
                IsLoading = false;
                Input = string.Empty;
                StateHasChanged();
            }
        }

        private async Task StartTypewriterAsync(string fullText, CancellationToken token)
        {
            DisplayText = string.Empty;
            // iterate while respecting simple HTML tags: append full tags at once
            for (int i = 0; i < fullText.Length; i++)
            {
                token.ThrowIfCancellationRequested();

                char c = fullText[i];
                if (c == '<')
                {
                    // append until '>' to keep tags intact
                    int tagEnd = fullText.IndexOf('>', i);
                    if (tagEnd == -1)
                    {
                        // no closing bracket, treat rest as text
                        DisplayText += fullText.Substring(i);
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
                        // small pause after a tag
                        await Task.Delay(BaseDelayMs * 2, token);
                        continue;
                    }
                }

                DisplayText += c;
                await InvokeAsync(StateHasChanged);

                // slightly longer pause for punctuation/newlines
                int delay = BaseDelayMs;
                if (c == '.' || c == '!' || c == '?') delay *= 8;
                if (c == ',' || c == ';' || c == ':') delay *= 4;
                if (c == '\n' || c == '\r') delay *= 6;

                await Task.Delay(delay, token);
            }

            // ensure full text is visible at the end
            DisplayText = fullText;
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            _typingCts?.Cancel();
            _typingCts?.Dispose();
        }
    }
}
