using System.Collections.Generic;

namespace Backend.DTOs 
{ 
    public class ApiTestScenarioDto 
    { public int Id { get; set; }
        public string ScenarioName { get; set; } = string.Empty;
        public List<int> TestIds { get; set; } = new(); 
        public string CreatedByUserId { get; set; } = string.Empty; 
    } 
}