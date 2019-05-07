using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Timer = System.Timers.Timer;

namespace Elastic.Apm.Metrics
{
	public class MetricsCollector
	{
		private static string _systemCpuTotalPct = "system.cpu.total.norm.pct";
		private static string _processCpuTotalPct = "system.process.cpu.total.norm.pc";

		private readonly IPayloadSender _payloadSender;
		private readonly IApmLogger _logger;

		Timer _timer = new Timer(1000);

		private DateTimeOffset _lastTick;
		private TimeSpan _lastCurrentProcessCpuTime;
		private bool _first = true;

		public MetricsCollector(IApmLogger logger, IPayloadSender payloadSender)
		{
			_payloadSender = payloadSender;
			_logger = logger.Scoped(nameof(MetricsCollector));

			_logger.Debug()?.Log("starting metricscollector");

			_timer.Elapsed += (sender, args) =>
			{
				var cpuUsage = Process.GetCurrentProcess().TotalProcessorTime;  // .Milliseconds % 100 / 100;

				var virtualMemory = Process.GetCurrentProcess().VirtualMemorySize64;
				var workingSet = Process.GetCurrentProcess().WorkingSet64;


				var timespan = DateTimeOffset.UtcNow;

				if (!_first)
				{
					var cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds;
					var totalMsPassed = (timespan - _lastTick).TotalMilliseconds;

					var cpuUsageTotal = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) / 100;
					_logger.Error()?.Log("New CPU metrics: {metricvalue}, pid: {pid}", cpuUsageTotal, Process.GetCurrentProcess().Id);


					var metricSet = new MetricSet((timespan.ToUnixTimeMilliseconds() * 1000), new Sample(_processCpuTotalPct, cpuUsageTotal));
					_payloadSender.QueueMetrics(metricSet);
				}

				_first = false;
				_lastTick = timespan;
				_lastCurrentProcessCpuTime = cpuUsage;
			};

			_timer.Start();
		}
	}
}
