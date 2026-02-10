using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ReservationWebAPI.Data;
using ReservationWebAPI.Interfaces;
using ReservationWebAPI.Proxies;
using ReservationWebAPI.Repositories;
using Serilog;

namespace ReservationWebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        //This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            var appSettingsSection = Configuration.GetSection("AppSettings");
            //var serilogConfigSection = Configuration.GetSection("Serilog");
            services.Configure<AppSettings>(appSettingsSection);
            //services.Configure<SerilogConfiguration>(serilogConfigSection);
            var appSettings = appSettingsSection.Get<AppSettings>();
            //var serilogConfig = serilogConfigSection.Get<SerilogConfiguration>();

            services.AddDbContext<LaundryDbContext>(option => option.UseSqlServer(appSettings?.LaundryDbConnectionString));


            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IMachineRepository, MachineRepository>();

            services.AddHttpClient();

            services.AddSingleton<IMachineApiProxy, MachineApiProxy>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(appSettings?.BaseAddress!);
                return new MachineApiProxy(httpClient);
            });

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Laundry Reservation API",
                    Version = "v1",
                    Description = "API to add reservation of Laundry Machines",
                    Contact = new OpenApiContact
                    {
                        Name = "Developer Team",
                        Email = string.Empty,
                        Url = new Uri("https://localhost:7077/"),
                    },
                });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            try
            {
                using var scope = app.ApplicationServices.CreateScope();
                var services = scope.ServiceProvider.GetRequiredService<LaundryDbContext>();
                services.Database.Migrate();
                SeedMachineData.Initialize(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while Migration: {ex.Message}");
                throw;
            }

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseRouting();
            loggerFactory.AddSerilog();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
