using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using DJMixMaster.Audio;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Test harness for auto-testing RizzAudioEngine dual deck playback.
    /// Simulates MIDI commands: load tracks, play simultaneously, monitor logs.
    /// Compartmentalized for easy removal in stable builds.
    /// </summary>
    public static class RizzAudioEngineTestHarness
    {
        public static void RunDualDeckTest(ILogger logger)
        {
            logger.LogInformation("Starting RizzAudioEngine Dual Deck Auto-Test");

            var engine = new RizzAudioEngine(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<RizzAudioEngine>(), true);

            string testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "assets", "audio", "ThisIsTrash.wav");
            if (!File.Exists(testFile))
            {
                logger.LogError("Test file not found: {File}", testFile);
                return;
            }

            // Simulate loading tracks on both decks
            logger.LogInformation("Loading test file on Deck 1");
            engine.LoadFile(1, testFile);

            logger.LogInformation("Loading test file on Deck 2");
            engine.LoadFile(2, testFile);

            // Simulate playing both decks
            logger.LogInformation("Starting playback on Deck 1");
            engine.Play(1);

            logger.LogInformation("Starting playback on Deck 2");
            engine.Play(2);

            // Monitor for 10 seconds
            logger.LogInformation("Monitoring playback for 10 seconds...");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                double pos1 = engine.GetPosition(1);
                double pos2 = engine.GetPosition(2);
                double len1 = engine.GetLength(1);
                double len2 = engine.GetLength(2);
                logger.LogInformation("Deck1: Pos={Pos1:F2}s/{Len1:F2}s, Deck2: Pos={Pos2:F2}s/{Len2:F2}s", pos1, len1, pos2, len2);
            }

            // Stop playback
            logger.LogInformation("Stopping playback on both decks");
            engine.Pause(1);
            engine.Pause(2);

            logger.LogInformation("Dual Deck Auto-Test completed");
        }
    }
}