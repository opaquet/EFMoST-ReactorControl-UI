using System;

namespace Core.DataTypes {
    public class TComputedValuesData : ICloneable {
        public DateTime TimeStamp { get; }
        public double Ethanol { get; }
        public double H2S { get; }

        public TComputedValuesData(double ethanol = 0, double h2s = 0) {
            Ethanol = ethanol;
            H2S = h2s;
            TimeStamp = DateTime.Now;
        }

        public object Clone() {
            return new TComputedValuesData(Ethanol, H2S);
        }
    }
}
