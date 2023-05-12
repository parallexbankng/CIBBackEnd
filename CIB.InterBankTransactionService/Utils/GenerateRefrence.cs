namespace CIB.InterBankTransactionService.Utils;
  public static class Transactions
  {
    public static string Ref()
    {
      var dateTime = DateTime.Now;
      var unixTime = ((DateTimeOffset)dateTime).ToUnixTimeSeconds().ToString();
      var date = DateTime.Now.ToString("yyyyMMddHHmmss");
      return  date + unixTime[^2..];
    }
  }
