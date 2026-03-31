using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.Communication.USB;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    // Singleton to be able to limit writing to USB to one at a time
    public class USBWriter
    {
        private static readonly Lazy<USBWriter> _instance = new Lazy<USBWriter>(() => new USBWriter());
        public static USBWriter Instance => _instance.Value;
        private USBWriter()
        {
            _usbWriteLock = new();
        }

        // Only one struct at a time is allowed to be tranfered => Synchronize access
        private readonly object _usbWriteLock;
        public void SendStruct<T>(T payload, byte message_id) where T : struct, IUSBSendStruct
        {
            // Convert payload to bytes
            byte[] payloadBytes = StructToBytes(payload);

            // Create header
            PacketHeaderUSB header = new PacketHeaderUSB
            {
                start = 0xAA,
                message_struct_id = message_id,
                payload_length = (byte)payloadBytes.Length
            };

            // Combine Header and Payload and send at the same time
            byte[] headerBytes = StructToBytes(header);

            byte[] final = new byte[headerBytes.Length + payloadBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, final, 0, headerBytes.Length);
            Buffer.BlockCopy(payloadBytes, 0, final, headerBytes.Length, payloadBytes.Length);

            SerialPort port = USBPort.Instance.Port;

            // Synchronize Access to prevent multiple instances writing at the same time
            lock (_usbWriteLock)
            {
                if(port.IsOpen) port.Write(final, 0, final.Length);
            }
        }
        private static byte[] StructToBytes<T>(T str) where T : struct
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }
    }
}
