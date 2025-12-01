using nanoFramework.Json;
using nanoFramework.M2Mqtt;
using nanoFramework.M2Mqtt.Messages;
using nanoFramework.M5Stack;
using nanoFramework.Networking;
using nanoFramework.Presentation.Media;
using SustainabilityNf.Models;
using SustainabilityNf.Sensors;
using System;
using System.Device.Adc;
using System.Device.Gpio;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Console = nanoFramework.M5Stack.Console;

namespace SustainabilityNf
{
    public class Program
    {
        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            AdcController adc1 = new AdcController();
            GpioController gpioController = new GpioController();
            var moisture = new DfRobotMoistureSensor(adc1, 7);
            var relay = new DfRobotRelay(gpioController, 2);

            M5Core.InitializeScreen();
            Console.Clear();


            M5Core.ButtonLeft.Press += (sender, e) =>
            {
                relay.ToggleRelay();
            };



            CancellationTokenSource cs = new(60000);
            var success = WifiNetworkHelper.ConnectDhcp(PlatformSettings.Ssid, PlatformSettings.Password, requiresDateTime: true, token: cs.Token);
            if (!success)
            {
                Debug.WriteLine($"Can't connect to the network, error: {WifiNetworkHelper.Status}");
                if (WifiNetworkHelper.HelperException != null)
                {
                    Debug.WriteLine($"ex: {WifiNetworkHelper.HelperException}");
                }
            }



            MqttClient mqttc = new MqttClient(PlatformSettings.MQTTBrokerAddress);

            mqttc.MqttMsgPublishReceived += Mqttc_MqttMsgPublishReceived; ;

            mqttc.Connect(PlatformSettings.MQTTClientId);

            if (mqttc.IsConnected)
            {
                mqttc.Subscribe(
                    new[] {
                    "house/sonoff-aircon/tele/SENSOR",
                     "house/enviro-monitor/tele"
                    },
                    new[] {
                        MqttQoSLevel.AtLeastOnce,
                        MqttQoSLevel.AtLeastOnce
                    }
                    );


               
                M5Core.ButtonCenter.Press += (sender, e) =>
                {
                    if (mqttc.IsConnected)
                    {
                        mqttc.Publish("house/sonoff-light/cmnd/power1", Encoding.UTF8.GetBytes("off"));
                    }
                };

                M5Core.ButtonRight.Press += (sender, e) =>
                {
                    if (mqttc.IsConnected)
                    {
                        mqttc.Publish("house/sonoff-light/cmnd/power1", Encoding.UTF8.GetBytes("on"));
                    }
                };






                while (true)
                {

                    var reading = moisture.GetMoisturePercentage();

                    Console.ForegroundColor = Color.Blue;
                    Console.CursorTop = 0;
                    Console.CursorLeft = 12;

                    Console.WriteLine("Office Plant");

                    Console.CursorTop = 2;
                    Console.CursorLeft = 2;

                    Console.ForegroundColor = Color.Yellow;

                    Console.Write("Moisture Percentage");
                    Debug.Write("Moisture Percentage ");

                    Console.ForegroundColor = Color.White;
                    Console.CursorLeft += 2;

                    Console.WriteLine($"{reading} %");
                    Debug.WriteLine($"{reading} %");
                    Thread.Sleep(1000);
                }
            }
        }

        private static void Mqttc_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (e.Message != null)
            {
                if (e.Topic.Contains("enviro-monitor"))
                {
                    var message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

                    Debug.WriteLine(message);

                          EnvironmentalData env = JsonConvert.DeserializeObject(message, typeof(EnvironmentalData)) as EnvironmentalData;

                    if (env != null)
                    {
                        Console.Clear();
                        
                        Console.CursorTop = 4;
                        Console.CursorLeft = 2;
                        Console.ForegroundColor = Color.Green;
                        Console.Write("Temperature");
                        Console.CursorLeft += 2;
                        Console.ForegroundColor = Color.White;
                        Console.WriteLine(env.temperature.ToString("F2") + " C");
                        Debug.WriteLine(env.temperature.ToString("F2") + " C");


                        Console.CursorTop = 5;
                        Console.CursorLeft = 2;
                        Console.ForegroundColor = Color.Green;
                        Console.Write("Pressure");
                        Console.CursorLeft += 5;
                        Console.ForegroundColor = Color.White;
                        Console.Write(env.pressure.ToString("F2") + " kPa");
                        Debug.WriteLine(env.pressure.ToString("F2") + " kPa");

                        Console.CursorTop = 6;
                        Console.CursorLeft = 2;
                        Console.ForegroundColor = Color.Green;
                        Console.Write("Humidity");
                        Console.CursorLeft += 5;
                        Console.ForegroundColor = Color.White;
                        Console.Write(env.humidity.ToString("F2") + " %");
                        Debug.WriteLine(env.humidity.ToString("F2") + " %");


                        Console.CursorTop = 8;
                        Console.CursorLeft = 2;
                        Console.ForegroundColor = Color.Green;
                        Console.Write("Light");
                        Console.CursorLeft += 8;
                        Console.ForegroundColor = Color.White;
                        Console.WriteLine(env.light.ToString("F2") + " Lux");
                        Debug.WriteLine(env.light.ToString("F2") + " Lux");


                        Console.CursorTop = 9;
                        Console.CursorLeft = 2;
                        Console.ForegroundColor = Color.Green;
                        Console.Write("Oxidation");
                        Console.CursorLeft += 4;
                        Console.ForegroundColor = Color.White;
                        Console.WriteLine(env.oxidation.ToString("F2") + " kO");
                        Debug.WriteLine(env.oxidation.ToString("F2") + " kO");

                        Console.CursorTop = 10;
                        Console.CursorLeft = 2;
                        Console.ForegroundColor = Color.Green;
                        Console.Write("Redux");
                        Console.CursorLeft += 8;
                        Console.ForegroundColor = Color.White;
                        Console.WriteLine(env.redu.ToString("F2") + " kO");
                        Debug.WriteLine(env.redu.ToString("F2") + " kO");


                        Console.CursorTop = 11;
                        Console.CursorLeft = 2;
                        Console.ForegroundColor = Color.Green;
                        Console.Write("NH3");
                        Console.CursorLeft += 10;
                        Console.ForegroundColor = Color.White;
                        Console.Write(env.nh3.ToString("F2") + " kO");
                        Debug.WriteLine(env.nh3.ToString("F2") + " kO");

                    }
                }
            }
        }
    }
}
