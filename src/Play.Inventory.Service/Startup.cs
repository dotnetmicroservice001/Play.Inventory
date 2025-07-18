using System;
using System.Net.Http;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Play.Common.HealthChecks;
using Play.Common.Identity;
using Play.Common.Logging;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Common.OpenTelemetry;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;
using Polly;


namespace Play.Inventory.Service
{
    public class Startup
    {
        private const string AllowedOriginSetting = "AllowedOrigin";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddMongo()
                .AddMongoRepository<InventoryItem>("inventoryitems")
                .AddMongoRepository<CatalogItem>("catalogitems")
                .AddMassTransitWithMessageBroker(Configuration,retryConfigurator =>
                {
                    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                    
                    // we want to say which exception does not need to be retried 
                    // like the item not found exception
                    retryConfigurator.Ignore(typeof(UnknownItemException));
                })
                .AddJwtBearer(); 
            
            AddCatalogClient(services);
            
            services.AddSeqLogging(Configuration)
                .AddTracing(Configuration);
            
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Inventory.Service", Version = "v1" });
            });
            services.AddHealthChecks().AddMongoDb();
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Inventory.Service v1"));
                app.UseCors(builder =>
                {
                    builder.WithOrigins(Configuration[AllowedOriginSetting])
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapPlayEconomyHealthChecks();
            });
        }
        
        private static void AddCatalogClient(IServiceCollection services)
        {
            Random jitterer = new Random();

            // register our catalog client 
            services.AddHttpClient<CatalogClient>(client =>
                    {
                        client.BaseAddress = new Uri("https://localhost:5001");
                    }
                )
                .AddTransientHttpErrorPolicy( builder => builder.WaitAndRetryAsync
                (5, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                    + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
                    onRetry : (outcome, timespan, retryAttempt) =>
                    {
                        var serviceProvider = services.BuildServiceProvider();
                        serviceProvider.GetService<ILogger<CatalogClient>>()?.LogWarning(
                            $"Delaying for {timespan.TotalSeconds} seconds before retrying { retryAttempt }"); 
                    }
                ))
                .AddTransientHttpErrorPolicy( builder => builder.CircuitBreakerAsync(
                    3, 
                    TimeSpan.FromSeconds(15)
                ))
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
        }

    }
}
