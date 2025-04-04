using System;

namespace BackendService.Models
{
    public class ScenarioStep
    {
        public int Id { get; set; }
        public int TestScenarioId { get; set; }
        public int Order { get; set; }
        public int? HttpRequestId { get; set; }
        public HttpRequest HttpRequest { get; set; }
        public int? HttpResponseId { get; set; }
        public HttpResponse HttpResponse { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        
        // Navigation property back to the test scenario
        public TestScenario TestScenario { get; set; }
        
        // Added for compatibility with tests
        public int ScenarioId 
        { 
            get => TestScenarioId;
            set => TestScenarioId = value;
        }
    }
}