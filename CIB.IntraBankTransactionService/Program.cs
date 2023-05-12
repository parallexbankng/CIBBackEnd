using System.Net.Http.Headers;
using CIB.IntraBankTransactionService;
using CIB.IntraBankTransactionService.Entities;
using CIB.IntraBankTransactionService.Jobs;
using CIB.IntraBankTransactionService.Modules.Common.Interface;
using CIB.IntraBankTransactionService.Modules.Common.Repository;
using CIB.IntraBankTransactionService.Services;
using CIB.IntraBankTransactionService.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;


namespace CIB.IntraBankTransactionService
{
  public class Program
  {
    public static void Main(string[] args)
    {

      var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
      //Initialize Logger
      Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();
      try
      {
        Log.Information("Application Starting.");
        CreateHostBuilder(args).Build().Run();
      }
      catch (Exception ex)
      {
        Log.Fatal(ex, "The Application failed to start.");
      }
      finally
      {
        Log.CloseAndFlush();
      }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .ConfigureServices((hostContext, services) => {
          IConfiguration configuration = hostContext.Configuration;
          var con = Encryption.DecryptStrings(configuration.GetConnectionString("parallaxCIBCon"));
          services.AddDbContext<ParallexCIBContext>(options => options.UseSqlServer(con));
          services.AddScoped<IUnitOfWork, UnitOfWork>();
          services.AddHttpClient("tokenClient",c => {
            c.BaseAddress = new Uri(configuration.GetValue<string>("TestApiUrl:baseUrl"));
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
          });
          services.AddSingleton<IIntraBankJob, IntraBankJob>();
          services.AddScoped<IApiService, ApiService>();
          services.AddHostedService<Worker>();
        })
        .UseSerilog();
  }
}
