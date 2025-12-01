using Iot.Device.Hts221;
using Iot.Device.Lis2mdl;
using Iot.Device.Lps22Hb;
using Iot.Device.Lsm6dsl;
using Iot.Device.Ssd1306;
using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Diagnostics;
using System.Threading;
using UnitsNet;

Debug.WriteLine("Hello from MXCHIP!");

// Button state variables
int displayMode = 0; // 0 = normal, 1 = detailed
int buttonBPressCount = 0;
string lastButtonMessage = "";

// Button event handlers
void ButtonA_ValueChanged(object sender, PinValueChangedEventArgs e)
{
    if (e.ChangeType == PinEventTypes.Falling) // Button pressed (active low)
    {
        displayMode = (displayMode + 1) % 2; // Toggle between 0 and 1
        lastButtonMessage = $"Btn A: Mode {displayMode}";
        Debug.WriteLine(lastButtonMessage);
    }
}

void ButtonB_ValueChanged(object sender, PinValueChangedEventArgs e)
{
    if (e.ChangeType == PinEventTypes.Falling) // Button pressed (active low)
    {
        buttonBPressCount++;
        lastButtonMessage = $"Btn B: {buttonBPressCount}x";
        Debug.WriteLine(lastButtonMessage);
    }
}

I2cConnectionSettings settingsLps = new(1, 0x5C);
I2cConnectionSettings settingsHts = new(1, 0x5F);
I2cConnectionSettings settingsLsm = new(1, 0x6A);
I2cConnectionSettings settingsLis = new(1, 0x1E);
I2cConnectionSettings settingsOled = new(1, 0x3C); // OLED display

// Initialize GPIO controller and button pins
Debug.WriteLine("Initializing buttons...");
var gpioController = new GpioController();

// Button A on GPIO pin 4 (PA_4)
var buttonA = gpioController.OpenPin(4, PinMode.Input);
buttonA.DebounceTimeout = TimeSpan.FromMilliseconds(50);
buttonA.ValueChanged += ButtonA_ValueChanged;
Debug.WriteLine("Button A initialized on pin 4");

// Button B on GPIO pin 10 (PA_10)
var buttonB = gpioController.OpenPin(10, PinMode.Input);
buttonB.DebounceTimeout = TimeSpan.FromMilliseconds(50);
buttonB.ValueChanged += ButtonB_ValueChanged;
Debug.WriteLine("Button B initialized on pin 10");

Debug.WriteLine("Starting main loop...");

//Debug.WriteLine("Starting main loop...");

while (true)
{
    try
    {
        double temperature = 0;
        double humidity = 0;
        double pressure = 0;
        double accelMagnitude = 0;
        double magMagnitude = 0;

        // Read pressure sensor
        Debug.WriteLine("Reading pressure...");
        using (var i2cDeviceLps = I2cDevice.Create(settingsLps))
        using (Lps22Hb lps22HdDevice = new(i2cDeviceLps, FifoMode.Bypass))
        {
            var pressureValue = lps22HdDevice.Pressure;
            pressure = pressureValue.Hectopascals;
            Debug.WriteLine($"Pressure: {pressure:F1}hPa");
        }

        Thread.Sleep(200); // Wait after dispose

        // Read temp/humidity sensor
        Debug.WriteLine("Reading temperature/humidity...");
        using (var i2cDeviceHts = I2cDevice.Create(settingsHts))
     
        
        using (Hts221 th = new(i2cDeviceHts))
        {
            var tempValue = th.Temperature;
            var humValue = th.Humidity;
            temperature = tempValue.DegreesCelsius;
            humidity = humValue.Percent;
            Debug.WriteLine($"Temperature: {temperature:F1}\u00B0C");
            Debug.WriteLine($"Relative humidity: {humidity:F1}%");
        }

        Thread.Sleep(200); // Wait after dispose

        // Read IMU sensor
        using (var i2cDeviceLsm = I2cDevice.Create(settingsLsm))
        using (Lsm6dsl imu = new(i2cDeviceLsm))
        {
            var accel = imu.GetAcceleration();
            var gyro = imu.GetGyroscope();
            var temp = imu.GetTemperature();

            accelMagnitude = Math.Sqrt(accel.X * accel.X + accel.Y * accel.Y + accel.Z * accel.Z);
            Debug.WriteLine($"Accel: X={accel.X:F2} Y={accel.Y:F2} Z={accel.Z:F2} m/s²");
            Debug.WriteLine($"Gyro: X={gyro.X:F2} Y={gyro.Y:F2} Z={gyro.Z:F2} °/s");
            Debug.WriteLine($"Temp: {temp:F1}°C");
        }

        Thread.Sleep(200); // Wait after dispose

        // Read magnetometer sensor
        Debug.WriteLine("Reading magnetometer...");
     
        using (var i2cDeviceLis = I2cDevice.Create(settingsLis))
        using (Lis2mdl mag = new(i2cDeviceLis))
        {
            var field = mag.GetMagneticField();
            var magTemp = mag.GetTemperature();

            magMagnitude = field.Magnitude;
            Debug.WriteLine($"Mag: X={field.X:F2} Y={field.Y:F2} Z={field.Z:F2} µT");
            Debug.WriteLine($"Mag Magnitude: {magMagnitude:F2} µT");
        }

        Thread.Sleep(200); // Wait after dispose

        // Update display
        Debug.WriteLine("Updating display...");
        try
        {
            using (var i2cDeviceOled = I2cDevice.Create(settingsOled))
         
            
            using (Ssd1306 display = new(i2cDeviceOled))
            {
                display.Clear();
                
                if (displayMode == 0)
                {
                    // Normal mode
                    // Yellow section (0-15): Title
                    display.DrawText(0, 0, "NanoF is awesome!");
                    display.DrawText(0, 8, $"{temperature:F1}C {humidity:F0}%RH");
                    
                    // Blue section (16-63): Sensor data
                    display.DrawText(0, 18, $"P:{pressure:F0}hPa");
                    display.DrawText(0, 30, $"A:{accelMagnitude:F2}");
                    display.DrawText(0, 42, $"M:{magMagnitude:F1}uT");
                    
                    // Show last button message if any
                    if (lastButtonMessage.Length > 0)
                    {
                        display.DrawText(0, 54, lastButtonMessage);
                    }
                }
                else
                {
                    // Detailed mode (Button A toggles to this)
                    display.DrawText(0, 0, "Detail View");
                    display.DrawText(0, 10, $"T:{temperature:F1}C");
                    display.DrawText(0, 20, $"H:{humidity:F0}%");
                    display.DrawText(0, 30, $"P:{pressure:F1}hPa");
                    display.DrawText(0, 40, $"Acc:{accelMagnitude:F2}");
                    display.DrawText(0, 50, $"Mag:{magMagnitude:F1}uT");
                }
                
                display.Display();
            }
            Debug.WriteLine("Display updated");
        }
        catch (Exception displayEx)
        {
            Debug.WriteLine($"Display update failed: {displayEx.Message}");
        }

        Thread.Sleep(200); // Wait after dispose
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error: {ex.Message}");
    }

    Thread.Sleep(1000);
}