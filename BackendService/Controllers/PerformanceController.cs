using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BackendService.Models;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/performance")]
    public class PerformanceController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<PerformanceController> _logger;
        
        public PerformanceController(DatabaseContext context, ILogger<PerformanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class PerformanceMetricsResponse
        {
            public string? TimeFrame { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public int TotalRequests { get; set; }
            public ResponseTimeMetrics? ResponseTimeMetrics { get; set; } = new ResponseTimeMetrics();
            public RequestSizeMetrics? RequestSizeMetrics { get; set; } = new RequestSizeMetrics();
            public List<MethodMetric>? MethodMetrics { get; set; } = new List<MethodMetric>();
            public List<StatusCodeMetric>? StatusCodeMetrics { get; set; } = new List<StatusCodeMetric>();
        }

        [HttpGet("metrics")]
        public async Task<ActionResult<PerformanceMetricsResponse>> GetPerformanceMetrics(
            [FromQuery] string? timeFrame = "day", 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Se le date non sono specificate, impostarle in base al timeframe
                if (startDate == null || endDate == null)
                {
                    endDate = DateTime.UtcNow;
                    
                    startDate = timeFrame?.ToLower() switch
                    {
                        "hour" => endDate.Value.AddHours(-1),
                        "day" => endDate.Value.AddDays(-1),
                        "week" => endDate.Value.AddDays(-7),
                        "month" => endDate.Value.AddMonths(-1),
                        _ => endDate.Value.AddDays(-1) // default a un giorno
                    };
                }

                // Query per ottenere i dati di performance delle richieste
                var requests = await _context.Requests
                    .Where(r => r.Timestamp >= startDate.Value && r.Timestamp <= endDate.Value)
                    .Include(r => r.Response)
                    .ToListAsync();

                // Calcolare statistiche sui tempi di risposta
                var responseTimeMetrics = CalculateResponseTimeMetrics(requests);
                
                // Calcolare statistiche sulle dimensioni delle richieste
                var requestSizeMetrics = CalculateRequestSizeMetrics(requests);
                
                // Calcolare statistiche per metodo HTTP
                var methodMetrics = CalculateMethodMetrics(requests);
                
                // Calcolare statistiche di successo/errore
                var statusCodeMetrics = CalculateStatusCodeMetrics(requests);

                // Restituire tutti i dati di metrica
                var response = new PerformanceMetricsResponse
                {
                    TimeFrame = timeFrame,
                    StartDate = startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    EndDate = endDate.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalRequests = requests.Count,
                    ResponseTimeMetrics = responseTimeMetrics,
                    RequestSizeMetrics = requestSizeMetrics,
                    MethodMetrics = methodMetrics,
                    StatusCodeMetrics = statusCodeMetrics
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle metriche di performance: {Message}", ex.Message);
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        public class TimeSeriesResponse
        {
            public string? TimeFrame { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public string? GroupBy { get; set; }
            public List<TimeSeriesPoint>? Data { get; set; } = new List<TimeSeriesPoint>();
        }

        [HttpGet("timeseries")]
        public async Task<ActionResult<TimeSeriesResponse>> GetTimeSeriesData(
            [FromQuery] string? timeFrame = "day",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? groupBy = "hour")
        {
            try
            {
                // Stessa logica per le date come sopra
                if (startDate == null || endDate == null)
                {
                    endDate = DateTime.UtcNow;
                    
                    startDate = timeFrame?.ToLower() switch
                    {
                        "hour" => endDate.Value.AddHours(-1),
                        "day" => endDate.Value.AddDays(-1),
                        "week" => endDate.Value.AddDays(-7),
                        "month" => endDate.Value.AddMonths(-1),
                        _ => endDate.Value.AddDays(-1)
                    };
                }

                var requests = await _context.Requests
                    .Where(r => r.Timestamp >= startDate.Value && r.Timestamp <= endDate.Value)
                    .Include(r => r.Response)
                    .ToListAsync();

                // Raggruppare i dati per intervalli di tempo
                var timeSeriesData = GroupRequestsByTimeInterval(requests, groupBy, startDate.Value, endDate.Value);

                var response = new TimeSeriesResponse
                {
                    TimeFrame = timeFrame,
                    StartDate = startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    EndDate = endDate.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    GroupBy = groupBy,
                    Data = timeSeriesData
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei dati delle serie temporali: {Message}", ex.Message);
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        public class ResponseTimeMetrics
        {
            public double Avg { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public double Median { get; set; }
            public double P95 { get; set; }
            public double P99 { get; set; }
        }

        private ResponseTimeMetrics CalculateResponseTimeMetrics(List<BackendService.Models.HttpRequest> requests)
        {
            // Calcolare i tempi di risposta (differenza tra timestamp della risposta e della richiesta)
            var responseTimes = new List<double>();
            
            foreach (var request in requests.Where(r => r.Response != null))
            {
                // Parsificare i timestamp (il formato esatto dipende da come sono salvati nel database)
                var requestTime = request.Timestamp;
                var responseTime = request.Response?.Timestamp ?? DateTime.UtcNow;
                double responseTimeMs = (responseTime - requestTime).TotalMilliseconds;
                responseTimes.Add(responseTimeMs);
            }

            // Se non ci sono dati, restituire valori di default
            if (!responseTimes.Any())
            {
                return new ResponseTimeMetrics
                {
                    Avg = 0,
                    Min = 0,
                    Max = 0,
                    Median = 0,
                    P95 = 0,
                    P99 = 0
                };
            }

            // Calcolare le metriche
            responseTimes.Sort();
            
            return new ResponseTimeMetrics
            {
                Avg = responseTimes.Average(),
                Min = responseTimes.First(),
                Max = responseTimes.Last(),
                Median = CalculatePercentile(responseTimes, 50),
                P95 = CalculatePercentile(responseTimes, 95),
                P99 = CalculatePercentile(responseTimes, 99)
            };
        }

        public class SizeMetrics
        {
            public double Avg { get; set; }
            public int Total { get; set; }
            public int Max { get; set; }
        }
        
        public class RequestSizeMetrics
        {
            public SizeMetrics? Request { get; set; } = new SizeMetrics();
            public SizeMetrics? Response { get; set; } = new SizeMetrics();
        }
        
        private RequestSizeMetrics CalculateRequestSizeMetrics(List<BackendService.Models.HttpRequest> requests)
        {
            // Calcolare le dimensioni delle richieste e risposte
            var requestSizes = new List<int>();
            var responseSizes = new List<int>();
            
            foreach (var request in requests)
            {
                // Calcolare la dimensione del corpo della richiesta
                if (!string.IsNullOrEmpty(request.Body))
                {
                    requestSizes.Add(request.Body.Length);
                }
                
                // Calcolare la dimensione del corpo della risposta
                if (request.Response != null && !string.IsNullOrEmpty(request.Response.Body))
                {
                    responseSizes.Add(request.Response.Body.Length);
                }
            }

            return new RequestSizeMetrics
            {
                Request = new SizeMetrics
                {
                    Avg = requestSizes.Any() ? requestSizes.Average() : 0,
                    Total = requestSizes.Sum(),
                    Max = requestSizes.Any() ? requestSizes.Max() : 0
                },
                Response = new SizeMetrics
                {
                    Avg = responseSizes.Any() ? responseSizes.Average() : 0,
                    Total = responseSizes.Sum(),
                    Max = responseSizes.Any() ? responseSizes.Max() : 0
                }
            };
        }

        public class MethodMetric
        {
            public string? Method { get; set; }
            public int Count { get; set; }
            public double AvgResponseTime { get; set; }
        }

        private List<MethodMetric> CalculateMethodMetrics(List<BackendService.Models.HttpRequest> requests)
        {
            // Contare le richieste per metodo HTTP
            var methodCounts = requests
                .GroupBy(r => r.Method)
                .Select(g => new MethodMetric
                {
                    Method = g.Key,
                    Count = g.Count(),
                    AvgResponseTime = g.Where(r => r.Response != null)
                        .Select(r => {
                            if (r.Response != null) {
                                var responseTime = r.Response.Timestamp;
                                var requestTime = r.Timestamp;
                                return (responseTime - requestTime).TotalMilliseconds;
                            }
                            return 0.0;
                        })
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .OrderByDescending(m => m.Count)
                .ToList();

            return methodCounts;
        }

        public class StatusCodeMetric
        {
            public string? StatusClass { get; set; }
            public int Count { get; set; }
            public double Percentage { get; set; }
        }

        private List<StatusCodeMetric> CalculateStatusCodeMetrics(List<BackendService.Models.HttpRequest> requests)
        {
            // Contare le risposte per codice di stato HTTP
            var statusCounts = requests
                .Where(r => r.Response != null)
                .GroupBy(r => r.Response != null ? r.Response.StatusCode / 100 : 0) // Raggruppa per classe di stato (2xx, 3xx, 4xx, 5xx)
                .Select(g => new StatusCodeMetric
                {
                    StatusClass = $"{g.Key}xx",
                    Count = g.Count(),
                    Percentage = (double)g.Count() / requests.Count(r => r.Response != null) * 100
                })
                .OrderBy(s => s.StatusClass)
                .ToList();

            return statusCounts;
        }

        public class TimeSeriesPoint
        {
            public string? Timestamp { get; set; }
            public int RequestCount { get; set; }
            public double AvgResponseTime { get; set; }
            public double SuccessRate { get; set; }
        }

        private List<TimeSeriesPoint> GroupRequestsByTimeInterval(List<BackendService.Models.HttpRequest> requests, string? groupBy, DateTime startDate, DateTime endDate)
        {
            // Determinare l'intervallo di tempo per il raggruppamento
            TimeSpan interval = groupBy?.ToLower() switch
            {
                "minute" => TimeSpan.FromMinutes(1),
                "hour" => TimeSpan.FromHours(1),
                "day" => TimeSpan.FromDays(1),
                _ => TimeSpan.FromHours(1) // default a un'ora
            };

            // Creare bucket di tempo per l'intero intervallo richiesto
            var timeBuckets = new List<DateTime>();
            for (var time = startDate; time <= endDate; time = time.Add(interval))
            {
                timeBuckets.Add(time);
            }

            // Utilizzare i timestamp delle richieste già come oggetti DateTime
            var requestsWithDateTime = requests
                .Select(r => new
                {
                    Request = r,
                    TimestampDate = r.Timestamp
                })
                .ToList();

            // Raggruppare le richieste per bucket di tempo
            var result = new List<TimeSeriesPoint>();
            
            foreach (var bucketStart in timeBuckets)
            {
                var bucketEnd = bucketStart.Add(interval);
                
                var bucketRequests = requestsWithDateTime
                    .Where(r => r.TimestampDate >= bucketStart && r.TimestampDate < bucketEnd)
                    .Select(r => r.Request)
                    .ToList();
                    
                var responseTimes = bucketRequests
                    .Where(r => r.Response != null)
                    .Select(r => {
                        if (r.Response != null)
                        {
                            return (r.Response.Timestamp - r.Timestamp).TotalMilliseconds;
                        }
                        return 0.0d;
                    })
                    .ToList();
                    
                result.Add(new TimeSeriesPoint
                {
                    Timestamp = bucketStart.ToString("yyyy-MM-dd HH:mm:ss"),
                    RequestCount = bucketRequests.Count(),
                    AvgResponseTime = responseTimes.Any() ? responseTimes.Average() : 0,
                    SuccessRate = bucketRequests.Count() > 0 
                        ? (double)bucketRequests.Count(r => r.Response != null && r.Response.StatusCode >= 200 && r.Response.StatusCode < 400) / bucketRequests.Count() * 100 
                        : 0
                });
            }
            
            return result;
        }

        private double CalculatePercentile(List<double> values, int percentile)
        {
            if (!values.Any())
                return 0;
                
            var index = (int)Math.Ceiling(percentile / 100.0 * values.Count()) - 1;
            return values[Math.Max(0, Math.Min(index, values.Count() - 1))];
        }
    }
}