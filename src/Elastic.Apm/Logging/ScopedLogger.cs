using System;
using System.Collections.Concurrent;

namespace Elastic.Apm.Logging
{
	internal class ScopedLogger : IApmLogger
	{
		public ScopedLogger(IApmLogger logger, string scope) => (Logger, Scope) = (logger, scope);

		private ConcurrentDictionary<string, LogValuesFormatter> Formatters { get; } = new ConcurrentDictionary<string, LogValuesFormatter>();

		public IApmLogger Logger { get; }

		public string Scope { get; }

		public bool IsEnabled(LogLevel level) => Logger.IsEnabled(level);

		internal LogValuesFormatter GetOrAddFormatter(string message, int expectedCount)
			=> Formatters.GetOrAdd(message, s => new LogValuesFormatter($"{{{{{{Scope}}}}}} {s}", expectedCount, Scope));

		void IApmLogger.Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) =>
			Logger.Log(level, state, e, formatter);
	}
}
