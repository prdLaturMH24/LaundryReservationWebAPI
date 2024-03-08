using Microsoft.Extensions.Hosting;
using Serilog;
using System.Configuration;
namespace ReservationWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Log.Logger = new LoggerConfiguration()
            //    .WriteTo.File(@"App_Data/Log-.txt",
            //    outputTemplate: "LaundryReservationApp-{Level}|{Timestamp:yyyy-MM-dd HH:mm:ss}|{Message:j}{NewLine}{Exception}",
            //    rollingInterval: RollingInterval.Day
            //    )
            //    .CreateLogger();

            var builtConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddCommandLine(args)
                .Build();


            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builtConfig).CreateLogger();
            try
            {
                Log.Information("Starting host.");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
               Log.Fatal(ex, "Host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}
