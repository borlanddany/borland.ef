using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Borland.EF.Tests
{
    public class ConsoleLoggerProvider : ILoggerProvider
    {
        public static TextWriter TextWriter = Console.Out;

        public ILogger CreateLogger(string categoryName)
        {
            return new ConsoleLogger(categoryName);
        }

        public void Dispose()
        {

        }

        private class ConsoleLogger : ILogger
        {
            private readonly string _categoryName;

            public ConsoleLogger(string categoryName)
            {
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state) => new LoggerScope();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                TextWriter.WriteLine($"[{_categoryName}] [{logLevel}] {formatter(state, exception)}");
            }

            private class LoggerScope : IDisposable
            {
                public void Dispose()
                {

                }
            }
        }
    }
}
