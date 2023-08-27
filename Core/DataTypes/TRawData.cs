using System;

namespace Core.DataTypes {
    public class TRawData : ICloneable {
        public DateTime TimeStamp { get; }
        public string Message { get; }

        public TRawData(string message = "") {
            TimeStamp = DateTime.Now;
            Message = message;
        }

        public object Clone() {
            return new TRawData(Message);
        }
    }
}
