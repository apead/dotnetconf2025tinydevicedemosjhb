using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Threading;

namespace Iot.Device.Ssd1306
{
    /// <summary>
    /// SSD1306 OLED display driver for VGM128064 (128x64)
    /// </summary>
    public class Ssd1306 : IDisposable
    {
        private SpiDevice _spiDevice;
        private I2cDevice _i2cDevice;
        private GpioController _gpio;
        private int _dcPin;
        private int _resetPin;
        private readonly int _width = 128;
        private readonly int _height = 64;
        private byte[] _buffer;
        private bool _isI2c;

        public const byte DefaultI2cAddress = 0x3C;

        public Ssd1306(I2cDevice i2cDevice)
        {
            _i2cDevice = i2cDevice ?? throw new ArgumentNullException(nameof(i2cDevice));
            _isI2c = true;
            _buffer = new byte[_width * _height / 8];
            Initialize();
        }

        public Ssd1306(SpiDevice spiDevice, int dcPin, int resetPin)
        {
            _spiDevice = spiDevice ?? throw new ArgumentNullException(nameof(spiDevice));
            _dcPin = dcPin;
            _resetPin = resetPin;
            _isI2c = false;
            
            _gpio = new GpioController();
            _gpio.OpenPin(_dcPin, PinMode.Output);
            _gpio.OpenPin(_resetPin, PinMode.Output);
            
            _buffer = new byte[_width * _height / 8];
            Initialize();
        }

        private void Initialize()
        {
            if (!_isI2c)
            {
                _gpio.Write(_resetPin, PinValue.High);
                Thread.Sleep(1);
                _gpio.Write(_resetPin, PinValue.Low);
                Thread.Sleep(10);
                _gpio.Write(_resetPin, PinValue.High);
                Thread.Sleep(10);
            }
            else
            {
                Thread.Sleep(10);
            }

            SendCommand(0xAE);
            SendCommand(0xD5);
            SendCommand(0x80);
            SendCommand(0xA8);
            SendCommand(0x3F);
            SendCommand(0xD3);
            SendCommand(0x00);
            SendCommand(0x40);
            SendCommand(0x8D);
            SendCommand(0x14);
            SendCommand(0x20);
            SendCommand(0x00);
            SendCommand(0xA1);
            SendCommand(0xC8);
            SendCommand(0xDA);
            SendCommand(0x12);
            SendCommand(0x81);
            SendCommand(0xCF);
            SendCommand(0xD9);
            SendCommand(0xF1);
            SendCommand(0xDB);
            SendCommand(0x40);
            SendCommand(0xA4);
            SendCommand(0xA6);
            SendCommand(0xAF);
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
        }

        public void SetPixel(int x, int y, bool on = true)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return;

            int index = x + (y / 8) * _width;
            byte bit = (byte)(1 << (y % 8));

            if (on)
                _buffer[index] |= bit;
            else
                _buffer[index] &= (byte)~bit;
        }

        public void DrawChar(int x, int y, char c)
        {
            if (c < 32 || c > 126)
                c = '?';

            byte[] charData = GetCharData(c);
            for (int col = 0; col < 5; col++)
            {
                byte data = charData[col];
                for (int row = 0; row < 8; row++)
                {
                    if ((data & (1 << row)) != 0)
                        SetPixel(x + col, y + row, true);
                }
            }
        }

        public void DrawText(int x, int y, string text)
        {
            int currentX = x;
            foreach (char c in text)
            {
                DrawChar(currentX, y, c);
                currentX += 6;
            }
        }

        public void Display()
        {
            SendCommand(0x21);
            SendCommand(0x00);
            SendCommand(0x7F);
            SendCommand(0x22);
            SendCommand(0x00);
            SendCommand(0x07);
            SendData(_buffer);
        }

        private void SendCommand(byte command)
        {
            if (_isI2c)
            {
                _i2cDevice.Write(new byte[] { 0x00, command });
            }
            else
            {
                _gpio.Write(_dcPin, PinValue.Low);
                _spiDevice.WriteByte(command);
            }
        }

