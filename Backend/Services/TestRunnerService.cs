using Backend.AppDbContext;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    public class TestRunnerService
    {
        public readonly ApplicationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        public TestRunnerService(
            ApplicationDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

     
        public async Task<List<TestResult>> RunAllApiTestsAsync(string userId)
        {
            var apiTests = await _dbContext.ApiTests
                .Where(t => t.CreatedByUserId == userId)
                .ToListAsync();

            var results = new List<TestResult>();

            foreach (var test in apiTests)
            {
                var result = await ExecuteApiTestAsync(test, userId);
                results.Add(result);
            }

            await _dbContext.TestResults.AddRangeAsync(results);
            await _dbContext.SaveChangesAsync();

            return results;
        }

   
        public async Task<List<TestResult>> RunAllSqlTestsAsync(string userId)
        {
            var sqlTests = await _dbContext.SqlTests
                .Where(t => t.CreatedByUserId == userId)
                .ToListAsync();

            var results = new List<TestResult>();

            foreach (var test in sqlTests)
            {
                var result = await RunSingleSqlTestAsync(test, userId);
                results.Add(result);
            }

            await _dbContext.TestResults.AddRangeAsync(results);
            await _dbContext.SaveChangesAsync();

            return results;
        }

        private void AddHeadersToClient(HttpClient client, Dictionary<string, string>? headers)
        {
            if (headers == null) return;

            foreach (var header in headers)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = header.Value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue(parts[0], parts[1]);
                    }
                    else
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                else
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }
        private async Task<TestResult> ExecuteApiTestAsync(ApiTest test, string userId)
        {
            var result = new TestResult
            {
                ApiTestId = test.Id,
                ExecutedByUserId = userId,
                ExecutedAt = DateTime.UtcNow
            };
            string finalUrl;

            if (test.IsMock)
            {
                var mockBaseUrl = _configuration["MockServer:PostmanMockBaseUrl"];
                finalUrl = $"{mockBaseUrl.TrimEnd('/')}/{test.Url.Replace("https://localhost:7200", "").TrimStart('/')}";
            }
            else
            {
                finalUrl = test.Url;
            }
            try
            {
                var client = _httpClientFactory.CreateClient();
                AddHeadersToClient(client, test.Headers);
                HttpResponseMessage response;
                var requestBody = test.BodyJson != null
                    ? new StringContent(JsonSerializer.Serialize(test.BodyJson), Encoding.UTF8, "application/json")
                    : null;

                switch (test.Method.ToUpper())
                {
                    case "GET":
                        response = await client.GetAsync(finalUrl);
                        break;

                    case "POST":
                        response = await client.PostAsync(finalUrl, requestBody ?? new StringContent(""));
                        break;

                    case "PUT":
                        response = await client.PutAsync(finalUrl, requestBody ?? new StringContent(""));
                        break;

                    case "DELETE":
                        response = await client.DeleteAsync(finalUrl);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported HTTP method: {test.Method}");
                }

                var respBody = await response.Content.ReadAsStringAsync();
                result.Response = respBody;

                bool statusOk = (int)response.StatusCode == test.ExpectedStatusCode;
                bool bodyOk = string.IsNullOrEmpty(test.ExpectedResponse) || respBody.Contains(test.ExpectedResponse);

                result.IsSuccess = statusOk && bodyOk;
                result.ErrorMessage = result.IsSuccess
                    ? null
                    : $"Expected status: {test.ExpectedStatusCode}, got: {(int)response.StatusCode}";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<TestResult> RunSingleSqlTestAsync(SqlTest test, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var connectionString = user?.DatabaseConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                return new TestResult
                {
                    SqlTestId = test.Id,
                     IsSuccess = false,
                     ErrorMessage = "User has not set a database connection string."
                };
            }

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(test.SqlQuery, connection);
                var actualResult = (await command.ExecuteScalarAsync())?.ToString();

                bool success = actualResult == test.ExpectedResult;

                return new TestResult
                {
                    SqlTestId = test.Id,
                    IsSuccess = success,
                     Response = actualResult,
                    ErrorMessage = success ? "Test passed." : "Test failed."
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    SqlTestId = test.Id,
                    IsSuccess = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

    }

}
