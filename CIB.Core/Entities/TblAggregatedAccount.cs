using System;
using System.Collections.Generic;

#nullable disable
namespace CIB.Core.Entities
{
	public partial class TblAggregatedAccount
	{
		public Guid Id { get; set; }
		public long Sn { get; set; }
		public Guid? AccountAggregationId { get; set; }
		public Guid? CorporateCustomerId { get; set; }
		public string CustomerId { get; set; }
		public string AccountNumber { get; set; }
		public string AccountName { get; set; }
		public int Status { get; set; }
		public DateTime? DateCreated { get; set; }
	}
}