using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
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

        public (IAudioEngine AudioEngine, ILogger Logger, StreamWriter LogWriter, IConfiguration Configuration) Initialize()
        {
            try
            {
                // Load configuration
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                var configuration = builder.Build();

                // Create logger
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddDebug();
                    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                });
                var logger = loggerFactory.CreateLogger<MainWindow>();

                var logWriter = new StreamWriter("error.log", true);
                LogInfo(logger, logWriter, "AudioEngineInitializer started");

                // Initialize CSCore audio engine
                var audioEngine = new AudioEngine(loggerFactory.CreateLogger<AudioEngine>());

                LogInfo(logger, logWriter, "Using CSCore audio engine");

                return (audioEngine, logger, logWriter, configuration);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error initializing audio engine: {ex.Message}", ex);
            }
        }

        private void LogInfo(ILogger logger, StreamWriter logWriter, string message)
        {
            logger?.LogInformation(message);
            logWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} INFO: {message}");
            logWriter?.Flush();
        }


    }
}