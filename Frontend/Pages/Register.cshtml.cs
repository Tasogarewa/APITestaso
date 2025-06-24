using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class RegisterModel : PageModel
{
    private readonly HttpClient _httpClient;

    public RegisterModel(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("BackendApi");
    }

    [BindProperty]
    public RegisterInput Input { get; set; }

    public string ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            return Page();
        }

        var response = await _httpClient.PostAsJsonAsync("/api/account/register", new
        {
            Email = Input.Email,
            FullName = Input.FullName,
            Password = Input.Password
        });

        if (response.IsSuccessStatusCode)
        {
            return RedirectToPage("/Login");
        }

        var errors = await response.Content.ReadAsStringAsync();
        ErrorMessage = errors;
        return Page();
    }
}

public class RegisterInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string FullName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }
}