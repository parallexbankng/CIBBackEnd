using System.Runtime.Serialization;
using System.Collections.Immutable;
using System.Net;

namespace CIB.IntraBankTransactionService.Utils;
public static class Transactions
{
  public static string Ref()
  {
    var dateTime = DateTime.Now;
    var unixTime = ((DateTimeOffset)dateTime).ToUnixTimeSeconds().ToString();
    var date = DateTime.Now.ToString("yyyyMMddHHmmss");
    return date + unixTime[^2..];
  }
  public static string GetHostIp()
  {
    string hostName = Dns.GetHostName();
    string myIP = Dns.GetHostByName(hostName).AddressList[1].ToString();
    return myIP;
  }
}
