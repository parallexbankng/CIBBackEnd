using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateBulkApprovalHistory
{
    public interface ICorporateBulkApprovalHistoryRepository : IRepository<TblCorporateBulkApprovalHistory>
    {
    List<TblCorporateBulkApprovalHistory> GetCorporateBulkAuthorizationHistories(Guid BulkTranId);
  }
}