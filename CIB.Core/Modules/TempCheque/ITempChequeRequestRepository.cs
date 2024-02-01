using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.Cheque.Dto;

namespace CIB.Core.Modules.Cheque
{
    public interface ITempChequeRequestRepository : IRepository<TblTempChequeRequest>
    {
        DuplicateStatus CheckDuplicate(TblTempChequeRequest chequeRequet, bool isUpdate = false);
        List<TblTempChequeRequest> GetChequeRequestList(int status);
        void UpdateChequeRequest(TblTempChequeRequest request);

    }
}