using System.Text.Json;
using System.Text.Json.Nodes;

namespace Backend.Services
{
    public class TestComparisonService
    {
        public bool CompareApiTest(string? expectedResponse, string? actualResponse, int expectedStatusCode, int actualStatusCode, out string? error)
        {
            error = null;

            if (expectedStatusCode != actualStatusCode)
            {
                error = $"Очікуваний статус-код: {expectedStatusCode}, фактичний: {actualStatusCode}";
                return false;
            }

            if (expectedResponse == null && actualResponse == null)
                return true;

            try
            {
                var expectedJson = TryUnwrapJsonStrings(JsonNode.Parse(expectedResponse ?? ""));
                var actualJson = TryUnwrapJsonStrings(JsonNode.Parse(actualResponse ?? ""));

                if (!JsonEquals(expectedJson, actualJson))
                {
                    error = "Очікувана та фактична JSON-відповіді не збігаються.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = $"Помилка порівняння JSON: {ex.Message}";
                return false;
            }
        }
        private JsonNode? TryUnwrapJsonStrings(JsonNode? node)
        {
            if (node is JsonValue value && value.TryGetValue<string>(out var strValue))
            {
                try
                {
                    var parsed = JsonNode.Parse(strValue);
                    return TryUnwrapJsonStrings(parsed);
                }
                catch
                {
                    return node;
                }
            }
            else if (node is JsonArray array)
            {
                var newArray = new JsonArray();
                foreach (var item in array)
                {
                    newArray.Add(TryUnwrapJsonStrings(item));
                }
                return newArray;
            }
            else if (node is JsonObject obj)
            {
                var newObj = new JsonObject();
                foreach (var kvp in obj)
                {
                    newObj[kvp.Key] = TryUnwrapJsonStrings(kvp.Value);
                }
                return newObj;
            }
            return node;
        }
        public bool CompareSqlTest(SqlTest sqlTest, string actualResultJson, out string? error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(sqlTest.ExpectedJson))
            {
                error = "Очікуване значення не задане.";
                return false;
            }

            try
            {
                var expected = JsonNode.Parse(sqlTest.ExpectedJson!);
                var actual = JsonNode.Parse(actualResultJson);

                if (!JsonEquals(expected, actual))
                {
                    error = "Очікуваний результат SQL не відповідає фактичному.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = $"Помилка при парсингу або порівнянні JSON: {ex.Message}";
                return false;
            }
        }

        private bool JsonEquals(JsonNode? a, JsonNode? b)
        {
            return JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b);
        }
    }
}
