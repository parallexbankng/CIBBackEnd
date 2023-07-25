using System.Security.Cryptography;

namespace CIB.TransactionReversalService.Utils;
public static class Transactions
{
	public static string Ref()
	{
		var unixTime = CreateDigitString(6);
		var result = DateTime.Now.ToString("yyyyMMddHHmmss");
		return $"{result}{unixTime}";
	}
	public static string CreateDigitString(int num)
	{
		RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
		byte[] randomNumber = new byte[num];//4 for int32
		rng.GetBytes(randomNumber);
		int value = BitConverter.ToInt32(randomNumber, 0);
		return value.ToString().Replace("-", "");
	}
}
