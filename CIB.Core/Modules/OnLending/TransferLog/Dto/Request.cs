using System;
using System.Collections.Generic;
using CIB.Core.Common;

namespace CIB.Core.Modules.OnLending.TransferLog.Dto
{
	public class InitiateDisbursmentRequest : BaseUpdateDto
	{
		public string? BatchId { get; set; }
		public string? Beneficiaries { get; set; }
		public string? Commemt { get; set; }
		public string? Otp { get; set; }
	}

	public class InitiateDisbursment : BaseUpdateDto
	{
		public Guid BatchId { get; set; }
		public List<BeneficiaryId> Beneficiaries { get; set; }
	}

	public class ApprovedInitiateDisbursmentRequest : BaseUpdateDto
	{
		public string? BatchId { get; set; }
		public string? Comment { get; set; }
		public string? Otp { get; set; }
	}

	public class ApprovedInitiateDisbursment : BaseUpdateDto
	{
		public Guid BatchId { get; set; }
		public string? Comment { get; set; }
		public string? Otp { get; set; }
	}




	public class BeneficiaryId
	{
		public Guid? Id { get; set; }
	}

	public class DisbursmentRequest
	{
		public decimal? BatchId { get; set; }
		public decimal? Beneficiaries { get; set; }
	}





}

