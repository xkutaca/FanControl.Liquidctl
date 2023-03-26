using System.Collections.Generic;
using System.Globalization;

namespace FanControl.Liquidctl
{
    public class LiquidctlStatusJSON
    {
        public class StatusRecord
        {
            public string key { get; set; }
            public string value { get; set; }
            public string unit { get; set; }

            public float? GetValueAsFloat() {
                if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float valueAsFloat))
                    return valueAsFloat;
                return null;
            }
        }
        public string bus { get; set; }
        public string address { get; set; }
        public string port { get; set; }

        public string description { get; set; }

        public List<StatusRecord> status { get; set; }

        public string GetAddress() {
            if (bus.StartsWith("usb"))
                return $"usb#{port}";
            return address;
        }

        public static KeyValuePair<string, string> GetBusAndAddress(string address) {
            if (address.StartsWith("usb#"))
                return new KeyValuePair<string, string>("usb", address.Split('#')[1]);
            return new KeyValuePair<string, string>("hid", address);
        }
    }
}
