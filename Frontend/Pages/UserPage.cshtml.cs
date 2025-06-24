using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Frontend.Pages
{
    public class UserPageModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UserPageModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public List<ApiTestDto> UserApiTests { get; set; } = new();
        public List<SqlTestDto> UserSqlTests { get; set; } = new();
        public List<ApiTestScenarioDto> UserApiTestScenarios { get; set; } = new();
        public List<TestResultDto> UserTestResults { get; set; } = new();
        public string LoadErrorMessage { get; set; } = string.Empty;

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

            if (CurrentUser == null)
            {
                ModelState.AddModelError(string.Empty, "Не вдалося десеріалізувати дані користувача.");
                return Page();
            }

            await LoadUserApiTestsAsync(client);
            await LoadUserSqlTestsAsync(client);
            await LoadUserApiTestScenariosAsync(client);
            await LoadUserTestResultsAsync(client);

            if (!string.IsNullOrEmpty(LoadErrorMessage))
            {
                ModelState.AddModelError(string.Empty, LoadErrorMessage);
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
                    var allTests = JsonSerializer.Deserialize<List<ApiTestDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ApiTestDto>();

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
                    var allTests = JsonSerializer.Deserialize<List<SqlTestDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<SqlTestDto>();

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

        private async Task LoadUserTestResultsAsync(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync($"api/TestResults?userId={CurrentUser?.Id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var allResults = JsonSerializer.Deserialize<List<TestResultDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<TestResultDto>();

                    UserTestResults = allResults;
                }
                else
                {
                    LoadErrorMessage += $"Не вдалося завантажити результати тестів: {(int)response.StatusCode} {response.ReasonPhrase}\n";
                }
            }
            catch (Exception ex)
            {
                LoadErrorMessage += $"Помилка при завантаженні результатів тестів: {ex.Message}\n";
            }
        }

        private HttpClient CreateClientWithAuth()
        {
            var client = _httpClientFactory.CreateClient("BackendApi");

            if (Request.Cookies.TryGetValue("JwtToken", out var token) && !string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }
    }
}