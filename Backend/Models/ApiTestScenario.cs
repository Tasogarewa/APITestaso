using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Backend.Models
{
    public class ApiTestScenario
    {
        public int Id { get; set; }
        public string ScenarioName { get; set; } = "";
        public ICollection<ApiTest> Tests { get; set; } = new List<ApiTest>();
        public string CreatedByUserId { get; set; } = null!;
        public ApplicationUser? CreatedByUser { get; set; }
    }

  
}
