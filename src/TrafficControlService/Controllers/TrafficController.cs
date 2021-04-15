using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TrafficControlService.Events;
using TrafficControlService.DomainServices;
using TrafficControlService.Models;
using System.Net.Http;
using System.Net.Http.Json;
using TrafficControlService.Repositories;
using Dapr.Client;

namespace TrafficControlService.Controllers
{
    [ApiController]
    [Route("")]
    public class TrafficController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IVehicleStateRepository _vehicleStateRepository;
        private readonly ILogger<TrafficController> _logger;
        private readonly ISpeedingViolationCalculator _speedingViolationCalculator;
        private readonly string _roadId;

        public TrafficController(
            ILogger<TrafficController> logger,
            HttpClient httpClient,
            IVehicleStateRepository vehicleStateRepository,
            ISpeedingViolationCalculator speedingViolationCalculator)
        {
            _logger = logger;
            _httpClient = httpClient;
            _vehicleStateRepository = vehicleStateRepository;
            _speedingViolationCalculator = speedingViolationCalculator;
            _roadId = speedingViolationCalculator.GetRoadId();
        }

        [HttpPost("entrycam")]
        public async Task<ActionResult> VehicleEntry(VehicleRegistered msg)
        {
            try
            {
                // log entry
                _logger.LogInformation($"ENTRY detected in lane {msg.Lane} at {msg.Timestamp.ToString("hh:mm:ss")} " +
                    $"of vehicle with license-number {msg.LicenseNumber}.");

                // store vehicle state
                var vehicleState = new VehicleState
                {
                    LicenseNumber = msg.LicenseNumber,
                    EntryTimestamp = msg.Timestamp
                };
                await _vehicleStateRepository.SaveVehicleStateAsync(vehicleState);

                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpPost("exitcam")]
        public async Task<ActionResult> VehicleExit(VehicleRegistered msg, [FromServices] DaprClient daprClient)
        {
            try
            {
                // get vehicle state
                var vehicleState = await _vehicleStateRepository.GetVehicleStateAsync(msg.LicenseNumber);
                if (vehicleState == null)
                {
                    return NotFound();
                }

                // log exit
                _logger.LogInformation($"EXIT detected in lane {msg.Lane} at {msg.Timestamp.ToString("hh:mm:ss")} " +
                    $"of vehicle with license-number {msg.LicenseNumber}.");

                // update state
                vehicleState.ExitTimestamp = msg.Timestamp;
                await _vehicleStateRepository.SaveVehicleStateAsync(vehicleState);

                // handle possible speeding violation
                int violation = _speedingViolationCalculator.DetermineSpeedingViolationInKmh(
                    vehicleState.EntryTimestamp, vehicleState.ExitTimestamp);
                if (violation > 0)
                {
                    _logger.LogInformation($"Speeding violation detected ({violation} KMh) of vehicle" +
                        $"with license-number {vehicleState.LicenseNumber}.");

                    var speedingViolation = new SpeedingViolation
                    {
                        VehicleId = msg.LicenseNumber,
                        RoadId = _roadId,
                        ViolationInKmh = violation,
                        Timestamp = msg.Timestamp
                    };

                    // In last half of Assignment 3, update pub/sub to use Dapr ASP.NET Core client
                    //   argument #1: Name of pub/sub component
                    //   argument #2: Name of topic to which to publish 
                    //   argument #3: The message payload
                    // publish speedingviolation
                    await daprClient.PublishEventAsync("pubsub", "collectfine", speedingViolation);

                    // pub/sub code from first-half of Assignment 3
                    // var message = JsonContent.Create<SpeedingViolation>(speedingViolation);
                    // // Replace the hardcoded API code with a call to the Dapr pub/sub side car
                    // await _httpClient.PostAsync("http://localhost:3600/v1.0/publish/pubsub/collectfine", message);
                    // //await _httpClient.PostAsync("http://localhost:6001/collectfine", message);
                }

                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
