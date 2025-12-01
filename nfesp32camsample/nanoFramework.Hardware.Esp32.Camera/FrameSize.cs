//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Esp32.Camera
{
    /// <summary>
    /// Camera frame size
    /// </summary>
    public enum FrameSize
    {
        /// <summary>96x96</summary>
        Size96X96 = 0,
        
        /// <summary>QQVGA 160x120</summary>
        QQVGA = 1,
        
        /// <summary>QCIF 176x144</summary>
        QCIF = 2,
        
        /// <summary>HQVGA 240x176</summary>
        HQVGA = 3,
        
        /// <summary>240x240</summary>
        Size240X240 = 4,
        
        /// <summary>QVGA 320x240</summary>
        QVGA = 5,
        
        /// <summary>CIF 400x296</summary>
        CIF = 6,
        
        /// <summary>HVGA 480x320</summary>
        HVGA = 7,
        
        /// <summary>VGA 640x480</summary>
        VGA = 8,
        
        /// <summary>SVGA 800x600</summary>
        SVGA = 9,
        
        /// <summary>XGA 1024x768</summary>
        XGA = 10,
        
        /// <summary>HD 1280x720</summary>
        HD = 11,
        
        /// <summary>SXGA 1280x1024</summary>
        SXGA = 12,
        
        /// <summary>UXGA 1600x1200</summary>
        UXGA = 13
    }
}
