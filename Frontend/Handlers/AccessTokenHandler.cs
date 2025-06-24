using Microsoft.AspNetCore.Authentication;

namespace Frontend.Handlers
{
    public class AccessTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccessTokenHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var accessToken = _httpContextAccessor.HttpContext?.Request.Cookies["AccessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                Console.WriteLine($"Додано заголовок Authorization: Bearer {accessToken}");
            }
            else
            {
                Console.WriteLine("AccessToken не знайдено в кукі");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }

}
