using System;

namespace DJMixMaster.Controls
{
    public class Fader
    {
        private double value = 0;
        private double maximum = 100;
        private double minimum = 0;

        public event EventHandler<double>? ValueChanged;

        public double Value
        {
            get => value;
            set
            {
                if (this.value != value)
                {
                    this.value = Math.Clamp(value, minimum, maximum);
                    ValueChanged?.Invoke(this, this.value);
                }
            }
        }

        public double Maximum
        {
            get => maximum;
            set
            {
                if (value > minimum)
                {
                    maximum = value;
                    Value = Math.Clamp(this.value, minimum, maximum);
                }
            }
        }

        public double Minimum
        {
            get => minimum;
            set
            {
                if (value < maximum)
                {
                    minimum = value;
                    Value = Math.Clamp(this.value, minimum, maximum);
                }
            }
        }
    }
}
