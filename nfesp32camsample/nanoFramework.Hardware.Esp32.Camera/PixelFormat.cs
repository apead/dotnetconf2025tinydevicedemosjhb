//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32.Camera
{
    /// <summary>
    /// Camera pixel format
    /// </summary>
    public enum PixelFormat
    {
        /// <summary>RGB565</summary>
        RGB565 = 0,
        
        /// <summary>YUV422</summary>
        YUV422 = 1,
        
        /// <summary>YUV420</summary>
        YUV420 = 2,
        
        /// <summary>Grayscale</summary>
        GRAYSCALE = 3,
        
        /// <summary>JPEG</summary>
        JPEG = 4,
        
        /// <summary>RGB888</summary>
        RGB888 = 5,
        
        /// <summary>RAW</summary>
        RAW = 6,
        
        /// <summary>RGB444</summary>
        RGB444 = 7,
        
        /// <summary>RGB555</summary>
        RGB555 = 8
    }
}
