using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Frontend.Pages
{
    public class MainPageModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MainPageModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public List<ApiTest> UserApiTests { get; set; } = new();
        public List<SqlTest> UserSqlTests { get; set; } = new();
        public List<ApiTestScenarioDto> UserApiTestScenarios { get; set; } = new();
        public string LoadErrorMessage { get; set; } = string.Empty;
        public string ApiResponse { get; set; } = string.Empty;

        [BindProperty]
        public ApiTest CurrentApiTest { get; set; } = new();

        [BindProperty]
        public string BodyJsonString { get; set; } = string.Empty;

        [BindProperty]
        public string HeadersJson { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var client = CreateClientWithAuth();

            var response = await client.GetAsync("api/account/me");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return RedirectToPage("/Login");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Не вдалося отримати інформацію про користувача.");
                return Page();
            }

            var json = await response.Content.ReadAsStringAsync();
            CurrentUser = JsonSerializer.Deserialize<ApplicationUser>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            await LoadUserApiTestsAsync(client);
            await LoadUserSqlTestsAsync(client);
            await LoadUserApiTestScenariosAsync(client);

            return Page();
        }

        public async Task<IActionResult> OnGetLoadTestAsync(int id)
        {
            var client = CreateClientWithAuth();
            var response = await client.GetAsync($"api/ApiTests/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            return new JsonResult(json);
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            var client = CreateClientWithAuth();
            await LoadUserApiTestsAsync(client);

            switch (action)
            {
                case "delete":
                    return await DeleteApiTestAsync(client);
                default:
                    ApiResponse = "Невідома дія.";
                    return Page();
            }
        }

        [HttpPost]
        public async Task<IActionResult> OnPostSaveTestAsync()
        {
            var client = CreateClientWithAuth();

            Dictionary<string, string>? headers = null;
            object? bodyObj = null;

            if (!string.IsNullOrWhiteSpace(HeadersJson))
            {
                try
                {
                    headers = JsonSerializer.Deserialize<Dictionary<string, string>>(HeadersJson);
                }
                catch
                {
                    ApiResponse = "Помилка парсингу заголовків.";
                    return Page();
                }
            }

            if (!string.IsNullOrWhiteSpace(BodyJsonString))
            {
                try
                {
                    bodyObj = JsonSerializer.Deserialize<object>(BodyJsonString);
                }
                catch
                {
                    ApiResponse = "Помилка парсингу JSON тіла запиту.";
                    return Page();
                }
            }

            CurrentApiTest.Headers = headers;
            CurrentApiTest.BodyJson = bodyObj;
            CurrentApiTest.CreatedByUserId = CurrentUser?.Id;

            try
            {
                var response = await client.PostAsJsonAsync("api/ApiTests", CurrentApiTest);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
                    return new JsonResult(new { success = true, id = data?["id"] });
                }
                ApiResponse = $"Помилка збереження: {(int)response.StatusCode} {response.ReasonPhrase}";
            }
            catch (Exception ex)
            {
                ApiResponse = $"Виняток: {ex.Message}";
            }

            return new JsonResult(new { success = false, message = ApiResponse });
        }

        [HttpPut]
        public async Task<IActionResult> OnPostUpdateTestAsync()
        {
            var client = CreateClientWithAuth();

            Dictionary<string, string>? headers = null;
            object? bodyObj = null;

            if (!string.IsNullOrWhiteSpace(HeadersJson))
            {
                try
                {
                    headers = JsonSerializer.Deserialize<Dictionary<string, string>>(HeadersJson);
                }
                catch
                {
                    ApiResponse = "Помилка парсингу заголовків.";
                    return Page();
                }
            }

            if (!string.IsNullOrWhiteSpace(BodyJsonString))
            {
                try
                {
                    bodyObj = JsonSerializer.Deserialize<object>(BodyJsonString);
                }
                catch
                {
                    ApiResponse = "Помилка парсингу JSON тіла запиту.";
                    return Page();
                }
            }

            CurrentApiTest.Headers = headers;
            CurrentApiTest.BodyJson = bodyObj;
            CurrentApiTest.CreatedByUserId = CurrentUser?.Id;

            try
            {
                var response = await client.PutAsJsonAsync($"api/ApiTests/{CurrentApiTest.Id}", CurrentApiTest);
                if (response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { success = true });
                }
                ApiResponse = $"Помилка оновлення: {(int)response.StatusCode} {response.ReasonPhrase}";
            }
            catch (Exception ex)
            {
                ApiResponse = $"Виняток: {ex.Message}";
            }

            return new JsonResult(new { success = false, message = ApiResponse });
        }

        private async Task<IActionResult> DeleteApiTestAsync(HttpClient client)
        {
            if (CurrentApiTest.Id == 0)
            {
                ApiResponse = "Не вибрано тест для видалення.";
                return Page();
            }

            try
            {
                var response = await client.DeleteAsync($"api/ApiTests/{CurrentApiTest.Id}");
                if (response.IsSuccessStatusCode)
                {
                    ApiResponse = "Тест успішно видалено.";
                    CurrentApiTest = new ApiTest();
                    await LoadUserApiTestsAsync(client);
                    return Page();
                }
                else
                {
                    ApiResponse = $"Помилка видалення: {(int)response.StatusCode} {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                ApiResponse = $"Виняток при видаленні: {ex.Message}";
            }

            return Page();
        }

        private async Task LoadUserApiTestsAsync(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync("api/ApiTests");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var allTests = JsonSerializer.Deserialize<List<ApiTest>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ApiTest>();

                    UserApiTests = allTests.Where(t => t.CreatedByUserId == CurrentUser?.Id).ToList();
                }
                else
                {
                    LoadErrorMessage += $"Не вдалося завантажити API тести: {(int)response.StatusCode} {response.ReasonPhrase}\n";
                }
            }
            catch (Exception ex)
            {
                LoadErrorMessage += $"Помилка при завантаженні API тестів: {ex.Message}\n";
            }
        }

        private async Task LoadUserSqlTestsAsync(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync("api/SqlTests");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var allTests = JsonSerializer.Deserialize<List<SqlTest>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<SqlTest>();

                    UserSqlTests = allTests.Where(t => t.CreatedByUserId == CurrentUser?.Id).ToList();
                }
                else
                {
                    LoadErrorMessage += $"Не вдалося завантажити SQL тести: {(int)response.StatusCode} {response.ReasonPhrase}\n";
                }
            }
            catch (Exception ex)
            {
                LoadErrorMessage += $"Помилка при завантаженні SQL тестів: {ex.Message}\n";
            }
        }

        private async Task LoadUserApiTestScenariosAsync(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync("api/ApiTestScenarios");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var allScenarios = JsonSerializer.Deserialize<List<ApiTestScenarioDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ApiTestScenarioDto>();

                    UserApiTestScenarios = allScenarios.Where(s => s.CreatedByUserId == CurrentUser?.Id).ToList();

                  
                    var allTestIds = UserApiTestScenarios.SelectMany(s => s.TestIds).Distinct().ToList();
                    if (allTestIds.Any())
                    {
                        var missingTestIds = allTestIds.Except(UserApiTests.Select(t => t.Id)).ToList();
                        if (missingTestIds.Any())
                        {
                            var testsResponse = await client.GetAsync("api/ApiTests");
                            if (testsResponse.IsSuccessStatusCode)
                            {
                                var testsJson = await testsResponse.Content.ReadAsStringAsync();
                                var allTests = JsonSerializer.Deserialize<List<ApiTest>>(testsJson, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                }) ?? new List<ApiTest>();
                                UserApiTests.AddRange(allTests.Where(t => missingTestIds.Contains(t.Id) && t.CreatedByUserId == CurrentUser?.Id));
                            }
                        }
                    }
                }
                else
                {
                    LoadErrorMessage += $"Не вдалося завантажити сценарії API тестів: {(int)response.StatusCode} {response.ReasonPhrase}\n";
                }
            }
            catch (Exception ex)
            {
                LoadErrorMessage += $"Помилка при завантаженні сценаріїв: {ex.Message}\n";
            }
        }

        private HttpClient CreateClientWithAuth()
        {
            var client = _httpClientFactory.CreateClient("BackendApi");

            if (Request.Cookies.TryGetValue("AccessToken", out var token) && !string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"Додано заголовок Authorization: Bearer {token}");
            }
            else
            {
                Console.WriteLine("AccessToken не знайдено в кукі");
            }

            return client;
        }
    }

}