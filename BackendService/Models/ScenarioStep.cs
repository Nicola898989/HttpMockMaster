using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendService.Models
{
    public class ScenarioStep
    {
        public int Id { get; set; }
        public int TestScenarioId { get; set; }
        public int Order { get; set; }
        public int? HttpRequestId { get; set; }
        public HttpRequest? HttpRequest { get; set; }
        public int? HttpResponseId { get; set; }
        public HttpResponse? HttpResponse { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        
        // Parametrizzazione
        public string? ParameterizedUrl { get; set; }
        public string? ParameterizedHeaders { get; set; }
        public string? ParameterizedBody { get; set; }
        
        // Dizionario di parametri serializzato come JSON
        public string? ParametersJson { get; set; }
        
        [JsonIgnore]
        public Dictionary<string, string> Parameters
        {
            get
            {
                if (string.IsNullOrEmpty(ParametersJson))
                    return new Dictionary<string, string>();
                
                try 
                {
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(ParametersJson) 
                        ?? new Dictionary<string, string>();
                }
                catch 
                {
                    return new Dictionary<string, string>();
                }
            }
            set => ParametersJson = JsonSerializer.Serialize(value);
        }
        
        // Navigation property back to the test scenario
        public TestScenario? TestScenario { get; set; }
        
        // Added for compatibility with tests
        public int ScenarioId 
        { 
            get => TestScenarioId;
            set => TestScenarioId = value;
        }

        // True se questo step è una richiesta dal client
        public bool IsClientRequest { get; set; } = true;
    }
}