using System;
using System.Windows.Controls;

namespace DJMixMaster.Controls
{
    public class FaderControl : Slider
    {
        public FaderControl()
        {
            // Set default properties for the fader
            Minimum = 0;
            Maximum = 100;
            Value = 100; // Start at full volume
        }

        // You can add additional properties or methods if needed
    }
}
