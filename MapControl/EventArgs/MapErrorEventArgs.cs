using System;
using System.IO;
namespace SwarmIntelligence.Logic.MapControl.EventArgs
{
    public class MapErrorEventArgs(Exception exception, string message, string sourceContext = "Map Control") 
        : SwarmErrorEventArgs(exception, message, sourceContext)
    { }
}