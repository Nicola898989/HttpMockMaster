using System;
using System.Collections.Generic;

namespace BackendService.Models
{
    public class TestScenario
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<ScenarioStep> Steps { get; set; } = new List<ScenarioStep>();
    }
}