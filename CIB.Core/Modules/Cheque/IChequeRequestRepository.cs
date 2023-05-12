using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.Cheque
{
    public interface IChequeRequestRepository : IRepository<TblChequeRequest>
    {
        List<TblChequeRequest> GetChequeRequetsByCorporateCustomer(Guid corporateCustomerId);
        List<TblChequeRequest>GetChequeRequestList(int status);
    }
}