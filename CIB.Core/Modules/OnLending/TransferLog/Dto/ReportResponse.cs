using System;

namespace CIB.Core.Modules.OnLending.TransferLog.Dto
{
  public class ReportResponse
  {
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public decimal? Amount { get; set; }
    public string SourceAccountNumber { get; set; }
    public int? TotalPayment { get; set; }
    public string InitiatedBy { get; set; }
    public string PaymentType { get; set; }
    public string Date { get; set; }
  }

	public class BatchResponse
	{
		public Guid Id { get; set; }
		public Guid BatchId { get; set; }
		public decimal? Amount { get; set; }
		public string SourceAccountNumber { get; set; }
		public int? TotalPayment { get; set; }
		public string InitiatedBy { get; set; }
		public string ApproveBy { get; set; }
		public string PaymentType { get; set; }
		public string Date { get; set; }
	}

	public class ReportListResponse
  {
    public Guid Id { get; set; }
    public string RepaymentDate { get; set; }
    public string BeneficiaryName { get; set; }
    public decimal? Amount { get; set; }
    public string SourceAccountNumber { get; set; }
    public int? ContractStatus { get; set; }
  }

	public class BatchBeneficaryResponse
	{
		public Guid Id { get; set; }
		public Guid BatchId { get; set; }
		public string BeneficiaryName { get; set; }
		public decimal? Amount { get; set; }
		public string BeneficiaryAccountNumber { get; set; }
		public string? Narration { get; set; }
		public string RepaymentDate { get; set; }
	}

	public class BatchBeneficaryRepaymentDateExtensionResponse
	{
		public Guid Id { get; set; }
		public Guid BatchId { get; set; }
		public string BeneficiaryName { get; set; }
		public decimal? Amount { get; set; }
		public string BeneficiaryAccountNumber { get; set; }
		public string? Narration { get; set; }
		public string RepaymentDate { get; set; }
	}
}