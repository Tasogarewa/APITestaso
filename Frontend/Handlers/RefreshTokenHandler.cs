using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Frontend.Handlers
{
    public class RefreshTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly object _lock = new object();
        private static bool _isRefreshing = false;

      

            public RefreshTokenHandler(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
            {
                _httpContextAccessor = httpContextAccessor;
                _httpClientFactory = httpClientFactory;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var context = _httpContextAccessor.HttpContext;
                var accessToken = context?.Request.Cookies["AccessToken"];
                var refreshToken = context?.Request.Cookies["RefreshToken"];

                Console.WriteLine($"AccessToken: {accessToken}, RefreshToken: {refreshToken}");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    Console.WriteLine($"Додано заголовок Authorization: Bearer {accessToken}");
                }
                else
                {
                    Console.WriteLine("AccessToken не знайдено, запит відправлено без токена");
                }

                var response = await base.SendAsync(request, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    if (string.IsNullOrEmpty(refreshToken))
                    {
                        Console.WriteLine("RefreshToken не знайдено, повертаємо 401");
                        return response;
                    }

                    lock (_lock)
                    {
                        if (_isRefreshing)
                        {
                            Console.WriteLine("Оновлення токена вже виконується, повертаємо 401");
                            return response;
                        }
                        _isRefreshing = true;
                    }

                    try
                    {
                        var newTokens = await RefreshTokensAsync(refreshToken);
                        if (newTokens != null)
                        {
                            await StoreTokensAsync(newTokens);
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
                            Console.WriteLine($"Оновлено токен, повторюємо запит із новим AccessToken: {newTokens.AccessToken}");
                            response.Dispose();
                            response = await base.SendAsync(request, cancellationToken);
                        }
                        else
                        {
                            Console.WriteLine("Не вдалося оновити токен");
                        }
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _isRefreshing = false;
                        }
                    }
                }

                return response;
            }

            private async Task<TokenResponse?> RefreshTokensAsync(string refreshToken)
            {
                var client = _httpClientFactory.CreateClient("BackendApi");
                var refreshEndpoint = "https://localhost:7200/api/account/refresh-token";

                var refreshRequest = new HttpRequestMessage(HttpMethod.Post, refreshEndpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { RefreshToken = refreshToken }), System.Text.Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(refreshRequest);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Помилка оновлення токена: {(int)response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine($"Оновлено токени: accessToken={tokenResponse?.AccessToken}, refreshToken={tokenResponse?.RefreshToken}");
                return tokenResponse;
            }

            private async Task StoreTokensAsync(TokenResponse tokens)
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null) return;

            context.Response.Cookies.Append("AccessToken", tokens.AccessToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn)
            });
            context.Response.Cookies.Append("RefreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.None, 
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            Console.WriteLine("Збережено нові кукі: AccessToken і RefreshToken");
            await Task.CompletedTask;

        }
    }

    }


    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
