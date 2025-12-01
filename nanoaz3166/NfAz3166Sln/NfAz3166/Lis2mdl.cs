using System;
using System.Device.I2c;

namespace Iot.Device.Lis2mdl
{
    /// <summary>
    /// LIS2MDL - 3D digital magnetometer
    /// </summary>
    public class Lis2mdl : IDisposable
    {
        private I2cDevice _i2cDevice;
        
        /// <summary>
        /// Default I2C address
        /// </summary>
        public const byte DefaultI2cAddress = 0x1E;
        
        /// <summary>
        /// Device ID from WHO_AM_I register
        /// </summary>
        public const byte DeviceId = 0x40;

        /// <summary>
        /// Initializes a new instance of the LIS2MDL class
        /// </summary>
        /// <param name="i2cDevice">I2C device</param>
        public Lis2mdl(I2cDevice i2cDevice)
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
            // CFG_REG_A: Continuous mode, ODR = 10 Hz, temperature compensation enabled
            WriteByte(Register.CFG_REG_A, 0x8C); // 1000 1100
            
            // CFG_REG_B: Off-set cancellation in single mode disabled
            WriteByte(Register.CFG_REG_B, 0x00);
            
            // CFG_REG_C: BDU enabled, I2C enabled
            WriteByte(Register.CFG_REG_C, 0x10); // 0001 0000
        }

        /// <summary>
        /// Read magnetic field in microTesla (µT)
        /// </summary>
        public MagneticFieldData GetMagneticField()
        {
            byte[] data = new byte[6];
            ReadBytes(Register.OUTX_L_REG, data);
            
            // Convert to signed 16-bit values
            short x = (short)(data[0] | (data[1] << 8));
            short y = (short)(data[2] | (data[3] << 8));
            short z = (short)(data[4] | (data[5] << 8));
            
            // Convert to microTesla (sensitivity is 1.5 mGauss/LSB = 0.15 µT/LSB)
            double sensitivity = 0.15; // µT per LSB
            
            return new MagneticFieldData(
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
            ReadBytes(Register.TEMP_OUT_L_REG, data);
            
            short temp = (short)(data[0] | (data[1] << 8));
            
            // Temperature in °C = (Output / 8) + 25
            return (temp / 8.0) + 25.0;
        }

        /// <summary>
        /// Check if new data is available
        /// </summary>
        public bool IsDataReady()
        {
            byte status = ReadByte(Register.STATUS_REG);
            return (status & 0x08) != 0; // ZYXDA bit
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
            WHO_AM_I = 0x4F,
            CFG_REG_A = 0x60,
            CFG_REG_B = 0x61,
            CFG_REG_C = 0x62,
            STATUS_REG = 0x67,
            OUTX_L_REG = 0x68,
            OUTX_H_REG = 0x69,
            OUTY_L_REG = 0x6A,
            OUTY_H_REG = 0x6B,
            OUTZ_L_REG = 0x6C,
            OUTZ_H_REG = 0x6D,
            TEMP_OUT_L_REG = 0x6E,
            TEMP_OUT_H_REG = 0x6F
        }
    }

    public struct MagneticFieldData
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public MagneticFieldData(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Calculate magnetic field magnitude
        /// </summary>
        public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);
    }
}
