using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Utils
{
    public static class Helper
    {
        public static string formatCurrency(decimal? amount, string currencyCode = "NGN")
        {
            string currency = string.Empty;
            if (amount.HasValue) currency = String.Format("{0:n}", amount);
            return $"{currencyCode} {currency}";
        }
    }
}