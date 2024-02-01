using System;

using System.Text;

namespace CIB.Core.Utils
{
    public static class Generate16DigitNumber
    {
        public static string Create16DigitString()
        {
            var dateTime = DateTime.Now;
            var unixTime = ((DateTimeOffset)dateTime).ToUnixTimeSeconds().ToString();
            var date = DateTime.Now.ToString("yyyyMMddHHmmss");
            return date + unixTime[^2..];
            //999015221215162807230328397425
        }
        public static string GenerateRestPin()
        {
            var dateTime = DateTime.Now;
            var unixTime = ((DateTimeOffset)dateTime).ToUnixTimeSeconds().ToString();
            return unixTime[^4..];
        }

    }

}