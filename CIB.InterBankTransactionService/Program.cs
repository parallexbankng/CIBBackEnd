using System.Net.Http.Headers;
using CIB.InterBankTransactionService.Entities;
using CIB.InterBankTransactionService.Jobs;
using CIB.InterBankTransactionService.Modules.Common.Interface;
using CIB.InterBankTransactionService.Modules.Common.Repository;
using CIB.InterBankTransactionService.Services;
using CIB.InterBankTransactionService.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;


namespace CIB.InterBankTransactionService;

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
    
    services.AddSingleton<IInterBankJob, InterBankJob>();
    IConfiguration configuration = hostContext.Configuration;
    var con = Encryption.DecryptStrings(configuration.GetConnectionString("parallaxCIBCon"));
    services.AddDbContext<ParallexCIBContext>(options => options.UseSqlServer(con));
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddHttpClient("tokenClient",c => {
      c.BaseAddress = new Uri(configuration.GetValue<string>("TestApiUrl:baseUrl"));
      c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    });
    services.AddScoped<IApiService, ApiService>();
    services.AddHostedService<Worker>();
  })
  .UseSerilog();
}

