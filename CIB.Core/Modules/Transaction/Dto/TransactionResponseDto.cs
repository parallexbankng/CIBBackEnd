using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.BulkTransaction.Dto;

namespace CIB.Core.Modules.Transaction.Dto
{
	public class TransactionResponseDto
	{
		public int AuthLimit { get; set; }
		public int AuthLimitIsEnable { get; set; }
		public string Message { get; set; }
		public bool Status { get; set; }
	}


}