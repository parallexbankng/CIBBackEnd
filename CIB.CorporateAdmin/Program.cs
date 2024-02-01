﻿
using NLog.Web;
using NLog.Extensions.Logging;

namespace CIB.CorporateAdmin
{
	public class Program
	{
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
		private static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
			.ConfigureWebHostDefaults(webBuilder =>
			{
				webBuilder.UseStartup<Startup>()
				.ConfigureLogging((hostingContext, logging) =>
				{
					var rsuif = hostingContext.Configuration.GetSection("Logging");
					logging.AddNLog(hostingContext.Configuration.GetSection("Logging"));
					logging.SetMinimumLevel(LogLevel.Information);
				});
			});
	}
}
