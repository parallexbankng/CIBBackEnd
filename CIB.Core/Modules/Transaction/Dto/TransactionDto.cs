using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Transaction.Dto
{
	public class TransactionDto
	{

	}
	public class InitiateSingleTransactionDto
	{
		public string SourceAccountNumber { get; set; }
		public string DestinationAccountNumber { get; set; }
		public string DestinationBankCode { get; set; }
		public string SourceBankName { get; set; }
		public string DestinationBankName { get; set; }
		public string SourceAccountName { get; set; }
		public string DestinationAccountName { get; set; }
		public decimal Amount { get; set; }
		public string Narration { get; set; }
		public string Otp { get; set; }
		public Guid? WorkflowId { get; set; }
		public string TransactionType { get; set; }
		public string Currency { get; set; }
	}

	public class ApproveTransactionModel : BaseTransactioDto
	{
		public Guid AuthorizerId { get; set; }
		public Guid TranLogId { get; set; }
		public string Pin { get; set; }
		public int AuthLimitIsEnable { get; set; }
		public string Comment { get; set; }
		public string Otp { get; set; }
	}
	public class BulkIntrabankTransactionModel : BaseTransactioDto
	{
		public string BankId { get; set; }
		public string TrnType { get; set; }
		public string TrnSubType { get; set; }
		public string RequestID { get; set; }
		public List<PartTrnRec> PartTrnRec { get; set; }
	}
	public class BulkIntraBankTransactionResponse
	{
		public string RequestId { get; set; }
		public string ResponseCode { get; set; }
		public string ResponseMessage { get; set; }
		public string TrnDt { get; set; }
		public string TrnId { get; set; }
		public List<ErrorDetail> ErrorDetail { get; set; }
	}

	public class ErrorDetail
	{
		public string ErrorCode { get; set; }
		public string ErrorDesc { get; set; }
		public string ErrorSource { get; set; }
		public string ErrorType { get; set; }
	}


	public class PartTrnRec
	{
		public string AcctId { get; set; }
		public string CreditDebitFlg { get; set; }
		public string TrnAmt { get; set; }
		public string currencyCode { get; set; }
		public string TrnParticulars { get; set; }
		public string ValueDt { get; set; }
		public string PartTrnRmks { get; set; }
		public string REFNUM { get; set; }
		public string RPTCODE { get; set; }
		public string TRANPARTICULARS2 { get; set; }
	}

	public class DeclineTransactionModel : BaseTransactioDto
	{
		public Guid TranLogId { get; set; }
		public string Comment { get; set; }
		public string Otp { get; set; }
	}
	public class IntraBankAccountTransferModel
	{
		public decimal amount { get; set; }
		public bool applyFee { get; set; }
		public string chequeNo { get; set; }
		public string currency { get; set; }
		public string destinationAccount { get; set; }
		public string externalReference { get; set; }
		public Fees[] fees { get; set; }
		public string narration { get; set; }
		public int noOfDays { get; set; }
		public string sourceAccount { get; set; }
		public string transType { get; set; }
		public string transferReference { get; set; }
		public string trnCode { get; set; }
	}
	public class Fees
	{
		public string account { get; set; }
		public decimal amount { get; set; }
		public string feeOn { get; set; }
		public string narration { get; set; }
		public string trnCode { get; set; }
	}

	public class InitiateBulkTransferDto
	{
		public string SourceAccountNumber { get; set; }
		public string Narration { get; set; }
		public string Otp { get; set; }
		public Guid? WorkflowId { get; set; }
		public string TransactionLocation { get; set; }
		public string Currency { get; set; }
	}
	public class TransactionReportDto
	{
		public Guid Id { get; set; }
		public DateTime? Date { get; set; }
		public string Narration { get; set; }
		public string Reference { get; set; }
		public decimal? Amount { get; set; }
		public string Beneficiary { get; set; }
		public string BeneficiaryName { get; set; }
		public string Sender { get; set; }
		public string SenderName { get; set; }
		public string Type { get; set; }
		public string Status { get; set; }
		public long Sn { get; set; }
		public int? CreditNumber { get; set; }
		public Guid? WorkflowId { get; set; }
		public string DebitAccount { get; set; }
		public Guid TranLogId { get; set; }
	}

}