using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using FineCollectionService.DomainServices;
using FineCollectionService.Helpers;
using FineCollectionService.Models;
using FineCollectionService.Proxies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FineCollectionService.Controllers
{
    [ApiController]
    [Route("")]
    public class CollectionController : ControllerBase
    {
        private static string _fineCalculatorLicenseKey;
        private readonly ILogger<CollectionController> _logger;
        private readonly IFineCalculator _fineCalculator;
        private readonly VehicleRegistrationService _vehicleRegistrationService;

        public CollectionController(ILogger<CollectionController> logger,
            IFineCalculator fineCalculator, 
            VehicleRegistrationService vehicleRegistrationService,
            DaprClient daprClient)
        {
            _logger = logger;
            _fineCalculator = fineCalculator;
            _vehicleRegistrationService = vehicleRegistrationService;

            // set finecalculator component license-key
            if (_fineCalculatorLicenseKey == null)
            {
                var secrets = daprClient.GetSecretAsync(
                    "trafficcontrol-secrets", "finecalculator.licensekey").Result;
                    _fineCalculatorLicenseKey = secrets["finecalculator.licensekey"];
                // _fineCalculatorLicenseKey = "HX783-K2L7V-CRJ4A-5PN1G";
            }
        }

        // Add Subscribe method in first of half of Assignment 4, but remove it when implementing the ASP.NET Core Dapr client
        // [Route("/dapr/subscribe")]
        // [HttpGet()]
        // // Exposes subscriptions for this service. Other services can consume this endpoint to determine subscriptions.
        // public object Subscribe()
        // {
        //     return new object[]
        //     {
        //         new
        //         {
        //             pubsubname = "pubsub",
        //             topic = "collectfine",
        //             route = "/collectfine"
        //         }
        //     };
        // }

        // Add Topic attribute for ASP.NET Core Dapr client
        [Topic("pubsub", "collectfine")]
        [Route("collectfine")]
        [HttpPost()]
        public async Task<ActionResult> CollectFine(SpeedingViolation speedingViolation, [FromServices]DaprClient daprClient)
        // Replace the SpeedingViolation with a JsonDocument parameter. Doing so will enable Dapr pub/sub messaging.
        //public async Task<ActionResult> CollectFine([FromBody] System.Text.Json.JsonDocument cloudevent)
        {
            // Remove CloudEvent parsing code when using ASP.NET Core Dapr client
            // // Extract SpeedingViolation data from the cloudevent parameter and assign to SpeedingViolation type.
            // var data = cloudevent.RootElement.GetProperty("data");
            
            // // Transform raw data into a SpeedingViolation object
            // var speedingViolation = new SpeedingViolation
            // {
            //     VehicleId = data.GetProperty("vehicleId").GetString(),
            //     RoadId = data.GetProperty("roadId").GetString(),
            //     Timestamp = data.GetProperty("timestamp").GetDateTime(),
            //     ViolationInKmh = data.GetProperty("violationInKmh").GetInt32()
            // };
            
            decimal fine = _fineCalculator.CalculateFine(_fineCalculatorLicenseKey, speedingViolation.ViolationInKmh);

            // get owner info
            var vehicleInfo = await _vehicleRegistrationService.GetVehicleInfo(speedingViolation.VehicleId);

            // log fine
            string fineString = fine == 0 ? "tbd by the prosecutor" : $"{fine} Euro";
            _logger.LogInformation($"Sent speeding ticket to {vehicleInfo.OwnerName}. " +
                $"Road: {speedingViolation.RoadId}, Licensenumber: {speedingViolation.VehicleId}, " +
                $"Vehicle: {vehicleInfo.Brand} {vehicleInfo.Model}, " +
                $"Violation: {speedingViolation.ViolationInKmh} Km/h, Fine: {fineString}, " +
                $"On: {speedingViolation.Timestamp.ToString("dd-MM-yyyy")} " +
                $"at {speedingViolation.Timestamp.ToString("hh:mm:ss")}.");

            // send fine by email
            // Create email body with custom EmailUtility class
            var body = EmailUtils.CreateEmailBody(speedingViolation, vehicleInfo, fineString);

            // Specify email metadata
            var metadata = new Dictionary<string, string>
            {
                ["emailFrom"] = "noreply@cfca.gov",
                ["emailTo"] = vehicleInfo.OwnerEmail,
                ["subject"] = $"Speeding violation on the {speedingViolation.RoadId}"
            };

            // Call email server with Dapr output binding
            await daprClient.InvokeBindingAsync("sendmail", "create", body, metadata);

            return Ok();
        }
    }
}
