using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DJMixMaster.Audio
{
    public class BeatDetector
    {
        private const int SAMPLE_RATE = 44100;
        private const int CHUNK_SIZE = 1024;
        private const int MIN_BPM = 60;
        private const int MAX_BPM = 200;
        private const double THRESHOLD_MULTIPLIER = 1.3;

        public List<double> BeatPositions { get; private set; }
        public double BPM { get; private set; }

        public BeatDetector()
        {
            BeatPositions = new List<double>();
        }

        public void AnalyzeFile(string filePath)
        {
            using (var audioFile = new AudioFileReader(filePath))
            {
                var samples = new List<float>();
                var buffer = new float[CHUNK_SIZE];
                int bytesRead;

                // Read audio data into samples list
                while ((bytesRead = audioFile.Read(buffer, 0, CHUNK_SIZE)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        samples.Add(Math.Abs(buffer[i]));
                    }
                }

                // Process chunks for energy levels
                var energyChunks = ProcessEnergyChunks(samples);
                
                // Detect beats
                DetectBeats(energyChunks);
                
                // Calculate BPM
                CalculateBPM();
            }
        }

        private List<double> ProcessEnergyChunks(List<float> samples)
        {
            var energyChunks = new List<double>();
            int chunkCount = samples.Count / CHUNK_SIZE;

            for (int i = 0; i < chunkCount; i++)
            {
                double energy = 0;
                int startIndex = i * CHUNK_SIZE;

                for (int j = 0; j < CHUNK_SIZE && (startIndex + j) < samples.Count; j++)
                {
                    energy += Math.Pow(samples[startIndex + j], 2);
                }

                energyChunks.Add(energy);
            }

            return energyChunks;
        }

        private void DetectBeats(List<double> energyChunks)
        {
            BeatPositions.Clear();
            double averageEnergy = energyChunks.Average();
            double threshold = averageEnergy * THRESHOLD_MULTIPLIER;

            for (int i = 1; i < energyChunks.Count - 1; i++)
            {
                if (energyChunks[i] > threshold &&
                    energyChunks[i] > energyChunks[i - 1] &&
                    energyChunks[i] > energyChunks[i + 1])
                {
                    // Convert chunk index to time position in seconds
                    double timePosition = (i * CHUNK_SIZE) / (double)SAMPLE_RATE;
                    BeatPositions.Add(timePosition);
                }
            }
        }

        private void CalculateBPM()
        {
            if (BeatPositions.Count < 2)
            {
                BPM = 0;
                return;
            }

            var intervals = new List<double>();
            for (int i = 1; i < BeatPositions.Count; i++)
            {
                intervals.Add(BeatPositions[i] - BeatPositions[i - 1]);
            }

            // Calculate average interval between beats
            double averageInterval = intervals.Average();
            
            // Convert to BPM
            BPM = Math.Round(60.0 / averageInterval);

            // Ensure BPM is within reasonable range
            while (BPM < MIN_BPM) BPM *= 2;
            while (BPM > MAX_BPM) BPM /= 2;
        }

        public List<double> GetBeatPositionsInRange(double startTime, double endTime)
        {
            return BeatPositions
                .Where(pos => pos >= startTime && pos <= endTime)
                .ToList();
        }
    }
}
