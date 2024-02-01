using System.Net;

namespace CIB.InterBankTransactionService.Utils;
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
