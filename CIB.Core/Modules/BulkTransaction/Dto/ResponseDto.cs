using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Entities;

namespace CIB.Core.Modules.BulkTransaction.Dto
{
	public class VerifyBulkTransactionResponseDto
	{
		public string AccountName { get; set; }
		public string CreditAccount { get; set; }
		public decimal CreditAmount { get; set; }
		public string Narration { get; set; }
		public string BankCode { get; set; }
		public string BankName { get; set; }
		public string Error { get; set; }
		// public string Error { get; set; }
	}

	public class VerifyBulkTransactionResponse
	{
		public int AuthLimit { get; set; }
		public int AuthLimitIsEnable { get; set; }
		public decimal Vat { get; set; }
		public decimal Fee { get; set; }
		public decimal TotalAmount { get; set; }
		public List<VerifyBulkTransactionResponseDto> Transaction { get; set; }
	}
	public class ApproveTransactionResponse
	{
		public long Sn { get; set; }
		public int AuthLimit { get; set; }
		public int AuthLimitIsEnable { get; set; }
		public TblPendingTranLog PendingTransaction { get; set; }
	}
	public class ApproveBulkTransactionResponse
	{
		public long Sn { get; set; }
		public int AuthLimit { get; set; }
		public int AuthLimitIsEnable { get; set; }
		public TblNipbulkTransferLog PendingTransaction { get; set; }
	}
}