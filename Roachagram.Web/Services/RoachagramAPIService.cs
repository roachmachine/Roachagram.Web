using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Roachagram.Web.Services
{
    /// <summary>
    /// Service class for interacting with the Roachagram API.
    /// Provides methods to fetch anagrams and manage device-specific identifiers.
    /// Device identifier is set to the user's IP address (considering X-Forwarded-For).
    /// </summary>
    public class RoachagramAPIService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : IRoachagramAPIService
    {
        // HttpClient instance used for making API requests.
        private readonly HttpClient _httpClient = httpClient;

        // Base URL for the API, retrieved from the configuration.
        private readonly string _apiBaseUrl = configuration["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing.");

        // HttpContextAccessor to retrieve the client's IP address.
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        /// <summary>
        /// Fetches anagrams for the given input string from the API.
        /// Uses the client's IP address as the device UUID (header "X-Device-ID").
        /// </summary>
        /// <param name="input">The input string for which anagrams are to be fetched.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response as a string.</returns>
        /// <exception cref="ArgumentException">Thrown when the input is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
        public async Task<string> GetAnagramsAsync(string input)
        {
            try
            {
                // Retrieve the client's IP address to use as device UUID.
                var device_uuid = GetClientIpAddress();

                // Ensure the "X-Device-ID" header is set with the current device UUID.
                if (_httpClient.DefaultRequestHeaders.Contains("X-Device-ID"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("X-Device-ID");
                }
                _httpClient.DefaultRequestHeaders.Add("X-Device-ID", device_uuid);

                // Validate the input string.
                if (string.IsNullOrWhiteSpace(input))
                    throw new ArgumentException("Input cannot be null or empty.", nameof(input));

                // Construct the API endpoint URL.
                var endpoint = $"{_apiBaseUrl}api/anagram?input={Uri.EscapeDataString(input)}";

                // Make the GET request to the API.
                var response = await _httpClient.GetAsync(endpoint);

                // Ensure the response indicates success.
                response.EnsureSuccessStatusCode();

                // Return the response content as a string.
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                throw;
            }
        }

        private static string GetClientIpAddress()
        {
            // Return a newly generated GUID as the device identifier.
            return Guid.NewGuid().ToString("D");
        }
    }
}