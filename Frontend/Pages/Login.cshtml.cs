using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LoginModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LoginModel> _logger;
    public LoginModel(IHttpClientFactory httpClientFactory, ILogger<LoginModel> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BackendApi");
        _logger = logger;
    }

    [BindProperty]
    public LoginInput Input { get; set; }

    public string ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public class TokenResponse
    {
        public string accessToken { get; set; } = string.Empty;
        public string refreshToken { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { _logger.LogWarning("�������� ������: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));  return Page(); }

        try
        {
            _logger.LogInformation("��������� ����� ����� ��� email: {Email}", Input.Email);
            var response = await _httpClient.PostAsJsonAsync("api/account/login", new
            {
                Email = Input.Email,
                Password = Input.Password
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("������� �����: {StatusCode}, {Content}", response.StatusCode, errorContent);
                ErrorMessage = "������������ email ��� ������";
                return Page();
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokens == null || string.IsNullOrEmpty(tokens.accessToken) || string.IsNullOrEmpty(tokens.refreshToken))
            {
                _logger.LogError("������ �� �������� � ������: {Response}", JsonSerializer.Serialize(tokens));
                ErrorMessage = "�� ������� �������� ������";
                return Page();
            }

            _logger.LogInformation("������ ��������: accessToken={AccessToken}, refreshToken={RefreshToken}", tokens.accessToken, tokens.refreshToken);

            HttpContext.Response.Cookies.Append("AccessToken", tokens.accessToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

            HttpContext.Response.Cookies.Append("RefreshToken", tokens.refreshToken, new CookieOptions
            {
                HttpOnly = false, // ������ � true
                Secure = true,
                SameSite = SameSiteMode.None, // ������ � Strict
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            _logger.LogInformation("��� �����������: AccessToken � RefreshToken");

            return RedirectToPage("/MainPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "������� �� ��� ����� ��� email: {Email}", Input.Email);
            ErrorMessage = "������� ������� �� ��� �����";
            return Page();
        }

    }
}
    public class LoginInput
{
    [Required(ErrorMessage = "Email � ����'�������")][EmailAddress(ErrorMessage = "������������ ������ email")] public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "������ � ����'�������")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

}