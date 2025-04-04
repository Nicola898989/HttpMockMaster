using System;
using System.Collections.Generic;

namespace BackendService.Models
{
    public class TestScenario
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<ScenarioStep> Steps { get; set; } = new List<ScenarioStep>();
    }
}