using System;
using System.IO;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Core.Tests.Xunit
{
    public class XUnitTestOutputSink : ILogEventSink
    {
        readonly ITestOutputHelper _output;
        readonly ITextFormatter _textFormatter;

        public XUnitTestOutputSink(ITestOutputHelper testOutputHelper, ITextFormatter textFormatter)
        {
            if (testOutputHelper == null) throw new ArgumentNullException("testOutputHelper");
            if (textFormatter == null) throw new ArgumentNullException("textFormatter");

            _output = testOutputHelper;
            _textFormatter = textFormatter;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException("logEvent");

            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);
            _output.WriteLine(renderSpace.ToString());
        }
    }

    public static class LoggerConfigurationXunitTestOutputExtensions
    {
        const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}";

        /// <summary>
        /// Adds a sink that writes log events to the output of an xUnit test.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="testOutputHelper">Xunit <see cref="TestOutputHelper"/> that writes to test output</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="outputTemplate">Message template describing the output format.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration XunitTestOutput(
            this LoggerSinkConfiguration loggerConfiguration,
            ITestOutputHelper testOutputHelper,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException("loggerConfiguration");
            if (testOutputHelper == null) throw new ArgumentNullException("testOutputHelper");
            if (outputTemplate == null) throw new ArgumentNullException("outputTemplate");

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            return loggerConfiguration.Sink(new XUnitTestOutputSink(testOutputHelper, formatter), restrictedToMinimumLevel);
        }
    }
}