@echo off
REM Flash ESP32-CAM nanoFramework Firmware
REM Usage: flash.bat COM5

if "%1"=="" (
    echo Usage: flash.bat [COM_PORT]
    echo Example: flash.bat COM5
    exit /b 1
)

set COMPORT=%1

echo ====================================
echo Flashing ESP32-CAM Firmware
echo ====================================
echo COM Port: %COMPORT%
echo.
echo Make sure IO0 is connected to GND!
echo Press any key to continue...
pause >nul

echo.
echo [1/3] Flashing bootloader...
echo Connect IO0 to GND and press RESET button to enter bootloader mode
echo Press any key when ready...
pause >nul
nanoff --target ESP32_CAM_PSRAM --serialport %COMPORT% --deploy --image bootloader.bin --address 0x1000
if errorlevel 1 (
    echo ERROR: Failed to flash bootloader
    pause
    exit /b 1
)

echo.
echo [2/3] Flashing partition table...
echo Press RESET button to enter bootloader mode again (keep IO0 connected to GND)
echo Press any key when ready...
pause >nul
nanoff --target ESP32_CAM_PSRAM --serialport %COMPORT% --deploy --image partition-table.bin --address 0x8000
if errorlevel 1 (
    echo ERROR: Failed to flash partition table
    pause
    exit /b 1
)

echo.
echo [3/3] Flashing main firmware...
echo Press RESET button to enter bootloader mode again (keep IO0 connected to GND)
echo Press any key when ready...
pause >nul
nanoff --target ESP32_CAM_PSRAM --serialport %COMPORT% --deploy --image nanoCLR.bin --address 0x10000
if errorlevel 1 (
    echo ERROR: Failed to flash main firmware
    pause
    exit /b 1
)

echo.
echo ====================================
echo Firmware flashed successfully!
echo ====================================
echo.
echo Next steps:
echo 1. Disconnect IO0 from GND
echo 2. Press RESET button on ESP32-CAM
echo 3. Open Visual Studio Device Explorer to verify
echo.
pause
