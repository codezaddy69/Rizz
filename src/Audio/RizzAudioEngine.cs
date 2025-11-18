using System;
using Microsoft.Extensions.Logging;
using DJMixMaster.Audio;

namespace DJMixMaster.Audio
{
    public class RizzAudioEngine : IAudioEngine
    {
        private readonly ILogger<RizzAudioEngine> _logger;
        private readonly RizzApplication _rizzApp;
        private readonly Deck[] _decks;

        public RizzAudioEngine(ILogger<RizzAudioEngine> logger)
        {
            _logger = logger;
            _rizzApp = new RizzApplication(logger);
            _rizzApp.Initialize();

            // Create decks
            _decks = new Deck[2];
            for (int i = 0; i < _decks.Length; i++)
            {
                _decks[i] = new Deck($"Deck{i + 1}", logger);
            }

            _logger.LogInformation("RizzAudioEngine initialized");
        }

        public void LoadFile(int deckNumber, string filePath)
        {
            if (deckNumber < 1 || deckNumber > _decks.Length) return;

            var deck = _decks[deckNumber - 1];
            deck.LoadFile(filePath);

            // TODO: Integrate with Rizz EngineMixer
            _logger.LogInformation("Loaded file {Path} on deck {Deck}", filePath, deckNumber);
        }

        public void Play(int deckNumber)
        {
            if (deckNumber < 1 || deckNumber > _decks.Length) return;

            var deck = _decks[deckNumber - 1];
            deck.Play();

            _logger.LogInformation("Play requested on deck {Deck}", deckNumber);
        }

        public void Pause(int deckNumber)
        {
            if (deckNumber < 1 || deckNumber > _decks.Length) return;

            var deck = _decks[deckNumber - 1];
            deck.Pause();

            _logger.LogInformation("Pause requested on deck {Deck}", deckNumber);
        }

        public void Seek(int deckNumber, double seconds)
        {
            if (deckNumber < 1 || deckNumber > _decks.Length) return;

            var deck = _decks[deckNumber - 1];
            deck.Seek(seconds);

            _logger.LogInformation("Seek to {Seconds}s on deck {Deck}", seconds, deckNumber);
        }

        public double GetPosition(int deckNumber)
        {
            if (deckNumber < 1 || deckNumber > _decks.Length) return 0;

            return _decks[deckNumber - 1].Position;
        }

        public double GetLength(int deckNumber)
        {
            if (deckNumber < 1 || deckNumber > _decks.Length) return 0;

            return _decks[deckNumber - 1].Length;
        }

        public void SetVolume(int deckNumber, float volume)
        {
            if (deckNumber < 1 || deckNumber > _decks.Length) return;

            _decks[deckNumber - 1].Volume = volume;
        }

        public void Dispose()
        {
            _rizzApp.Shutdown();
            foreach (var deck in _decks)
            {
                deck.Dispose();
            }
        }
    }
}