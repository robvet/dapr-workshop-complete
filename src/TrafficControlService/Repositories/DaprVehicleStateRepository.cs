using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TrafficControlService.Models;
using Dapr.Client;

namespace TrafficControlService.Repositories
{
    public class DaprVehicleStateRepository : IVehicleStateRepository
    {
       private const string DAPR_STORE_NAME = "statestore";
       private readonly DaprClient _daprClient;
       
       public DaprVehicleStateRepository(DaprClient daprClient)
       {
           _daprClient = daprClient;
       }

        public async Task<VehicleState> GetVehicleStateAsync(string licenseNumber)
        {
            return await _daprClient.GetStateAsync<VehicleState>(
                DAPR_STORE_NAME, licenseNumber);

            // Code for Steps 1 and 2
            // var state = await _daprClient.GetFromJsonAsync<VehicleState>(
            //     $"http://localhost:3600/v1.0/state/{DAPR_STORE_NAME}/{licenseNumber}");
            //     return state;
        }

        public async Task SaveVehicleStateAsync(VehicleState vehicleState)
        {
            await _daprClient.SaveStateAsync(
                DAPR_STORE_NAME, vehicleState.LicenseNumber, vehicleState);
                        
            // Code for Steps 1 and 2
            // var state = new[]
            // {
            //     new { 
            //     key = vehicleState.LicenseNumber,
            //     value = vehicleState
            // }};

            // await _daprClient.PostAsJsonAsync(
            //     $"http://localhost:3600/v1.0/state/{DAPR_STORE_NAME}",
            //     state);
        }
    }
}