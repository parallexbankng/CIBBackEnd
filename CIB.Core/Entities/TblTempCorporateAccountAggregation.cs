using System;
using System.Collections.Generic;
using static Humanizer.On;

#nullable disable
namespace CIB.Core.Entities
{
	public partial class TblTempCorporateAccountAggregation
	{
		public Guid Id { get; set; }
		public long Sn { get; set; }
		public Guid? CorporateCustomerId { get; set; }
		public Guid? AccountAggregationId { get; set; }
		public Guid? InitiatorId { get; set; }
		public string? DefaultAccountNumber { get; set; }
		public string? DefaultAccountName { get; set; }
		public string? CustomerId { get; set; }
		public string? InitiatorUserName { get; set; }
		public string? Reasons { get; set; }
		public string? Action { get; set; }
		public int? Status { get; set; }
		public int? IsTreated { get; set; }
		public int? PreviousStatus { get; set; }
		public DateTime? DateInitiated { get; set; }
		public DateTime? ActionResponseDate { get; set; }
	}
}
