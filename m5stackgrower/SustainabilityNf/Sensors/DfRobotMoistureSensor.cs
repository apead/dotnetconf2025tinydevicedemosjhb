using SustainabilityNf.Enums;
using System;
using System.Device.Adc;
using System.Diagnostics;

namespace SustainabilityNf.Sensors
{
    public class DfRobotMoistureSensor
    {
        private AdcController _controller;
        private AdcChannel _moistureAnalog;
        private int _pin;


        const int AirValue = 2900;
        const int WaterValue = 1300;

        public DfRobotMoistureSensor(AdcController controller, int pin)
        {
            _controller = controller;
            _pin = pin;

            _moistureAnalog = _controller.OpenChannel(pin);

            int max1 = controller.MaxValue;
            int min1 = controller.MinValue;

            Debug.WriteLine("min1=" + min1.ToString() + " max1=" + max1.ToString());
        }

        public int GetSensorReading()
        {
            return _moistureAnalog.ReadValue();
        }

        private double Map(double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        public int GetMoisturePercentage()
        {
            int soilMoistureValue = GetSensorReading();

            double moisture1_value = Map(soilMoistureValue, AirValue, WaterValue, 0, 100);

            if (moisture1_value < 0)
                moisture1_value = 0;

            if (moisture1_value > 100)
                moisture1_value = 100;

            return (int) Math.Round(moisture1_value);
        }

        public MoistureDegree GetMoistureReading()
        {
            int intervals = (AirValue - WaterValue) / 3;

            int soilMoistureValue = GetSensorReading();

            if (soilMoistureValue > WaterValue && soilMoistureValue < (WaterValue + intervals))
            {
                return MoistureDegree.VeryWet;
            }
            else if (soilMoistureValue > (WaterValue + intervals) && soilMoistureValue < (AirValue - intervals))
            {
                return MoistureDegree.Wet;
            }
            else if (soilMoistureValue < AirValue && soilMoistureValue > (AirValue - intervals))
            {
                return MoistureDegree.Dry;
            }

            return MoistureDegree.Unknown;
        }
    }
}