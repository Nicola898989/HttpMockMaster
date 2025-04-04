using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BackendService.Models;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any, NoStore = false)]
    public class PerformanceController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(DatabaseContext context, ILogger<PerformanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("metrics")]
        public async Task<ActionResult<object>> GetPerformanceMetrics(
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
                    
                    startDate = timeFrame.ToLower() switch
                    {
                        "hour" => endDate.Value.AddHours(-1),
                        "day" => endDate.Value.AddDays(-1),
                        "week" => endDate.Value.AddDays(-7),
                        "month" => endDate.Value.AddMonths(-1),
                        _ => endDate.Value.AddDays(-1) // default a un giorno
                    };
                }

                // Convertire le date in stringhe per la query (il formato dipende dal database)
                string startDateStr = startDate.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                string endDateStr = endDate.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                // Query per ottenere i dati di performance delle richieste
                var requests = await _context.Requests
                    .Where(r => r.Timestamp.CompareTo(startDateStr) >= 0 && r.Timestamp.CompareTo(endDateStr) <= 0)
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
                return Ok(new
                {
                    timeFrame,
                    startDate,
                    endDate,
                    totalRequests = requests.Count,
                    responseTimeMetrics,
                    requestSizeMetrics,
                    methodMetrics,
                    statusCodeMetrics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle metriche di performance: {Message}", ex.Message);
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        [HttpGet("timeseries")]
        public async Task<ActionResult<object>> GetTimeSeriesData(
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
                    
                    startDate = timeFrame.ToLower() switch
                    {
                        "hour" => endDate.Value.AddHours(-1),
                        "day" => endDate.Value.AddDays(-1),
                        "week" => endDate.Value.AddDays(-7),
                        "month" => endDate.Value.AddMonths(-1),
                        _ => endDate.Value.AddDays(-1)
                    };
                }

                string startDateStr = startDate.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                string endDateStr = endDate.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                var requests = await _context.Requests
                    .Where(r => r.Timestamp.CompareTo(startDateStr) >= 0 && r.Timestamp.CompareTo(endDateStr) <= 0)
                    .Include(r => r.Response)
                    .ToListAsync();

                // Raggruppare i dati per intervalli di tempo
                var timeSeriesData = GroupRequestsByTimeInterval(requests, groupBy, startDate.Value, endDate.Value);

                return Ok(new
                {
                    timeFrame,
                    startDate,
                    endDate,
                    groupBy,
                    data = timeSeriesData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei dati delle serie temporali: {Message}", ex.Message);
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        private object CalculateResponseTimeMetrics(List<HttpRequest> requests)
        {
            // Calcolare i tempi di risposta (differenza tra timestamp della risposta e della richiesta)
            var responseTimes = new List<double>();
            
            foreach (var request in requests.Where(r => r.Response != null))
            {
                // Parsificare i timestamp (il formato esatto dipende da come sono salvati nel database)
                if (DateTime.TryParse(request.Timestamp, out var requestTime) &&
                    DateTime.TryParse(request.Response.Timestamp, out var responseTime))
                {
                    double responseTimeMs = (responseTime - requestTime).TotalMilliseconds;
                    responseTimes.Add(responseTimeMs);
                }
            }

            // Se non ci sono dati, restituire valori di default
            if (!responseTimes.Any())
            {
                return new
                {
                    avg = 0,
                    min = 0,
                    max = 0,
                    median = 0,
                    p95 = 0,
                    p99 = 0
                };
            }

            // Calcolare le metriche
            responseTimes.Sort();
            
            return new
            {
                avg = responseTimes.Average(),
                min = responseTimes.First(),
                max = responseTimes.Last(),
                median = CalculatePercentile(responseTimes, 50),
                p95 = CalculatePercentile(responseTimes, 95),
                p99 = CalculatePercentile(responseTimes, 99)
            };
        }

        private object CalculateRequestSizeMetrics(List<HttpRequest> requests)
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

            return new
            {
                request = new
                {
                    avg = requestSizes.Any() ? requestSizes.Average() : 0,
                    total = requestSizes.Sum(),
                    max = requestSizes.Any() ? requestSizes.Max() : 0
                },
                response = new
                {
                    avg = responseSizes.Any() ? responseSizes.Average() : 0,
                    total = responseSizes.Sum(),
                    max = responseSizes.Any() ? responseSizes.Max() : 0
                }
            };
        }

        private object CalculateMethodMetrics(List<HttpRequest> requests)
        {
            // Contare le richieste per metodo HTTP
            var methodCounts = requests
                .GroupBy(r => r.Method)
                .Select(g => new
                {
                    method = g.Key,
                    count = g.Count(),
                    avgResponseTime = g.Where(r => r.Response != null)
                        .Select(r => {
                            if (DateTime.TryParse(r.Timestamp, out var requestTime) &&
                                DateTime.TryParse(r.Response.Timestamp, out var responseTime))
                            {
                                return (responseTime - requestTime).TotalMilliseconds;
                            }
                            return 0.0;
                        })
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .OrderByDescending(m => m.count)
                .ToList();

            return methodCounts;
        }

        private object CalculateStatusCodeMetrics(List<HttpRequest> requests)
        {
            // Contare le risposte per codice di stato HTTP
            var statusCounts = requests
                .Where(r => r.Response != null)
                .GroupBy(r => r.Response.StatusCode / 100) // Raggruppa per classe di stato (2xx, 3xx, 4xx, 5xx)
                .Select(g => new
                {
                    statusClass = $"{g.Key}xx",
                    count = g.Count(),
                    percentage = (double)g.Count() / requests.Count(r => r.Response != null) * 100
                })
                .OrderBy(s => s.statusClass)
                .ToList();

            return statusCounts;
        }

        private List<object> GroupRequestsByTimeInterval(List<HttpRequest> requests, string? groupBy, DateTime startDate, DateTime endDate)
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

            // Convertire i timestamp delle richieste in oggetti DateTime
            var requestsWithDateTime = requests
                .Select(r => new
                {
                    Request = r,
                    DateTime = DateTime.TryParse(r.Timestamp, out var dt) ? dt : startDate
                })
                .ToList();

            // Raggruppare le richieste per bucket di tempo
            return timeBuckets.Select(bucketStart =>
            {
                var bucketEnd = bucketStart.Add(interval);
                
                var bucketRequests = requestsWithDateTime
                    .Where(r => r.DateTime >= bucketStart && r.DateTime < bucketEnd)
                    .Select(r => r.Request)
                    .ToList();

                // Calcolare le metriche per questo bucket di tempo
                var responseTimes = bucketRequests
                    .Where(r => r.Response != null)
                    .Select(r => {
                        if (DateTime.TryParse(r.Timestamp, out var requestTime) &&
                            DateTime.TryParse(r.Response.Timestamp, out var responseTime))
                        {
                            return (responseTime - requestTime).TotalMilliseconds;
                        }
                        return 0.0;
                    })
                    .ToList();

                return new
                {
                    timestamp = bucketStart,
                    requestCount = bucketRequests.Count,
                    avgResponseTime = responseTimes.Any() ? responseTimes.Average() : 0,
                    successRate = bucketRequests.Count > 0 
                        ? (double)bucketRequests.Count(r => r.Response != null && r.Response.StatusCode >= 200 && r.Response.StatusCode < 400) / bucketRequests.Count * 100 
                        : 0
                };
            }).ToList();
        }

        private double CalculatePercentile(List<double> values, int percentile)
        {
            if (!values.Any())
                return 0;
                
            var index = (int)Math.Ceiling(percentile / 100.0 * values.Count) - 1;
            return values[Math.Max(0, Math.Min(index, values.Count - 1))];
        }
    }
}