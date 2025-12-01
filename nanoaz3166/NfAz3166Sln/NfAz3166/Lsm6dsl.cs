using System;
using System.Device.I2c;
using System.Diagnostics;

namespace Iot.Device.Lsm6dsl
{
    /// <summary>
    /// LSM6DSL - 3D accelerometer and 3D gyroscope
    /// </summary>
    public class Lsm6dsl : IDisposable
    {
        private I2cDevice _i2cDevice;
        
        /// <summary>
        /// Default I2C address (0x6A when SA0 is low, 0x6B when SA0 is high)
        /// </summary>
        public const byte DefaultI2cAddress = 0x6A;
        
        /// <summary>
        /// Device ID from WHO_AM_I register
        /// </summary>
        public const byte DeviceId = 0x6A;

        /// <summary>
        /// Initializes a new instance of the LSM6DSL class
        /// </summary>
        /// <param name="i2cDevice">I2C device</param>
        public Lsm6dsl(I2cDevice i2cDevice)
        {
            _i2cDevice = i2cDevice ?? throw new ArgumentNullException(nameof(i2cDevice));
            
            // Verify device ID
            byte whoAmI = ReadByte(Register.WHO_AM_I);
            if (whoAmI != DeviceId)
            {
                throw new Exception($"Invalid device ID. Expected 0x{DeviceId:X2}, got 0x{whoAmI:X2}");
            }
            
            // Initialize device
            Initialize();
        }

        private void Initialize()
        {
            // Configure accelerometer: ODR = 104 Hz, ±2g, LPF 400 Hz
            WriteByte(Register.CTRL1_XL, 0x40); // 0100 0000
            
            // Configure gyroscope: ODR = 104 Hz, 250 dps
            WriteByte(Register.CTRL2_G, 0x40);  // 0100 0000
            
            // Enable BDU (Block Data Update)
            WriteByte(Register.CTRL3_C, 0x44);  // 0100 0100
        }

        /// <summary>
        /// Read acceleration in m/s²
        /// </summary>
        public AccelerationData GetAcceleration()
        {
            byte[] data = new byte[6];
            ReadBytes(Register.OUTX_L_XL, data);
            
            // Convert to signed 16-bit values
            short x = (short)(data[0] | (data[1] << 8));
            short y = (short)(data[2] | (data[3] << 8));
            short z = (short)(data[4] | (data[5] << 8));
            
            // Convert to m/s² (sensitivity at ±2g is 0.061 mg/LSB)
            double sensitivity = 0.061 / 1000.0 * 9.80665; // Convert mg to m/s²
            
            return new AccelerationData(
                x * sensitivity,
                y * sensitivity,
                z * sensitivity
            );
        }

        /// <summary>
        /// Read gyroscope angular rate in degrees/second
        /// </summary>
        public GyroscopeData GetGyroscope()
        {
            byte[] data = new byte[6];
            ReadBytes(Register.OUTX_L_G, data);
            
            // Convert to signed 16-bit values
            short x = (short)(data[0] | (data[1] << 8));
            short y = (short)(data[2] | (data[3] << 8));
            short z = (short)(data[4] | (data[5] << 8));
            
            // Convert to degrees/second (sensitivity at 250 dps is 8.75 mdps/LSB)
            double sensitivity = 8.75 / 1000.0; // Convert mdps to dps
            
            return new GyroscopeData(
                x * sensitivity,
                y * sensitivity,
                z * sensitivity
            );
        }

        /// <summary>
        /// Read temperature in Celsius
        /// </summary>
        public double GetTemperature()
        {
            byte[] data = new byte[2];
            ReadBytes(Register.OUT_TEMP_L, data);
            
            short temp = (short)(data[0] | (data[1] << 8));
            
            // Temperature in °C = (Output / 256) + 25
            return (temp / 256.0) + 25.0;
        }

        private void WriteByte(Register register, byte value)
        {
            byte[] data = new byte[] { (byte)register, value };
            _i2cDevice.Write(data);
        }

        private byte ReadByte(Register register)
        {
            byte[] result = new byte[1];
            _i2cDevice.WriteRead(new byte[] { (byte)register }, result);
            return result[0];
        }

        private void ReadBytes(Register register, byte[] buffer)
        {
            _i2cDevice.WriteRead(new byte[] { (byte)register }, buffer);
        }

        public void Dispose()
        {
            _i2cDevice?.Dispose();
            _i2cDevice = null;
        }

        private enum Register : byte
        {
            WHO_AM_I = 0x0F,
            CTRL1_XL = 0x10,
            CTRL2_G = 0x11,
            CTRL3_C = 0x12,
            OUT_TEMP_L = 0x20,
            OUT_TEMP_H = 0x21,
            OUTX_L_G = 0x22,
            OUTX_H_G = 0x23,
            OUTY_L_G = 0x24,
            OUTY_H_G = 0x25,
            OUTZ_L_G = 0x26,
            OUTZ_H_G = 0x27,
            OUTX_L_XL = 0x28,
            OUTX_H_XL = 0x29,
            OUTY_L_XL = 0x2A,
            OUTY_H_XL = 0x2B,
            OUTZ_L_XL = 0x2C,
            OUTZ_H_XL = 0x2D
        }
    }

    public struct AccelerationData
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public AccelerationData(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct GyroscopeData
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public GyroscopeData(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
