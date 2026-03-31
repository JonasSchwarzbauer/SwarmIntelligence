using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using SwarmIntelligence.Logic.MapControl;
using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication.USB
{
    public static class USBReader
    {
        public static void StartReadUSBTask(CancellationToken ct)
        {
            Task.Run(() => ReceiveVehicleStateTask(ct), ct);
        }

        private static async Task ReceiveVehicleStateTask(CancellationToken ct)
        {
            SerialPort port = USBPort.Instance.Port;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    PacketHeaderUSB? heade = await ReadHeaderSafeAsync(port, ct);
                    if (heade == null)
                    {
                        // A null return now likely means cancellation or stream closure
                        continue;
                    }

                    PacketHeaderUSB header = (PacketHeaderUSB)heade!;

                    byte[] payloadBytes = await ReceiveBytesAsync(port, header.payload_length, ct);

                    IUSBReceiveStruct? rcvStruct = null;

                    switch ((MessageStructIDsUSB)header.message_struct_id)
                    {
                        case MessageStructIDsUSB.SlaveLeadVehicleState:
                            rcvStruct = FromBytes<SlaveToLeadVehicleStateUSBPayload>(payloadBytes);
                            break;
                    }

                    if (rcvStruct != null)
                        USBReceiver.Instance.ProcessReceivedStruct(rcvStruct);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal exit when cancellation token is triggered
            }
            catch (Exception ex)
            {
                Console.WriteLine($"USB Read Error: {ex.Message}");
            }
            finally
            {
                if (port.IsOpen) USBPort.Instance.ClosePort();
            }
        }

        private static async Task<PacketHeaderUSB?> ReadHeaderSafeAsync(SerialPort port, CancellationToken ct)
        {
            const byte HEADER_START = 0xAA;
            int headerSize = Marshal.SizeOf<PacketHeaderUSB>();
            byte[] singleByteBuffer = new byte[1];

            // 1. Hunt for the Header Start byte
            while (true)
            {
                int read = await port.BaseStream.ReadAsync(singleByteBuffer, 0, 1, ct);
                if (read == 0) return null; // Stream closed
                if (singleByteBuffer[0] == HEADER_START) break;
            }

            // 2. Read the rest of the header
            byte[] headerBytes = new byte[headerSize];
            headerBytes[0] = HEADER_START;
            int totalRead = 1;

            while (totalRead < headerSize)
            {
                int read = await port.BaseStream.ReadAsync(headerBytes, totalRead, headerSize - totalRead, ct);
                if (read == 0) return null; // Stream closed mid-header
                totalRead += read;
            }

            return FromBytes<PacketHeaderUSB>(headerBytes);
        }

        private static async Task<byte[]> ReceiveBytesAsync(SerialPort port, int length, CancellationToken ct)
        {
            byte[] buffer = new byte[length];
            int totalRead = 0;

            while (totalRead < length)
            {
                int read = await port.BaseStream.ReadAsync(buffer, totalRead, length - totalRead, ct);
                if (read == 0) throw new EndOfStreamException("Serial port disconnected during payload read.");
                totalRead += read;
            }

            return buffer;
        }

        private static T FromBytes<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
}