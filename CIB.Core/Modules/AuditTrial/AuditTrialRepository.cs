using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.AuditTrial
{
	public class AuditTrialRepository : Repository<TblAuditTrail>, IAuditTrialRepository
	{
		public AuditTrialRepository(ParallexCIBContext context) : base(context)
		{
		}
		public ParallexCIBContext context { get { return _context as ParallexCIBContext; } }
		public IEnumerable<TblAuditTrail> Search(Guid? userId, string userName, string action, DateTime dateFrom, DateTime dateTo, int pageNumber, int pageSize, out int totalRecord)
		{
			var record = new List<TblAuditTrail>();

			if (!string.IsNullOrEmpty(userName))
			{
				record = _context.TblAuditTrails.Where(a => a.Username != null && a.Username.ToLower().Contains(userName.ToLower())).ToList();
			}

			if (!string.IsNullOrEmpty(action))
			{
				record = _context.TblAuditTrails.Where(a => a.ActionCarriedOut != null && a.ActionCarriedOut.ToLower().Equals(action.ToLower())).ToList();
			}

			if (userId.HasValue)
			{
				record = _context.TblAuditTrails.Where(a => a.UserId != null && a.UserId.Equals(userId.Value)).ToList();
			}

			if (dateFrom != DateTime.MinValue && dateTo != DateTime.MinValue)
			{
				record = _context.TblAuditTrails.Where(a => a.TimeStamp != null && (DateTime)a.TimeStamp.Value >= dateFrom && (DateTime)a.TimeStamp.Value <= dateTo.AddDays(1).AddMinutes(-1)).ToList();
			}
			if (dateFrom == DateTime.MinValue && dateTo == DateTime.MinValue && !userId.HasValue && string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(action))
			{
				record = _context.TblAuditTrails.OrderByDescending(ctx => ctx.TimeStamp).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
				totalRecord = _context.TblAuditTrails.Count();
				return record;
			}
			totalRecord = record.Count;
			return record.OrderByDescending(ctx => ctx.TimeStamp).Skip((pageNumber - 1) * pageSize).Take(pageSize);
		}
	}
}