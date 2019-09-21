using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyse.Server.ChangeFeeds;
using Nyse.Server.Repositories;
using Qx.SignalR;
using Remote.Linq;
using System.Linq;

namespace Nyse.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSignalR()
                // Allow anything in our Nyse.Schema library
                .AddQx(o => o.WithAllowedMembers(from types in typeof(Schema.SharePrice).Assembly.GetTypes()
                                                 from members in types.GetMembers()
                                                 select members))
                .AddNewtonsoftJsonProtocol(s => s.PayloadSerializerSettings.ConfigureRemoteLinq());

            services.AddSingleton<ISharesRepository, SampleSharesRepository>();
            services.AddSingleton<ISharesChangeFeed, SampleSharesChangeFeed>();
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<StocksHub>("shares");
                endpoints.MapHub<QueryableStocksHub>("queryable-shares");
            });
        }
    }
}
