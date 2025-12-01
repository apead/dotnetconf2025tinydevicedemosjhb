//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;

namespace nanoFramework.Hardware.Esp32.Camera
{
    /// <summary>
    /// Camera configuration structure
    /// </summary>
    public class CameraConfig
    {
        /// <summary>Power down pin</summary>
        public int pinPwdn;
        
        /// <summary>Reset pin</summary>
        public int pinReset;
        
        /// <summary>XCLK pin</summary>
        public int pinXclk;
        
        /// <summary>SIOD (I2C SDA) pin</summary>
        public int pinSiod;
        
        /// <summary>SIOC (I2C SCL) pin</summary>
        public int pinSioc;
        
        /// <summary>Data pin 7</summary>
        public int pinD7;
        
        /// <summary>Data pin 6</summary>
        public int pinD6;
        
        /// <summary>Data pin 5</summary>
        public int pinD5;
        
        /// <summary>Data pin 4</summary>
        public int pinD4;
        
        /// <summary>Data pin 3</summary>
        public int pinD3;
        
        /// <summary>Data pin 2</summary>
        public int pinD2;
        
        /// <summary>Data pin 1</summary>
        public int pinD1;
        
        /// <summary>Data pin 0</summary>
        public int pinD0;
        
        /// <summary>VSYNC pin</summary>
        public int pinVsync;
        
        /// <summary>HREF pin</summary>
        public int pinHref;
        
        /// <summary>PCLK pin</summary>
        public int pinPclk;
        
        /// <summary>XCLK frequency in Hz</summary>
        public int xclkFreqHz;
        
        /// <summary>Pixel format</summary>
        public PixelFormat pixelFormat;
        
        /// <summary>Frame size</summary>
        public FrameSize frameSize;
        
        /// <summary>JPEG quality (10-63, lower means higher quality)</summary>
        public int jpegQuality;
        
        /// <summary>Number of frame buffers</summary>
        public int fbCount;

        /// <summary>
        /// Creates a default configuration for ESP32-CAM
        /// </summary>
        public static CameraConfig CreateDefault()
        {
            return new CameraConfig
            {
                pinPwdn = 32,
                pinReset = -1,
                pinXclk = 0,
                pinSiod = 26,
                pinSioc = 27,
                pinD7 = 35,
                pinD6 = 34,
                pinD5 = 39,
                pinD4 = 36,
                pinD3 = 21,
                pinD2 = 19,
                pinD1 = 18,
                pinD0 = 5,
                pinVsync = 25,
                pinHref = 23,
                pinPclk = 22,
                xclkFreqHz = 20000000,
                pixelFormat = PixelFormat.JPEG,
                frameSize = FrameSize.SVGA,
                jpegQuality = 12,
                fbCount = 1
            };
        }
    }
}
