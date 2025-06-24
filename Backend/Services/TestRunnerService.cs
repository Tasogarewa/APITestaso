using Backend.AppDbContext;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using static System.Net.Mime.MediaTypeNames;

namespace Backend.Services
{
    public class TestRunnerService
    {
        public readonly ApplicationDbContext _dbContext;
        public readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TestComparisonService _testComparison;

        public TestRunnerService(
            ApplicationDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            TestComparisonService testComparison)
        {
            _testComparison = testComparison;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<List<TestResult>> ExecuteScenarioAsync(ApiTestScenario scenario, HttpClient client, string executedByUserId)
        {
            var state = new Dictionary<string, string>();
            var results = new List<TestResult>();
            string error;

            foreach (var step in scenario.Tests)
            {
                var testResult = new TestResult
                {
                    ExecutedAt = DateTime.UtcNow,
                    ApiTestId = step.Id,
                    ExecutedByUserId = executedByUserId,
                    IsSuccess = false
                };

                try
                {
                    await MeasureExecutionTimeAsync(testResult, async () =>
                    {
                        string url = ReplaceTokens(step.Url, state);

                        
                        if (step.QueryParameters != null && step.QueryParameters.Any())
                        {
                            var uriBuilder = new UriBuilder(url);
                            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                            foreach (var param in step.QueryParameters)
                            {
                                string paramValue = ReplaceTokens(param.Value, state);
                                query[param.Key] = paramValue;
                            }
                            uriBuilder.Query = query.ToString();
                            url = uriBuilder.ToString();
                        }

                        HttpContent? content = null;
                        if (step.BodyJson != null)
                        {
                            string bodyStr = JsonSerializer.Serialize(step.BodyJson);
                            bodyStr = ReplaceTokens(bodyStr, state);
                            content = new StringContent(bodyStr, Encoding.UTF8, "application/json");
                        }

                        client.DefaultRequestHeaders.Clear();
                        AddHeadersToClient(client, step.Headers);

                        var request = new HttpRequestMessage(new HttpMethod(step.Method), url);
                        if (content != null)
                            request.Content = content;

                        using var cts = new CancellationTokenSource();
                        if (step.TimeoutSeconds.HasValue && step.TimeoutSeconds > 0)
                            cts.CancelAfter(TimeSpan.FromSeconds(step.TimeoutSeconds.Value));

                        HttpResponseMessage response = await client.SendAsync(request, cts.Token);

                        int statusCode = (int)response.StatusCode;
                        string responseBody = await response.Content.ReadAsStringAsync();

                        testResult.Response = $"Status: {statusCode}\nBody:\n{responseBody}";
                        string expectedResponseWithReplacements = ReplaceTokens(step.ExpectedResponse ?? "", state);
                        testResult.IsSuccess = _testComparison.CompareApiTest(expectedResponseWithReplacements, testResult.Response, step.ExpectedStatusCode, (int)response.StatusCode, out error);

                        if (!testResult.IsSuccess)
                        {
                            testResult.ErrorMessage = error;
                            throw new Exception(error);
                        }

                        if (step.Save != null)
                        {
                            foreach (var savePair in step.Save)
                            {
                                string value = ExtractJsonValue(responseBody, savePair.Value);
                                state[savePair.Key] = value;
                            }
                        }

                        testResult.IsSuccess = true;
                    });
                }
                catch (TaskCanceledException)
                {
                    testResult.ErrorMessage = $"Test '{step.Name}' timed out.";
                }
                catch (Exception ex)
                {
                    testResult.ErrorMessage = ex.Message;
                }

                results.Add(testResult);
                _dbContext.TestResults.Add(testResult);
            }

            await _dbContext.SaveChangesAsync();
            return results;
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

            await _dbContext.SaveChangesAsync();
            return results;
        }


public async Task<TestResult> ExecuteApiTestAsync(ApiTest test, string userId)
        {
            var state = new Dictionary<string, string>();
            string error;
            var result = new TestResult
            {
                ApiTestId = test.Id,
                ExecutedByUserId = userId,
                ExecutedAt = DateTime.UtcNow
            };

           
            string finalUrl = test.IsMock
                ? $"{_configuration["MockServer:PostmanMockBaseUrl"]?.TrimEnd('/')}/{test.Url.Replace("https://localhost:7200", "").TrimStart('/')}"
                : test.Url;

           
            if (test.QueryParameters != null && test.QueryParameters.Any())
            {
                var uriBuilder = new UriBuilder(finalUrl);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                foreach (var param in test.QueryParameters)
                {
                   
                    string paramValue = ReplaceTokens(param.Value, state);
                    query[param.Key] = paramValue;
                }
                uriBuilder.Query = query.ToString();
                finalUrl = uriBuilder.ToString();
            }

            try
            {
                await MeasureExecutionTimeAsync(result, async () =>
                {
                    var client = _httpClientFactory.CreateClient();
                    AddHeadersToClient(client, test.Headers);

                    HttpContent? requestBody = test.BodyJson != null
                        ? new StringContent(JsonSerializer.Serialize(test.BodyJson), Encoding.UTF8, "application/json")
                        : null;

                    
                    using var cts = new CancellationTokenSource();
                    if (test.TimeoutSeconds.HasValue && test.TimeoutSeconds > 0)
                        cts.CancelAfter(TimeSpan.FromSeconds(test.TimeoutSeconds.Value));

                    HttpResponseMessage response = test.Method.ToUpper() switch
                    {
                        "GET" => await client.GetAsync(finalUrl, cts.Token),
                        "POST" => await client.PostAsync(finalUrl, requestBody ?? new StringContent(""), cts.Token),
                        "PUT" => await client.PutAsync(finalUrl, requestBody ?? new StringContent(""), cts.Token),
                        "DELETE" => await client.DeleteAsync(finalUrl, cts.Token),
                        _ => throw new InvalidOperationException($"Unsupported HTTP method: {test.Method}")
                    };

                    string respBody = await response.Content.ReadAsStringAsync();
                    result.Response = respBody;

                    if (test.Save != null && test.Save.Any())
                    {
                        foreach (var kvp in test.Save)
                        {
                            try
                            {
                                var val = ExtractJsonValue(respBody, kvp.Value);
                                state[kvp.Key] = val;
                            }
                            catch
                            {
                                
                            }
                        }
                    }

                    string expectedResponseWithReplacements = ReplaceTokens(test.ExpectedResponse ?? "", state);

                    result.IsSuccess = _testComparison.CompareApiTest(expectedResponseWithReplacements, result.Response, test.ExpectedStatusCode, (int)response.StatusCode, out error);

                    if (!result.IsSuccess)
                        result.ErrorMessage = error;
                });
            }
            catch (TaskCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Test '{test.Name}' timed out.";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }

            if (result.IsSuccess)
                result.ErrorMessage = "Тест пройшов успішно";

            _dbContext.TestResults.Add(result);
            await _dbContext.SaveChangesAsync();

            return result;
        }

        public async Task<TestResult> RunSingleSqlTestAsync(SqlTest test, string userId)
        {
          
            var cs = test.DatabaseConnectionName;

            if (string.IsNullOrEmpty(cs))
            {
                return new TestResult
                {
                    SqlTestId = test.Id,
                    IsSuccess = false,
                    ErrorMessage = "SQL test has no database connection string specified.",
                    ExecutedByUserId = userId,
                    ExecutedAt = DateTime.UtcNow
                };
            }

            try
            {
                using var conn = new SqlConnection(cs);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(test.SqlQuery, conn);

                
                if (!string.IsNullOrEmpty(test.ParametersJson))
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(test.ParametersJson)!;
                    foreach (var kv in dict)
                    {
                        object? paramValue = kv.Value.ValueKind switch
                        {
                            JsonValueKind.String => kv.Value.GetString(),
                            JsonValueKind.Number => kv.Value.TryGetInt32(out int i) ? i : kv.Value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => DBNull.Value,
                            _ => kv.Value.GetRawText()
                        };

                        cmd.Parameters.AddWithValue(kv.Key, paramValue ?? DBNull.Value);
                    }
                }

                var result = new TestResult
                {
                    SqlTestId = test.Id,
                    ExecutedByUserId = userId,
                    ExecutedAt = DateTime.UtcNow
                };

                await MeasureExecutionTimeAsync(result, async () =>
                {
                    switch (test.TestType)
                    {
                        case SqlTestType.Scalar:
                            var scalar = (await cmd.ExecuteScalarAsync())?.ToString();
                            result.Response = scalar;
                            result.IsSuccess = scalar == test.ExpectedJson;
                            result.ErrorMessage = result.IsSuccess ? "Passed" : $"Expected '{test.ExpectedJson}', got '{scalar}'";
                            break;

                        case SqlTestType.ResultSet:
                            var actual = new List<Dictionary<string, object>>();
                            using (var rdr = await cmd.ExecuteReaderAsync())
                            {
                                while (await rdr.ReadAsync())
                                {
                                    var row = new Dictionary<string, object>();
                                    for (int i = 0; i < rdr.FieldCount; i++)
                                        row[rdr.GetName(i)] = rdr.GetValue(i);
                                    actual.Add(row);
                                }
                            }

                            var expected = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(test.ExpectedJson!)!;
                            result.Response = JsonSerializer.Serialize(actual);
                            result.IsSuccess = CompareResultSets(actual, expected);
                            result.ErrorMessage = result.IsSuccess ? "Passed" : "Result sets differ";
                            break;

                        case SqlTestType.Schema:
                            var spec = JsonSerializer.Deserialize<SchemaSpec>(test.ExpectedJson!)!;
                            result.IsSuccess = await ValidateSchemaAsync(conn, spec);
                            result.Response = result.IsSuccess ? "Schema matches spec" : "Schema does not match spec";
                            result.ErrorMessage = result.IsSuccess ? null : "Schema validation failed";
                            break;
                    }
                });

                _dbContext.TestResults.Add(result);
                await _dbContext.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                var failResult = new TestResult
                {
                    SqlTestId = test.Id,
                    ExecutedByUserId = userId,
                    IsSuccess = false,
                    ErrorMessage = $"{ex.Message}\nStackTrace: {ex.StackTrace}",
                    ExecutedAt = DateTime.UtcNow
                };
                _dbContext.TestResults.Add(failResult);
                await _dbContext.SaveChangesAsync();
                return failResult;
            }
        }

        private async Task<bool> ValidateSchemaAsync(SqlConnection conn, SchemaSpec spec)
        {
            var colCmd = new SqlCommand(
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table", conn);
            colCmd.Parameters.AddWithValue("@table", spec.TableName);

            var actualCols = new List<string>();
            using (var rdr = await colCmd.ExecuteReaderAsync())
                while (await rdr.ReadAsync())
                    actualCols.Add(rdr.GetString(0));

            if (!spec.ExpectedColumns.All(c => actualCols.Contains(c)))
                return false;

            var idxSql = $@"
                SELECT name FROM sys.indexes 
                WHERE object_id = OBJECT_ID(@table) AND is_primary_key = 0";

            using var idxCmd = new SqlCommand(idxSql, conn);
            idxCmd.Parameters.AddWithValue("@table", spec.TableName);

            var actualIdx = new List<string>();
            using (var rdr = await idxCmd.ExecuteReaderAsync())
                while (await rdr.ReadAsync())
                    actualIdx.Add(rdr.GetString(0));

            return spec.ExpectedIndexes.All(i => actualIdx.Contains(i));
        }

        private bool CompareResultSets(List<Dictionary<string, object>> actual, List<Dictionary<string, object>> expected)
        {
            if (actual.Count != expected.Count) return false;

            var actJson = actual.Select(d => JsonSerializer.Serialize(d)).OrderBy(s => s);
            var expJson = expected.Select(d => JsonSerializer.Serialize(d)).OrderBy(s => s);

            return actJson.SequenceEqual(expJson);
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
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue(parts[0], parts[1]);
                    else
                        client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
                else
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        public string ReplaceTokens(string input, Dictionary<string, string> state)
        {
            return Regex.Replace(input, @"\{(\w+)\}", match =>
            {
                var key = match.Groups[1].Value;
                return state.TryGetValue(key, out var val) ? val : match.Value;
            });
        }

        public string ExtractJsonValue(string json, string jsonPath)
        {
            using var doc = JsonDocument.Parse(json);

            if (!jsonPath.StartsWith("$."))
                throw new ArgumentException("JSONPath must start with '$.'");

            var pathParts = jsonPath.Substring(2).Split('.');

            JsonElement current = doc.RootElement;

            foreach (var part in pathParts)
            {
                if (current.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Property '{part}' not found because current element is not an object.");

                if (!current.TryGetProperty(part, out var next))
                    throw new KeyNotFoundException($"Property '{part}' not found in JSON.");

                current = next;
            }

            
            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString() ?? "",
                JsonValueKind.Number => current.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                _ => current.GetRawText() 
            };
        }
        private async Task MeasureExecutionTimeAsync(TestResult result, Func<Task> testExecution)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await testExecution();
            }
            finally
            {
                stopwatch.Stop();
                result.DurationMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }
    }
}