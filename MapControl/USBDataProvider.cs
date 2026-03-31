using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.MapControl.EventArgs;
using SwarmIntelligence.Logic.MapControl.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SwarmIntelligence.Logic.MapControl
{
    public class USBDataProvider : IDataProvider
    {
        public event EventHandler<UsbStartedEventArgs>? StartedUSB;
        public event EventHandler<BufferInformationEventArgs>? BufferInformationEvent;

        public void StartProviding(CancellationToken ct)
        {
            UsbStartedEventArgs args;

            try
            {
                // Start task that uses the USBReceiver to process incoming data
                USBReader.StartReadUSBTask(ct);

                var port = USBPort.Instance.Port;
                args = new UsbStartedEventArgs(true, port.PortName, port.BaudRate, "Task to read USB data was started successfully.");
            }
            catch (Exception ex)
            {
                var port = USBPort.Instance?.Port;
                args = new UsbStartedEventArgs(false, port?.PortName ?? string.Empty, port?.BaudRate ?? 0, ex.Message);
            }

            StartedUSB?.Invoke(this, args);
        }

        public USBDataProvider()
        {
            // Subscribe to USBReceiver buffer information event (fired when data is processed)
            USBReceiver.BufferInformationEvent += OnBufferInformationEvent;
        }

        private void OnBufferInformationEvent(object? sender, BufferInformationEventArgs e)
        {
            BufferInformationEvent?.Invoke(this, e);
        }
    }
}
