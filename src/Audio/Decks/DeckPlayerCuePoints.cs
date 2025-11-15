using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    public class DeckPlayerCuePoints
    {
        private readonly ILogger logger;
        private readonly List<double> cuePoints;
        private readonly DeckPlayer deckPlayer;

        public DeckPlayerCuePoints(DeckPlayer deckPlayer, ILogger logger)
        {
            this.deckPlayer = deckPlayer;
            this.logger = logger;
            cuePoints = new List<double>();
        }

        public void AddCuePoint(double position)
        {
            if (!cuePoints.Contains(position))
            {
                cuePoints.Add(position);
                logger.LogDebug($"Adding cue point at position {position}");
            }
        }

        public void JumpToCuePoint(int index)
        {
            if (index >= 0 && index < cuePoints.Count)
            {
                deckPlayer.Seek(TimeSpan.FromSeconds(cuePoints[index]));
                logger.LogDebug($"Jumping to cue point at position {cuePoints[index]}");
            }
            else
            {
                logger.LogWarning($"Invalid cue point index: {index}");
            }
        }

        public void RemoveCuePoint(int index)
        {
            if (index >= 0 && index < cuePoints.Count)
            {
                var position = cuePoints[index];
                cuePoints.RemoveAt(index);
                logger.LogDebug($"Removed cue point at position {position}");
            }
        }

        public double[] GetCuePoints()
        {
            return cuePoints.ToArray();
        }

        public void ClearCuePoints()
        {
            cuePoints.Clear();
            logger.LogDebug("Cleared all cue points");
        }
    }
}
