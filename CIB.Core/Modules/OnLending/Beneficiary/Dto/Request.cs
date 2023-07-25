using System;
using CIB.Core.Modules.Transaction.Dto;
using Microsoft.AspNetCore.Http;

namespace CIB.Core.Modules.OnLending.Beneficiary.Dto
{
    public class VerifyOnlendingBeneficiaryDto
    {
        public string? SourceAccountNumber { get; set; }
        public string? OperatingAccountNumber { get; set; }
        public string? TransactionLocation { get; set; }
        public string? Narration { get; set; }
        public string? WorkflowId { get; set; }
        public string? Currency { get; set; }
        public IFormFile files { get; set; }
    }

    public class InitiaOnlendingBeneficiaryDto : BaseTransactioDto
    {
        public string? SourceAccountNumber { get; set; }
        public string? TransactionLocation { get; set; }
        public string? Narration { get; set; }
        public string? WorkflowId { get; set; }
        public string? Currency { get; set; }
        public IFormFile files { get; set; }
    }

    public class InitiaOnlendingBeneficiary : BaseTransactioDto
    {
        public string? SourceAccountNumber { get; set; }
        public string? OperatingAccountNumber { get; set; }
        public string? TransactionLocation { get; set; }
        public string? Narration { get; set; }
        public Guid? WorkflowId { get; set; }
        public string? Currency { get; set; }
        public IFormFile files { get; set; }
    }

    public class OnlendingBatchRequest : BaseTransactioDto
    {
        public string? BatchId { get; set; }
    }
	  public class OnlendingBatch : BaseTransactioDto
	  {
		  public string? BatchId { get; set; }
	  }
}