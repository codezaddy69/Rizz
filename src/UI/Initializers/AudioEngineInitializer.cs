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

        public (IAudioEngine AudioEngine, AudioEngineType EngineType, ILogger Logger, StreamWriter LogWriter, IConfiguration Configuration) Initialize()
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

                // Initialize audio engine with fallback detection
                var factory = new AudioEngineFactory();
                var currentAudioEngineType = factory.DetectWorkingEngine(loggerFactory);
                var audioEngine = factory.CreateAudioEngine(currentAudioEngineType, loggerFactory);

                // Save the detected engine type to config
                SaveAudioEngineTypeToConfig(currentAudioEngineType, logger, logWriter);

                LogInfo(logger, logWriter, $"Using audio engine: {currentAudioEngineType}");

                return (audioEngine, currentAudioEngineType, logger, logWriter, configuration);
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

        private void SaveAudioEngineTypeToConfig(AudioEngineType engineType, ILogger logger, StreamWriter logWriter)
        {
            try
            {
                var configPath = "appsettings.json";
                var json = File.ReadAllText(configPath);
                dynamic? config = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

                // For simplicity, we'll just overwrite the file with the new setting
                var updatedJson = $$"""
                {
                  "PluginFolder": "C:\\Program Files\\Image-Line\\FL Studio 20\\Plugins\\VST",
                  "AudioEngineType": "{{engineType}}"
                }
                """;

                File.WriteAllText(configPath, updatedJson);
                LogInfo(logger, logWriter, $"Saved audio engine type to config: {engineType}");
            }
            catch (Exception ex)
            {
                LogInfo(logger, logWriter, $"Error saving audio engine type to config: {ex}");
            }
        }
    }
}