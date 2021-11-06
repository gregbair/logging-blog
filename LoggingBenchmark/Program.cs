using System;
using System.IO;
using System.Reflection;using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NLog;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NLog.LogLevel;
using NLogManager = NLog.LogManager;

RunBenchmarks();

void RunBenchmarks()
{
    var defaultOut = Console.Out;
    Console.SetOut(TextWriter.Null);
    BenchmarkRunner.Run<ConsoleLogBenchmark>();

    log4net.LogManager.ResetConfiguration();
    Console.SetOut(defaultOut);

    BenchmarkRunner.Run<FileLogBenchmark>();
}

public class ConsoleLogBenchmark
{
    private readonly ILog _log4NetLogger;
    private readonly Serilog.Core.Logger _serilogLogger;
    private readonly Logger _nlogLogger;
    private readonly ILogger _msLogger;

    public ConsoleLogBenchmark()
    {
        var baseDir = Directory.GetParent(Assembly.GetEntryAssembly()!.Location)!.ToString();
        
        // set up log4net
        XmlConfigurator.Configure(new FileInfo(Path.Combine(baseDir, "log4net-console.xml")));
        _log4NetLogger = log4net.LogManager.GetLogger(typeof(ConsoleLogBenchmark));

        // set up NLog
        var nLogConfig = new NLog.Config.LoggingConfiguration();
        var console = new NLog.Targets.ConsoleTarget();
        nLogConfig.AddRule(LogLevel.Trace, LogLevel.Fatal, console);
        var factory = new LogFactory(nLogConfig);
        _nlogLogger = factory.GetLogger(nameof(ConsoleLogBenchmark));

        // set up Serilog
        _serilogLogger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        var builder = new MyLoggingBuilder();
        builder.AddSimpleConsole(opt =>
        {
            opt.ColorBehavior = LoggerColorBehavior.Disabled;
        });
        var sp = builder.Services.BuildServiceProvider(new ServiceProviderOptions
            { ValidateScopes = true, ValidateOnBuild = true });
        var loggerProvider = sp.GetRequiredService<ILoggerProvider>();
        _msLogger = loggerProvider.CreateLogger(nameof(ConsoleLogBenchmark));
    }

    [Benchmark]
    public void Log4Net() => _log4NetLogger.Error("This is my error");

    [Benchmark]
    public void Serilog() => _serilogLogger.Error("This is my error");

    [Benchmark]
    public void NLog() => _nlogLogger.Error("This is my error");

        [Benchmark]
    public void MsLogger() => _msLogger.LogError("This is my error");
}

public class FileLogBenchmark
{
    private readonly ILog _log4NetLogger;
    private readonly Serilog.Core.Logger _serilogLogger;
    private readonly Logger _nlogLogger;
    private readonly ILogger _msLogger;

    public FileLogBenchmark()
    {
        var baseDir = Directory.GetParent(Assembly.GetEntryAssembly()!.Location)!.ToString();
        var baseLogDir = Path.Combine(baseDir, "logs");

        // set up log4net
        XmlConfigurator.Configure(new FileInfo(Path.Combine(baseDir, "log4net-file.xml")));
        _log4NetLogger = log4net.LogManager.GetLogger(typeof(ConsoleLogBenchmark));

        // set up NLog
        var nLogConfig = new NLog.Config.LoggingConfiguration();
        var fileTarget = new NLog.Targets.FileTarget() { FileName = Path.Combine(baseLogDir, "nlog.log"), KeepFileOpen = true, ConcurrentWrites = false};
        nLogConfig.AddRule(LogLevel.Trace, LogLevel.Fatal, fileTarget);
        var factory = new LogFactory(nLogConfig);
        _nlogLogger = factory.GetLogger(nameof(ConsoleLogBenchmark));

        // set up Serilog
        _serilogLogger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(baseLogDir, "serilog.log"))
            .CreateLogger();

        var builder = new MyLoggingBuilder();
        builder.AddFile(Path.Combine(baseLogDir, "msLog.log"));
        var sp = builder.Services.BuildServiceProvider(new ServiceProviderOptions
            { ValidateScopes = true, ValidateOnBuild = true });
        var loggerProvider = sp.GetRequiredService<ILoggerProvider>();
        _msLogger = loggerProvider.CreateLogger(nameof(ConsoleLogBenchmark));
    }

    [Benchmark]
    public void Log4Net() => _log4NetLogger.Error("This is my error");


    [Benchmark]
    public void Serilog() => _serilogLogger.Error("This is my error");

    [Benchmark]
    public void NLog() => _nlogLogger.Error("This is my error");

    [Benchmark]
    public void MsLog() => _msLogger.LogError("This is my error");
}

public class MyLoggingBuilder : ILoggingBuilder
{
    public IServiceCollection Services { get; }

    public MyLoggingBuilder()
    {
        Services = new ServiceCollection();
    }
}