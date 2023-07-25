
using CIB.Core.Utils;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Utils
{
    public static class LogFormater<T>
    {
        public static void Error(ILogger<T> _logger,string action,string errorMessage, string customer, string? accountNumber = null)
        {
            _logger.LogError("Action:"+action+","+"Message:{0}, INFO:{1}, INFO:{2}",Formater.JsonType(errorMessage), Formater.JsonType(customer),Formater.JsonType(accountNumber));
        }
        public static void Info(ILogger<T> _logger,string action,string errorMessage, string customer, string? accountNumber = null)
        {
            _logger.LogInformation("Action:"+action+","+"Message:{0}, INFO:{1}, INFO:{2}",Formater.JsonType(errorMessage), Formater.JsonType(customer),Formater.JsonType(accountNumber));
        }
    }
}