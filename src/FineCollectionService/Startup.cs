using FineCollectionService.DomainServices;
using FineCollectionService.Proxies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dapr.Client;

namespace FineCollectionService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IFineCalculator, HardCodedFineCalculator>();

            // Updated snippet to use Dapr.AspNetCore SDK.
            services.AddSingleton<VehicleRegistrationService>(_ => 
                new VehicleRegistrationService(DaprClient.CreateInvokeHttpClient(
                    "vehicleregistrationservice", "http://localhost:3601")));
            
            
            // Replaced the following code to use the Dapr.AspNetCore SDK
            // // add service proxies
            // services.AddHttpClient();
            // services.AddSingleton<VehicleRegistrationService>();

            // Add .AddDapr() extension to .AddControllers. 
            // Makes controller Dapr aware and adds Dapr integration.
            services.AddControllers().AddDapr();

            // Register DaprClient with DI container
            services.AddDaprClient(builder => builder
                .UseHttpEndpoint($"http://localhost:3601")
                .UseGrpcEndpoint($"http://localhost:60001")
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // Add so that CloudEvent format is handled for messages.
            app.UseCloudEvents();

            app.UseEndpoints(endpoints =>
            {
                // MapSubscriber registers controllers that subscribe to pub/sub.
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();
            });
        }
    }
}
