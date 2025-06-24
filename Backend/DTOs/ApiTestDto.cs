namespace Backend.DTOs
{
    public class ApiTestDto
    {

        public int Id { get; set; }  

        public string Name { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public Dictionary<string, string>? Headers { get; set; }
        public Dictionary<string, string>? QueryParameters { get; set; }
        public object? BodyJson { get; set; }

        public string? ExpectedResponse { get; set; }

        public bool IsMock { get; set; }

        public int? TimeoutSeconds { get; set; }

        public int ExpectedStatusCode { get; set; } = 200;

        public string CreatedByUserId { get; set; } = string.Empty;

        public Dictionary<string, string>? Save { get; set; }
    }
}
