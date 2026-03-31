using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Webserver.Interfaces;
using Webserver.Models.SystemRelated;

namespace Webserver.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShortcutsController : ControllerBase
    {
        private readonly ISwarmCache _cache;

        public ShortcutsController(ISwarmCache cache)
        {
            _cache = cache;
        }

        [HttpGet("map-worker")]
        public IActionResult GetMapWorker()
        {
            var state = _cache.MapWorkerState.Get();
            if (state is null)
            {
                return NotFound();
            }

            return Ok(state);
        }

        [HttpGet("drive-control")]
        public IActionResult GetDriveControl()
        {
            var state = _cache.ManagerState.Get();
            if (state is null)
            {
                return NotFound();
            }

            return Ok(state);
        }

        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            var mapWorker = _cache.MapWorkerState.Get();
            var manager = _cache.ManagerState.Get();
            var usb = _cache.UsbStatus.Get();
            var buffer = _cache.BufferInformation.Get();

            var response = new
            {
                DriveControlRunning = manager?.IsRunning ?? false,
                DriveControlLastUpdated = manager?.LastUpdated,
                MapWorkerState = mapWorker?.CurrentState,
                MapWorkerLastUpdated = mapWorker?.LastUpdated,
                Usb = usb is null ? null : new { usb.Success, usb.PortName, usb.BaudRate, usb.Message, usb.Timestamp },
                Buffer = buffer is null ? null : new { buffer.Success, buffer.BufferCount, buffer.BufferCapacity, buffer.BufferUsagePercentage, buffer.Timestamp }
            };

            return Ok(response);
        }

        [HttpGet("summary-file")]
        public IActionResult GetSummaryFile()
        {
            var mapWorker = _cache.MapWorkerState.Get();
            var manager = _cache.ManagerState.Get();
            var usb = _cache.UsbStatus.Get();
            var buffer = _cache.BufferInformation.Get();

            var response = new
            {
                DriveControlRunning = manager?.IsRunning ?? false,
                DriveControlLastUpdated = manager?.LastUpdated,
                MapWorkerState = mapWorker?.CurrentState,
                MapWorkerLastUpdated = mapWorker?.LastUpdated,
                Usb = usb is null ? null : new { usb.Success, usb.PortName, usb.BaudRate, usb.Message, usb.Timestamp },
                Buffer = buffer is null ? null : new { buffer.Success, buffer.BufferCount, buffer.BufferCapacity, buffer.BufferUsagePercentage, buffer.Timestamp }
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", "cache-summary.json");
        }
    }
}
