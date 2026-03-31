using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwarmIntelligence.Logic.MapControl;
using Webserver.Interfaces;
using Webserver.Models.AgentRelated;
using Webserver.Models.SystemRelated;

namespace Webserver.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheController(ISwarmCache cache) : ControllerBase
    {
        private readonly ISwarmCache _cache = cache;

        [HttpGet("all")]
        public IActionResult GetAll()
        {
            var snapshot = new
            {
                Agents = _cache.Agents.GetAll(),
                Commands = _cache.Commands.GetAll(),
                Registrations = _cache.Registrations.GetAll(),
                AgentErrors = _cache.AgentErrors.GetAll(),
                ManagerState = _cache.ManagerState.Get(),
                MapWorkerState = _cache.MapWorkerState.Get(),
                UsbStatus = _cache.UsbStatus.Get(),
                BufferInformation = _cache.BufferInformation.Get(),
                MapError = _cache.MapError.Get(),
                SwarmMode = _cache.SwarmMode.Get(),
                SwarmSettings = _cache.SwarmSettings.Get()
            };

            return Ok(snapshot);
        }

        [HttpGet("agents")]
        public IActionResult GetAgents() => Ok(_cache.Agents.GetAll());

        [HttpGet("commands")]
        public IActionResult GetCommands() => Ok(_cache.Commands.GetAll());

        [HttpGet("registrations")]
        public IActionResult GetRegistrations() => Ok(_cache.Registrations.GetAll());

        [HttpGet("errors")]
        public IActionResult GetAgentErrors() => Ok(_cache.AgentErrors.GetAll());

        [HttpGet("manager-state")]
        public IActionResult GetManagerState() => Ok(_cache.ManagerState.Get());

        [HttpGet("map-worker-state")]
        public IActionResult GetMapWorkerState() => Ok(_cache.MapWorkerState.Get());

        [HttpGet("usb-status")]
        public IActionResult GetUsbStatus() => Ok(_cache.UsbStatus.Get());

        [HttpGet("buffer-information")]
        public IActionResult GetBufferInformation() => Ok(_cache.BufferInformation.Get());

        [HttpGet("map-error")]
        public IActionResult GetMapError() => Ok(_cache.MapError.Get());

        [HttpGet("obstacles")]
        public IActionResult GetObstacles()
        {
            if (!_cache.ObstacleGrid.IsInitialized)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = "Obstacle grid has not been initialized." });

            return Ok(_cache.ObstacleGrid.GetGrid());
        }

        [HttpGet("obstacles/cells")]
        public IActionResult GetObstacleCells([FromQuery] bool? occupiedOnly)
        {
            if (!_cache.ObstacleGrid.IsInitialized)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = "Obstacle grid has not been initialized." });

            var cells = _cache.ObstacleGrid.GetCells();

            if (occupiedOnly == true)
                cells = cells.Where(c => c.Type != ObstacleType.Free);

            return Ok(cells);
        }

        [HttpGet("swarm-mode")]
        public IActionResult GetSwarmMode() => Ok(_cache.SwarmMode.Get());

        [HttpGet("swarm-settings")]
        public IActionResult GetSwarmSettings()
        {
            var settings = _cache.SwarmSettings.Get();
            return Ok(new
            {
                IsInitialized = settings is not null,
                SwarmSettings = settings
            });
        }

    }
}
