using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyse.Server.ChangeFeeds;
using Nyse.Server.Repositories;

namespace Nyse.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSignalR()
                .AddNewtonsoftJsonProtocol(s => s.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects);

            services.AddSingleton<IStockPriceRepository, SampleStockPriceRepository>();
            services.AddSingleton<IStockPriceChangeFeed, SampleStockPriceChangeFeed>();
        }
            

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<StocksHub>("stocks");
            });
        }
    }
}
