﻿using DockerSamples.AspNetExporter.Common.Collectors;
using DockerSamples.AspNetExporter.Common.Configuration;
using Newtonsoft.Json;
using Prometheus;
using Prometheus.Advanced;
using Serilog;
using System.IO;
using System.Linq;
using System.Threading;
using static System.Console;

namespace DockerSamples.AspNetExporter.App
{
    class Program
    {
        private static ManualResetEvent _ResetEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            if (!File.Exists(Config.CollectorConfigPath))
            {
                WriteLine("No collector config path configured. Set $env:COLLECTOR_CONFIG_PATH");
                return;
            }

            var logger = new LoggerConfiguration()
                                .ReadFrom.AppSettings()
                                .CreateLogger();

            var json = File.ReadAllText(Config.CollectorConfigPath);
            var collectorConfig = JsonConvert.DeserializeObject<CollectorConfiguration>(json);
            if (!collectorConfig.PerformanceCounterCollectors.Any())
            {
                logger.Information("No collectors configured. Config file path: {Path}", Config.CollectorConfigPath);
                return;
            }

            DefaultCollectorRegistry.Instance.Clear();
            DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(
                new ConfiguredPerformanceCounterCollector(collectorConfig, Config.RecordCollections, logger));
            
            var server = new MetricServer(Config.MetricsPort);
            server.Start();
            logger.Information("Metrics server listening on port: {Port}", Config.MetricsPort);

            _ResetEvent.WaitOne();
        }
    }
}
