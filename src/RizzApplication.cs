using System;
using Microsoft.Extensions.Logging;
using DJMixMaster.Audio;

namespace DJMixMaster
{
    public class RizzApplication
    {
        private readonly ILogger<RizzApplication> _logger;
        private SoundManager? _soundManager;
        private EngineMixer? _engineMixer;
        private EngineWorkerScheduler? _workerScheduler;

        public RizzApplication(ILogger<RizzApplication> logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            _logger.LogInformation("Initializing Rizz Application");

            // Initialize components with separate loggers
            var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            _soundManager = new SoundManager(loggerFactory.CreateLogger<SoundManager>());
            _soundManager.Initialize();

            _engineMixer = new EngineMixer(loggerFactory.CreateLogger<EngineMixer>(), 44100, 1024);
            _workerScheduler = new EngineWorkerScheduler(loggerFactory.CreateLogger<EngineWorkerScheduler>());

            _logger.LogInformation("Rizz Application initialized");
        }

        public void Shutdown()
        {
            _workerScheduler?.Shutdown();
            _soundManager?.Shutdown();
            _logger.LogInformation("Rizz Application shutdown");
        }

        public SoundManager? SoundManager => _soundManager;
        public EngineMixer? EngineMixer => _engineMixer;
        public EngineWorkerScheduler? WorkerScheduler => _workerScheduler;
    }
}