using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Transaction._PendingCreditLog.Dto
{
	public class SingleTransactionDto
	{
		public Guid Id { get; set; }
		public long Sn { get; set; }
		public Guid? CustAuthId { get; set; }
		public decimal? TranAmout { get; set; }
		public string SourceAccountNo { get; set; }
		public string SourceAccountName { get; set; }
		public string SourceBank { get; set; }
		public DateTime? TranDate { get; set; }
		public string TranType { get; set; }
		public string Narration { get; set; }
		public string DestinationAcctNo { get; set; }
		public string DestinationAcctName { get; set; }
		public string DesctionationBank { get; set; }
		public string Channel { get; set; }
		public string TransactionReference { get; set; }
		public Guid? BatchId { get; set; }
		public string TransactionStatus { get; set; }
		public Guid? CorporateCustomerId { get; set; }

	}
	public class TransactionRecipt
	{
		public TransactionRecipt(
			decimal? amount,
			string sourceAccountNo,
			string sourceAccountName,
			string sourceBank,
			DateTime? tranDate,
			string tranType,
			string narration,
			string destinationAcctNo,
			string destinationAcctName,
			string desctionationBank,
			string transactionReference,
			string transactionStatus
			)
		{
			TranAmout = amount;
			SourceAccountNo = sourceAccountNo;
			SourceAccountName = sourceAccountName;
			SourceBank = sourceBank;
			TranDate = tranDate;
			TranType = tranType;
			Narration = narration;
			DestinationAcctNo = destinationAcctNo;
			DestinationAcctName = destinationAcctName;
			DestinationBank = desctionationBank;
			TransactionReference = transactionReference;
			TransactionStatus = transactionStatus;
		}

		public decimal? TranAmout { get; set; }
		public string SourceAccountNo { get; set; }
		public string SourceAccountName { get; set; }
		public string SourceBank { get; set; }
		public DateTime? TranDate { get; set; }
		public string TranType { get; set; }
		public string Narration { get; set; }
		public string DestinationAcctNo { get; set; }
		public string DestinationAcctName { get; set; }
		public string DestinationBank { get; set; }
		public string TransactionReference { get; set; }
		public string TransactionStatus { get; set; }
	}
}

