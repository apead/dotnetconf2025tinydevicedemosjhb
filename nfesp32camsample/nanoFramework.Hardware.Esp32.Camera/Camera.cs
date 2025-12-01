//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

namespace nanoFramework.Hardware.Esp32.Camera
{
    /// <summary>
    /// ESP32 Camera driver
    /// </summary>
    public class Camera : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// Initializes the camera with the specified configuration
        /// </summary>
        /// <param name="config">Camera configuration</param>
        /// <returns>True if initialization successful, false otherwise</returns>
        public bool Initialize(CameraConfig config)
        {
            return NativeInitialize(config);
        }

        /// <summary>
        /// Captures an image from the camera
        /// </summary>
        /// <returns>Byte array containing the image data, or null if capture failed</returns>
        public byte[] CaptureImage()
        {
            return NativeCaptureImage();
        }

        /// <summary>
        /// Deinitializes the camera
        /// </summary>
        public void Deinitialize()
        {
            if (!_disposed)
            {
                NativeDeinitialize();
            }
        }

        /// <summary>
        /// Disposes of the camera resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Deinitialize();
                _disposed = true;
            }
        }

        #region Native Calls

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool NativeInitialize(CameraConfig config);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern byte[] NativeCaptureImage();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeDeinitialize();

        #endregion
    }
}
