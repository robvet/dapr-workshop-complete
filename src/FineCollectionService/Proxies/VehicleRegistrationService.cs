using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FineCollectionService.Models;

namespace FineCollectionService.Proxies
{
    public class VehicleRegistrationService
    {
        private HttpClient _httpClient;

        public VehicleRegistrationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<VehicleInfo> GetVehicleInfo(string licenseNumber)
        {
            // Using the Dapr.AspNetCore library (step 3 in assignment 2)
            return await _httpClient.GetFromJsonAsync<VehicleInfo>($"/vehicleinfo/{licenseNumber}");

            // Call when using the Dapr HTTP API to invoke Dapr (step 2 in assignment 2)
            // return await _httpClient.GetFromJsonAsync<VehicleInfo>(
            //     $"http://localhost:3601/v1.0/invoke/vehicleregistrationservice/method/vehicleinfo/{licenseNumber}");
            
            // This is earlier non-Dapr service invocation call to the Vehicle Information Service
            // return await _httpClient.GetFromJsonAsync<VehicleInfo>(
            //    $"http://localhost:6002/vehicleinfo/{licenseNumber}");
        }       
    }
}