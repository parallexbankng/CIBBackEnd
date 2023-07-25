using System;
using CIB.Core.Common;
using Microsoft.AspNetCore.Http;

namespace CIB.Core.Modules.OnLending.Liquidation.Dto
{
	public class Request
	{

	}
	public class BatchRepaymentDateExtendionRequest : BaseUpdateDto
	{
		public string? Id { get; set; }
		public string? Duration { get; set; }
	}

	public class BatchRepaymentDateExtendion : BaseUpdateDto
	{
		public Guid Id { get; set; }
		public int? Duration { get; set; }
		//public Guid? WorkflowId { get; set; }
	}

	public class BatchLiquidationRequest : BaseUpdateDto
	{
		public string Id { get; set; }
		public string Amount { get; set; }
		public string Otp { get; set; }
	}
	public class BatchLiquidation : BaseUpdateDto
	{
		public Guid Id { get; set; }
		public decimal Amount { get; set; }
		public string Otp { get; set; }
	}
}

