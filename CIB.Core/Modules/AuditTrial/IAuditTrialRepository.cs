using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.AuditTrial
{
	public interface IAuditTrialRepository : IRepository<TblAuditTrail>
	{
		IEnumerable<TblAuditTrail> Search(Guid? userId, string userName, string action, DateTime dateFrom, DateTime dateTo, int pageNumber, int pageSize, out int totalRecord);

	}
}