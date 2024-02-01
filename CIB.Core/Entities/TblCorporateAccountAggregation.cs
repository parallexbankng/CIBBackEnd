using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
	public partial class TblCorporateAccountAggregation
	{
		public Guid Id { get; set; }
		public long Sn { get; set; }
		public Guid? CorporateCustomerId { get; set; }
		public string? DefaultAccountNumber { get; set; }
		public string? DefaultAccountName { get; set; }
		public string? CustomerId { get; set; }
		public int? Status { get; set; }
		public DateTime? DateCreated { get; set; }
	}
}