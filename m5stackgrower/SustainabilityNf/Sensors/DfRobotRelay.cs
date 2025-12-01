using System;
using System.Device.Gpio;

namespace SustainabilityNf.Sensors
{
    public class DfRobotRelay
    {
        private GpioPin _pin;
        private PinValue _state;
        private GpioController _controller;

        public DfRobotRelay(GpioController controller, int pin)
        {
            _controller = controller;

            _pin = controller.OpenPin(pin);
            controller.SetPinMode(pin, PinMode.Output);

            SetRelay(0);
        }
    
        public void SetRelay(PinValue state)
        {
            _state = state;
            _pin.Write(state);
    }
        
        public PinValue GetRelay()
        {
            return _state;
        }
        
        public void ToggleRelay()
        {
            if (GetRelay() == PinValue.Low)
                SetRelay(PinValue.High);
            else
                SetRelay(PinValue.Low);
        }
    }
}
