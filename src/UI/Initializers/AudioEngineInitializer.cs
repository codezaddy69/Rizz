using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using DJMixMaster.Audio;

namespace DJMixMaster.UI.Initializers
{
    public class AudioEngineInitializer
    {
        private readonly ILogger<AudioEngineInitializer> _logger;

        public AudioEngineInitializer(ILogger<AudioEngineInitializer> logger)
        {
            _logger = logger;
        }

        public (IAudioEngine AudioEngine, Microsoft.Extensions.Logging.ILogger Logger, StreamWriter LogWriter, IConfiguration Configuration, bool AsioSetupRequired) Initialize()
        {
            try
            {
                LogBootStatus("Config", "START", "Loading configuration");

                // Auto-populate config with defaults if not exists
                string configPath = "appsettings.json";
                string defaultConfigPath = "appsettings.default.json";
                if (!File.Exists(configPath) && File.Exists(defaultConfigPath))
                {
                    File.Copy(defaultConfigPath, configPath);
                    LogBootStatus("Config", "AUTO_POPULATED", "Default settings applied");
                }

                // Load configuration
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                var configuration = builder.Build();

                LogBootStatus("Config", "LOADED", "Configuration ready");

                // Configure Serilog
                Directory.CreateDirectory("logs");
                LogBootStatus("Logging", "START", "Configuring Serilog");
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File("logs/djmixmaster_.log",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File("logs/audio_performance_.log",
                        rollingInterval: RollingInterval.Day,
                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}")
                    .CreateLogger();
                LogBootStatus("Logging", "CONFIGURED", "Serilog ready");

                // Create Microsoft.Extensions.Logging logger from Serilog
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog();
                    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                });
                var logger = loggerFactory.CreateLogger<MainWindow>();

                Directory.CreateDirectory("logs");
                var logWriter = new StreamWriter("logs/audio_engine.log", true);
                LogInfo(logger, logWriter, "AudioEngineInitializer started");

                 // Initialize audio engine
                 LogBootStatus("AudioEngine", "START", "Initializing NAudio engine");
                 IAudioEngine audioEngine;
                 bool asioSetupRequired = false;

                 audioEngine = new AudioEngine(loggerFactory.CreateLogger<AudioEngine>());
                 LogBootStatus("AudioEngine", "CREATED", "NAudio engine ready");

                  // Check if ASIO setup is required
                  asioSetupRequired = (audioEngine as AudioEngine)?.AsioFailed ?? false;

                 LogInfo(logger, logWriter, "Using NAudio audio engine");

                return (audioEngine, logger, logWriter, configuration, asioSetupRequired);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error initializing audio engine: {ex.Message}", ex);
            }
        }

        private void LogInfo(Microsoft.Extensions.Logging.ILogger logger, StreamWriter logWriter, string message)
        {
            logger?.LogInformation(message);
            logWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} INFO: {message}");
            logWriter?.Flush();
        }

        private void LogBootStatus(string component, string status, string details = "")
        {
            var logEntry = $"BOOT | {DateTime.Now:HH:mm:ss.fff} | {component} | {status}";
            if (!string.IsNullOrEmpty(details)) logEntry += $" | {details}";

            // Write to boot log
            Directory.CreateDirectory("logs");
            using (var bootWriter = new StreamWriter("logs/boot.log", true))
            {
                bootWriter.WriteLine(logEntry);
                bootWriter.Flush();
            }

            // Also to main log
            _logger?.LogInformation("Boot: {Component} - {Status} {Details}", component, status, details);
        }


    }
}