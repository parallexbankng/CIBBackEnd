using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.Cheque.Dto;

namespace CIB.Core.Modules.Cheque
{
    public class TempChequeRequestRepository :Repository<TblTempChequeRequest>, ITempChequeRequestRepository
    {
      public TempChequeRequestRepository(ParallexCIBContext context) : base(context)
      {

      }
      public ParallexCIBContext context
      {
      get { return _context as ParallexCIBContext; }
      }

      public  DuplicateStatus CheckDuplicate(TblTempChequeRequest chequeRequet,bool isUpdate)
      {
          return new DuplicateStatus();
      }

      public List<TblTempChequeRequest> GetChequeRequestList(int status)
      {
        return _context.TblTempChequeRequests.Where(ctx => ctx.IsTreated == status).ToList();
      }

      public void UpdateChequeRequest(TblTempChequeRequest request)
      {
        _context.Update(request).Property(x=>x.Sn).IsModified = false;
      }
  }
}