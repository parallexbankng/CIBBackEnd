using System.Net.Http.Headers;
using CIB.InterBankTransactionService.Entities;
using CIB.InterBankTransactionService.Jobs;
using CIB.InterBankTransactionService.Modules.Common.Interface;
using CIB.InterBankTransactionService.Modules.Common.Repository;
using CIB.InterBankTransactionService.Services;
using CIB.InterBankTransactionService.Utils;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using NLog.Web;

namespace CIB.InterBankTransactionService;

public class Program
{
    [Obsolete]
    public static void Main(string[] args)
  {

    var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
    try
    {
      logger.Debug("init main");
      CreateHostBuilder(args).Build().Run();
    }
    catch (Exception ex)
    {
      //NLog: catch setup errors
      logger.Error(ex, "Stopped program because of exception");
      throw;
    }
    finally
    {
      // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
      NLog.LogManager.Shutdown();
    }
  }
  public static IHostBuilder CreateHostBuilder(string[] args) =>
  Host.CreateDefaultBuilder(args)
  .UseWindowsService()
  .ConfigureServices((hostContext, services) => {
    
    services.AddSingleton<IInterBankJob, InterBankJob>();
    IConfiguration configuration = hostContext.Configuration;
    var con = Encryption.DecryptStrings(configuration.GetConnectionString("ParallexCIBCon"));
    services.AddDbContext<ParallexCIBContext>(options => options.UseSqlServer(con));
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddHttpClient("tokenClient",c => {
      c.BaseAddress = new Uri(configuration.GetValue<string>("prodApiUrl:baseUrl"));
      c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    });
    services.AddScoped<IApiService, ApiService>();
    services.AddHostedService<Worker>();
  })
   .ConfigureLogging((hostingContext,logging) =>
    {
      logging.AddNLog(hostingContext.Configuration.GetSection("Logging")); 
      logging.SetMinimumLevel(LogLevel.Information);
    });
}

