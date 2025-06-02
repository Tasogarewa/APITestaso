using Backend.AppDbContext;
using Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services
{
    public class TestRunnerService
    {
        public readonly ApplicationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public TestRunnerService(
            ApplicationDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
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
                var result = await ExecuteSqlTestAsync(test, userId);
                results.Add(result);
            }

            await _dbContext.TestResults.AddRangeAsync(results);
            await _dbContext.SaveChangesAsync();

            return results;
        }

     
        private async Task<TestResult> ExecuteApiTestAsync(ApiTest test, string userId)
        {
            var result = new TestResult
            {
                ApiTestId = test.Id,
                ExecutedByUserId = userId,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                HttpResponseMessage response;

                switch (test.Method.ToUpper())
                {
                    case "GET":
                        response = await client.GetAsync(test.Url);
                        break;

                    case "POST":
                        var postContent = new StringContent(test.Body ?? "", Encoding.UTF8, "application/json");
                        response = await client.PostAsync(test.Url, postContent);
                        break;

                    case "PUT":
                        var putContent = new StringContent(test.Body ?? "", Encoding.UTF8, "application/json");
                        response = await client.PutAsync(test.Url, putContent);
                        break;

                    case "DELETE":
                        response = await client.DeleteAsync(test.Url);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported HTTP method: {test.Method}");
                }

                var respBody = await response.Content.ReadAsStringAsync();
                result.Response = respBody;

                
                bool statusOk = response.StatusCode.GetHashCode() == test.ExpectedStatusCode;
                bool bodyOk = string.IsNullOrEmpty(test.ExpectedResponse)
                    || respBody.Contains(test.ExpectedResponse);

                result.IsSuccess = statusOk && bodyOk;
                result.ErrorMessage = result.IsSuccess ? null : $"Expected status: {test.ExpectedStatusCode}, got: {(int)response.StatusCode}";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

     
        private async Task<TestResult> ExecuteSqlTestAsync(SqlTest test, string userId)
        {
            var result = new TestResult
            {
                SqlTestId = test.Id,
                ExecutedByUserId = userId,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                
                var connString = _configuration.GetConnectionString(test.DatabaseConnectionName);
                using var connection = new SqlConnection(connString);
                await connection.OpenAsync();

                using var command = new SqlCommand(test.SqlQuery, connection);
                var reader = await command.ExecuteReaderAsync();

             
                var sb = new StringBuilder();
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            sb.Append(reader[i]?.ToString());
                            if (i < reader.FieldCount - 1)
                                sb.Append(", ");
                        }
                        sb.AppendLine();
                    }
                }

                var actual = sb.ToString().TrimEnd();
                result.Response = actual;

                
                if (string.IsNullOrEmpty(test.ExpectedResult))
                {
                    result.IsSuccess = true;
                }
                else
                {
                    result.IsSuccess = actual.Contains(test.ExpectedResult);
                    if (!result.IsSuccess)
                    {
                        result.ErrorMessage = $"Expected: '{test.ExpectedResult}', got: '{actual}'";
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }
}
