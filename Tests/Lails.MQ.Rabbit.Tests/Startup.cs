using Lails.MQ.Rabbit.Tests.Consumers;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;

namespace Lails.MQ.Rabbit.Tests
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDbContextPool<LailsMQTestDbContext>(opt => opt.UseNpgsql(Configuration.GetConnectionString("LailsMQTestDbContext")), 10)
                .AddTransient<DbContext, LailsMQTestDbContext>();

            services
                .AddMvc();

            services.AddMvcCore(r => { r.EnableEndpointRouting = false; });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lails.MQ.Rabbit.Tests ", Version = "v1" });
            });

            services
                .RegisterRabbitPublisher()
                .AddMassTransit(x =>
            {
                x.AddConsumer<AddPointConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.AddDataBusConfiguration(Configuration);

                    cfg
                        .RegisterConsumerWithRetry<AddPointConsumer, IAddPointsEvent>(context, 1, 1,10);
                });
            });
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env, IBusControl busControl, IServiceProvider provider)
        {
            var dbContext = provider.GetService<LailsMQTestDbContext>();
            dbContext.Database.Migrate();


            busControl.Start();


            var swaggerBasePath = string.Empty;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                swaggerBasePath = "/messageQueue";
                app.UseHsts();
            }

            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"https://{httpReq.Host.Value}{swaggerBasePath}" } };
                });
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{swaggerBasePath}/swagger/v1/swagger.json", "Lails.MQ.Rabbit.Tests");
            });

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
