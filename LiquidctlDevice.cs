using System;
using System.Linq;
using FanControl.Plugins;

namespace FanControl.Liquidctl
{
    internal class LiquidctlDevice
    {
        public class LiquidTemperature : IPluginSensor
        {
            public LiquidTemperature(LiquidctlStatusJSON output)
            {
                _id = $"{output.GetAddress().ToLower()}-liqtmp";
                _name = $"Liquid Temp. - {output.description}";
                UpdateFromJSON(output);
            }
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                _value = (float)output.status.Single(entry => entry.key == "Liquid temperature").GetValueAsFloat();
            }
            public string Id => _id;
            string _id;

            public string Name => _name;
            string _name;

            public float? Value => _value;
            float _value;
            
            public void Update()
            { } // plugin updates sensors
        }
        public class PumpSpeed : IPluginSensor
        {
            public PumpSpeed(LiquidctlStatusJSON output)
            {
                _id = $"{output.GetAddress().ToLower()}-pumprpm";
                _name = $"Pump - {output.description}";
                UpdateFromJSON(output);
            }
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                _value = (float)output.status.Single(entry => entry.key == "Pump speed").GetValueAsFloat();
            }
            public string Id => _id;
            readonly string _id;

            public string Name => _name;
            readonly string _name;

            public float? Value => _value;
            float _value;

            public void Update()
            { } // plugin updates sensors
        }
        public class PumpDuty : IPluginControlSensor
        {
            public PumpDuty(LiquidctlStatusJSON output)
            {
                _address = output.GetAddress();
                _id = $"{_address.ToLower()}-pumpduty";
                _name = $"Pump Control - {output.description}";
                UpdateFromJSON(output);
            }
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                _value = (float)output.status.Single(entry => entry.key == "Pump duty").GetValueAsFloat();
            }
            public string Id => _id;
            string _id;
            string _address;

            public string Name => _name;
            string _name;

            public float? Value => _value;
            float _value;

            public void Reset()
            {
                Set(100.0f);
            }

            public void Set(float val)
            {
                LiquidctlCLIWrapper.SetPump(_address, (int) val);
            }

            public void Update()
            { } // plugin updates sensors

        }
        public LiquidctlDevice(LiquidctlStatusJSON output)
        {
            address = output.GetAddress();

            hasPumpSpeed = output.status.Exists(entry => entry.key == "Pump speed" && !(entry.GetValueAsFloat() is null));
            if (hasPumpSpeed)
                pumpSpeed = new PumpSpeed(output);

            hasPumpDuty = output.status.Exists(entry => entry.key == "Pump duty" && !(entry.GetValueAsFloat() is null));
            if (hasPumpDuty)
                pumpDuty = new PumpDuty(output);

            hasLiquidTemperature = output.status.Exists(entry => entry.key == "Liquid temperature" && !(entry.GetValueAsFloat() is null));
            if (hasLiquidTemperature)
                liquidTemperature = new LiquidTemperature(output);
        }

        public readonly bool hasPumpSpeed, hasPumpDuty, hasLiquidTemperature;

        public void UpdateFromJSON(LiquidctlStatusJSON output)
        {
            if (hasLiquidTemperature) liquidTemperature.UpdateFromJSON(output);
            if (hasPumpSpeed) pumpSpeed.UpdateFromJSON(output);
            if (hasPumpDuty) pumpDuty.UpdateFromJSON(output);
        }

        public string address;
        public LiquidTemperature liquidTemperature;
        public PumpSpeed pumpSpeed;
        public PumpDuty pumpDuty;

        public void LoadJSON()
        {
            try
            {
                LiquidctlStatusJSON output = LiquidctlCLIWrapper.ReadStatus(address).First();
                UpdateFromJSON(output);
            }
            catch (InvalidOperationException)
            {
                throw new Exception($"Device {address} not showing up");
            }
        }
    }
}
