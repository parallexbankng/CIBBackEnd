using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.Transaction._NipbulkCreditLog
{
  public class NipBulkCreditLogRepository : Repository<TblNipbulkCreditLog>,INipBulkCreditLogRepository
  {
    public NipBulkCreditLogRepository(ParallexCIBContext context) : base(context)
    {
    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public List<TblNipbulkCreditLog> GetbulkCreditLog(Guid TranLog)
    {
    //   var innerGroupJoinQuery2 =
    // from category in categories
    // join prod in products on category.ID equals prod.CategoryID into prodGroup
    // from prod2 in prodGroup
    // where prod2.UnitPrice > 2.50M
    // select prod2
      //return _context.TblNipbulkCreditLogs.Where(ctx => ctx.TranLogId == TranLog && ctx.NameEnquiryStatus == 1).ToList();
      return _context.TblNipbulkCreditLogs.Where(ctx => ctx.TranLogId == TranLog && ctx.CreditReversalId == null).ToList();
    }

    public List<TblNipbulkCreditLog> GetbulkCreditLogStatus(Guid TranLog, int Status)
    {
      return _context.TblNipbulkCreditLogs.Where(ctx => ctx.TranLogId == TranLog && ctx.CreditStatus == Status && ctx.CreditReversalId == null).ToList();
    }

    public void UpdateBulkCreditLog(TblNipbulkCreditLog update)
    {
      _context.Update(update).Property(x => x.Sn).IsModified = false;
    }

    void INipBulkCreditLogRepository.UpdateBulkCreditLogList(IEnumerable<TblNipbulkCreditLog> update)
    {
      //_context.UpdateRange(update);
      foreach(var item in update){
         _context.Update(item).Property(x => x.Sn).IsModified = false;
      }
     
    }
  }
}
