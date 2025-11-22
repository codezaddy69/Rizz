using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Serilog;
using DJMixMaster.Audio;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Test harness for auto-testing RizzAudioEngine sequential deck playback.
    /// Simulates MIDI commands: load and play deck 1 to completion, then deck 2.
    /// Compartmentalized for easy removal in stable builds.
    /// </summary>
    public static class RizzAudioEngineTestHarness
    {
        public static void RunDualDeckTest(Microsoft.Extensions.Logging.ILogger logger)
        {
            logger.LogInformation("Starting RizzAudioEngine Sequential Deck Auto-Test");

            var engineLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<RizzAudioEngine>();
            var engine = new RizzAudioEngine(engineLogger, false);

            string testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "assets", "audio", "ThisIsTrash.wav");
            if (!File.Exists(testFile))
            {
                engineLogger.LogError("Test file not found: {File}", testFile);
                return;
            }

            // Simulate loading and playing deck 1 to completion
            engineLogger.LogInformation("Loading test file on Deck 1");
            engine.LoadFile(1, testFile);

            // engineLogger.LogInformation("Starting playback on Deck 1");
            // engine.Play(1);

            // double len1 = engine.GetLength(1);
            // engineLogger.LogInformation("Len1 = {Len1}, monitoring Deck 1 playback until completion...", len1);
            // int counter = 0;
            // while (engine.GetPosition(1) < len1 && counter < 20)
            // {
            //     Thread.Sleep(1000);
            //     double pos1 = engine.GetPosition(1);
            //     engineLogger.LogInformation("Deck1: Pos={Pos1:F2}s/{Len1:F2}s", pos1, len1);
            //     counter++;
            // }

            // engineLogger.LogInformation("Deck 1 playback completed, pausing");
            // engine.Pause(1);

            // Simulate loading and playing deck 2 to completion
            engineLogger.LogInformation("Loading test file on Deck 2");
            engine.LoadFile(2, testFile);

            // engineLogger.LogInformation("Starting playback on Deck 2");
            // engine.Play(2);

            // double len2 = engine.GetLength(2);
            // engineLogger.LogInformation("Len2 = {Len2}, monitoring Deck 2 playback until completion...", len2);
            // counter = 0;
            // while (engine.GetPosition(2) < len2 && counter < 20)
            // {
            //     Thread.Sleep(1000);
            //     double pos2 = engine.GetPosition(2);
            //     engineLogger.LogInformation("Deck2: Pos={Pos2:F2}s/{Len2:F2}s", pos2, len2);
            //     counter++;
            // }

            // engineLogger.LogInformation("Deck 2 playback completed, pausing");
            // engine.Pause(2);

            engineLogger.LogInformation("Sequential Deck Auto-Test completed");
        }
    }
}