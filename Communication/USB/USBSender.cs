using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    public abstract class USBSender<T>(IStructConverter conv) : IUSBSender where T : struct, IUSBSendable 
    {
        private IStructConverter structConverter = conv;
        public void SendStructViaUSB(T structToSend) 
        {
            var converted = structConverter.ConvertStruct(structToSend);

            // Runtime type needs to be provided to the method via generics => Use Reflections
            Type runtimeTypeStruct = converted.ConvStruct.GetType();
            var methodInfo = typeof(USBWriter)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m =>
                    m.Name == nameof(USBWriter.SendStruct) &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == 1
                );
            var genericMethod = methodInfo!.MakeGenericMethod(runtimeTypeStruct);
            genericMethod.Invoke(USBWriter.Instance, [converted.ConvStruct, converted.MessageId]);
        }
    }
}
