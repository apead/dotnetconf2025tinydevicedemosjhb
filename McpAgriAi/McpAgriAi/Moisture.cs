// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Device.Adc;
using System.Diagnostics;
using nanoFramework.WebServer.Mcp;
using SustainabilityNf.Sensors;

namespace McpAi
{
    public static class Moisture
    {
        private static DfRobotMoistureSensor _moistureSensor;
        private static string _location = "dot net conf joburg";

        public static void Initialize()
        {
            AdcController adcController = new AdcController();
            _moistureSensor = new DfRobotMoistureSensor(adcController, 7);
        }

        [McpServerTool("get_moisture", "Get the moisture reading from the sensor. Check the location to make sure it's the proper location first.")]
        public static string GetMoisture()
        {
            if (_moistureSensor == null)
            {
                Initialize();
            }

            int moisturePercentage = _moistureSensor.GetMoisturePercentage();
            string moistureReading = _moistureSensor.GetMoistureReading().ToString();
            
            Debug.WriteLine($"Getting moisture at location: {_location}, Percentage: {moisturePercentage}%, Reading: {moistureReading}");
            
            return $"Moisture at {_location}: {moisturePercentage}% ({moistureReading})";
        }

        [McpServerTool("get_moisture_location", "Get the location of the moisture sensor. Check this before reading moisture.")]
        public static string GetLocation()
        {
            Debug.WriteLine($"Getting the location of the moisture sensor: {_location}");
            return _location;
        }

        [McpServerTool("set_moisture_location", "Change the location of the moisture sensor. Do not change the location unless the user asks you to change the location.")]
        public static void SetLocation(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentException("Location cannot be null or empty.", nameof(location));
            }

            Debug.WriteLine($"Setting the location of the moisture sensor to: {location}");
            _location = location;
        }
    }
}