        private void SendData(byte[] data)
        {
            if (_isI2c)
            {
                const int chunkSize = 16;
                for (int i = 0; i < data.Length; i += chunkSize)
                {
                    int length = Math.Min(chunkSize, data.Length - i);
                    byte[] packet = new byte[length + 1];
                    packet[0] = 0x40;
                    Array.Copy(data, i, packet, 1, length);
                    _i2cDevice.Write(packet);
                }
            }
            else
            {
                _gpio.Write(_dcPin, PinValue.High);
                _spiDevice.Write(data);
            }
        }

        private byte[] GetCharData(char c)
        {
            int index = c - 32;
            byte[][] font = new byte[][]
            {
                new byte[] {0x00, 0x00, 0x00, 0x00, 0x00}, new byte[] {0x00, 0x00, 0x5F, 0x00, 0x00},
                new byte[] {0x00, 0x07, 0x00, 0x07, 0x00}, new byte[] {0x14, 0x7F, 0x14, 0x7F, 0x14},
                new byte[] {0x24, 0x2A, 0x7F, 0x2A, 0x12}, new byte[] {0x23, 0x13, 0x08, 0x64, 0x62},
                new byte[] {0x36, 0x49, 0x55, 0x22, 0x50}, new byte[] {0x00, 0x05, 0x03, 0x00, 0x00},
                new byte[] {0x00, 0x1C, 0x22, 0x41, 0x00}, new byte[] {0x00, 0x41, 0x22, 0x1C, 0x00},
                new byte[] {0x14, 0x08, 0x3E, 0x08, 0x14}, new byte[] {0x08, 0x08, 0x3E, 0x08, 0x08},
                new byte[] {0x00, 0x50, 0x30, 0x00, 0x00}, new byte[] {0x08, 0x08, 0x08, 0x08, 0x08},
                new byte[] {0x00, 0x60, 0x60, 0x00, 0x00}, new byte[] {0x20, 0x10, 0x08, 0x04, 0x02},
                new byte[] {0x3E, 0x51, 0x49, 0x45, 0x3E}, new byte[] {0x00, 0x42, 0x7F, 0x40, 0x00},
                new byte[] {0x42, 0x61, 0x51, 0x49, 0x46}, new byte[] {0x21, 0x41, 0x45, 0x4B, 0x31},
                new byte[] {0x18, 0x14, 0x12, 0x7F, 0x10}, new byte[] {0x27, 0x45, 0x45, 0x45, 0x39},
                new byte[] {0x3C, 0x4A, 0x49, 0x49, 0x30}, new byte[] {0x01, 0x71, 0x09, 0x05, 0x03},
                new byte[] {0x36, 0x49, 0x49, 0x49, 0x36}, new byte[] {0x06, 0x49, 0x49, 0x29, 0x1E},
                new byte[] {0x00, 0x36, 0x36, 0x00, 0x00}, new byte[] {0x00, 0x56, 0x36, 0x00, 0x00},
                new byte[] {0x08, 0x14, 0x22, 0x41, 0x00}, new byte[] {0x14, 0x14, 0x14, 0x14, 0x14},
                new byte[] {0x00, 0x41, 0x22, 0x14, 0x08}, new byte[] {0x02, 0x01, 0x51, 0x09, 0x06},
                new byte[] {0x32, 0x49, 0x79, 0x41, 0x3E}, new byte[] {0x7E, 0x11, 0x11, 0x11, 0x7E},
                new byte[] {0x7F, 0x49, 0x49, 0x49, 0x36}, new byte[] {0x3E, 0x41, 0x41, 0x41, 0x22},
                new byte[] {0x7F, 0x41, 0x41, 0x22, 0x1C}, new byte[] {0x7F, 0x49, 0x49, 0x49, 0x41},
                new byte[] {0x7F, 0x09, 0x09, 0x09, 0x01}, new byte[] {0x3E, 0x41, 0x49, 0x49, 0x7A},
                new byte[] {0x7F, 0x08, 0x08, 0x08, 0x7F}, new byte[] {0x00, 0x41, 0x7F, 0x41, 0x00},
                new byte[] {0x20, 0x40, 0x41, 0x3F, 0x01}, new byte[] {0x7F, 0x08, 0x14, 0x22, 0x41},
                new byte[] {0x7F, 0x40, 0x40, 0x40, 0x40}, new byte[] {0x7F, 0x02, 0x0C, 0x02, 0x7F},
                new byte[] {0x7F, 0x04, 0x08, 0x10, 0x7F}, new byte[] {0x3E, 0x41, 0x41, 0x41, 0x3E},
                new byte[] {0x7F, 0x09, 0x09, 0x09, 0x06}, new byte[] {0x3E, 0x41, 0x51, 0x21, 0x5E},
                new byte[] {0x7F, 0x09, 0x19, 0x29, 0x46}, new byte[] {0x46, 0x49, 0x49, 0x49, 0x31},
                new byte[] {0x01, 0x01, 0x7F, 0x01, 0x01}, new byte[] {0x3F, 0x40, 0x40, 0x40, 0x3F},
                new byte[] {0x1F, 0x20, 0x40, 0x20, 0x1F}, new byte[] {0x3F, 0x40, 0x38, 0x40, 0x3F},
                new byte[] {0x63, 0x14, 0x08, 0x14, 0x63}, new byte[] {0x07, 0x08, 0x70, 0x08, 0x07},
                new byte[] {0x61, 0x51, 0x49, 0x45, 0x43}, new byte[] {0x00, 0x7F, 0x41, 0x41, 0x00},
                new byte[] {0x02, 0x04, 0x08, 0x10, 0x20}, new byte[] {0x00, 0x41, 0x41, 0x7F, 0x00},
                new byte[] {0x04, 0x02, 0x01, 0x02, 0x04}, new byte[] {0x40, 0x40, 0x40, 0x40, 0x40},
                new byte[] {0x00, 0x01, 0x02, 0x04, 0x00}, new byte[] {0x20, 0x54, 0x54, 0x54, 0x78},
                new byte[] {0x7F, 0x48, 0x44, 0x44, 0x38}, new byte[] {0x38, 0x44, 0x44, 0x44, 0x20},
                new byte[] {0x38, 0x44, 0x44, 0x48, 0x7F}, new byte[] {0x38, 0x54, 0x54, 0x54, 0x18},
                new byte[] {0x08, 0x7E, 0x09, 0x01, 0x02}, new byte[] {0x0C, 0x52, 0x52, 0x52, 0x3E},
                new byte[] {0x7F, 0x08, 0x04, 0x04, 0x78}, new byte[] {0x00, 0x44, 0x7D, 0x40, 0x00},
                new byte[] {0x20, 0x40, 0x44, 0x3D, 0x00}, new byte[] {0x7F, 0x10, 0x28, 0x44, 0x00},
                new byte[] {0x00, 0x41, 0x7F, 0x40, 0x00}, new byte[] {0x7C, 0x04, 0x18, 0x04, 0x78},
                new byte[] {0x7C, 0x08, 0x04, 0x04, 0x78}, new byte[] {0x38, 0x44, 0x44, 0x44, 0x38},
                new byte[] {0x7C, 0x14, 0x14, 0x14, 0x08}, new byte[] {0x08, 0x14, 0x14, 0x18, 0x7C},
                new byte[] {0x7C, 0x08, 0x04, 0x04, 0x08}, new byte[] {0x48, 0x54, 0x54, 0x54, 0x20},
                new byte[] {0x04, 0x3F, 0x44, 0x40, 0x20}, new byte[] {0x3C, 0x40, 0x40, 0x20, 0x7C},
                new byte[] {0x1C, 0x20, 0x40, 0x20, 0x1C}, new byte[] {0x3C, 0x40, 0x30, 0x40, 0x3C},
                new byte[] {0x44, 0x28, 0x10, 0x28, 0x44}, new byte[] {0x0C, 0x50, 0x50, 0x50, 0x3C},
                new byte[] {0x44, 0x64, 0x54, 0x4C, 0x44}, new byte[] {0x00, 0x08, 0x36, 0x41, 0x00},
                new byte[] {0x00, 0x00, 0x7F, 0x00, 0x00}, new byte[] {0x00, 0x41, 0x36, 0x08, 0x00},
                new byte[] {0x08, 0x04, 0x08, 0x10, 0x08}
            };

            if (index >= 0 && index < font.Length)
                return font[index];
            
            return new byte[] {0x00, 0x00, 0x00, 0x00, 0x00};
        }

        public void Dispose()
        {
            _gpio?.ClosePin(_dcPin);
            _gpio?.ClosePin(_resetPin);
            _gpio?.Dispose();
            _spiDevice?.Dispose();
            _i2cDevice?.Dispose();
        }
    }
}
