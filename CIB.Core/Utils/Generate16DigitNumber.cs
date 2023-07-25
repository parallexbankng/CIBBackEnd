using System;
using System.Security.Cryptography;
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

	public static string CreateDigitString(int num)
	{
		RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
		byte[] randomNumber = new byte[num];//4 for int32
		rng.GetBytes(randomNumber);
		int value = BitConverter.ToInt32(randomNumber, 0);
		return value.ToString();
		//return RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
	}

}
  public class GenerateBulkTransactionRefrences
  {
    public string Create16DigitString()
    {
          // date
      var dateTime = DateTime.UtcNow;
      //var unixTime = ((DateTimeOffset)dateTime).ToUnixTimeSeconds().ToString();
      var result = DateTime.Now.ToString("yyyyMMddHHmmss");
	  var numnber = CreateDigitString(6);

	  return $"{result}{numnber}";
    }

		public static string CreateDigitString(int num)
		{
			RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
			byte[] randomNumber = new byte[num];//4 for int32
			rng.GetBytes(randomNumber);
			int value = BitConverter.ToInt32(randomNumber, 0);
			return value.ToString().Replace("-","");
			//return RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
		}
	}

	


	
}