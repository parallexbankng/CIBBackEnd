
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.Cheque.Dto;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;
using Microsoft.EntityFrameworkCore;

namespace CIB.Core.Modules.TempCorporateSalarySchedule._TempCorporateEmployee
{
    public class TempCorporateEmployeeRepository : Repository<TblTempCorporateCustomerEmployee>, ITempCorporateEmployeeRepository
    {
        public TempCorporateEmployeeRepository(ParallexCIBContext context) : base(context)
        {
            
        }
        public ParallexCIBContext context
        {
         get { return _context as ParallexCIBContext; }
        }

        public CorporateEmployeeDuplicateStatus CheckDuplicate(TblTempCorporateCustomerEmployee employee,bool IsUpdate)
        {
            var checkDuplicateStaffId = _context.TblTempCorporateCustomerEmployees.Where(ctx => ctx.StaffId == employee.StaffId && ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == employee.CorporateCustomerId && ctx.IsTreated == 0).FirstOrDefault();
            var checkDuplicateAccountNumber = _context.TblTempCorporateCustomerEmployees.Where(ctx => ctx.AccountNumber== employee.AccountNumber && ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == employee.CorporateCustomerId && ctx.IsTreated == 0).FirstOrDefault();

            if(checkDuplicateStaffId != null)
            {
                if(IsUpdate)
                {
                    if(employee.CorporateCustomerEmployeeId != checkDuplicateStaffId.CorporateCustomerEmployeeId)
                    {
                        return new CorporateEmployeeDuplicateStatus { Message = "newly updated staff id Already Exit", IsDuplicate = true };
                    }
                }
                else
                {
                    return new CorporateEmployeeDuplicateStatus { Message = "staff id Already Exit", IsDuplicate =true};
                }
            }

            if(checkDuplicateAccountNumber != null)
            {
                if(IsUpdate)
                {
                    if(employee.AccountNumber != checkDuplicateAccountNumber.AccountNumber)
                    {
                        return new CorporateEmployeeDuplicateStatus { Message = "newly updated Account number Already Exit", IsDuplicate = true };
                    }
                }
                else
                {
                    return new CorporateEmployeeDuplicateStatus { Message = "Account number Already Exit", IsDuplicate =true};
                }
            }

            return new CorporateEmployeeDuplicateStatus { Message = "", IsDuplicate = false };
        }

        public void UpdateCorporateEmployee(TblTempCorporateCustomerEmployee request)
        {
          _context.Update(request).Property(x=>x.Sn).IsModified = false;
        }

    public async Task<List<TblTempCorporateCustomerEmployee>> GetPendingCorporateEmployee(Guid CorporateCustomerId)
    {
      return await _context.TblTempCorporateCustomerEmployees.Where(ctx => ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == CorporateCustomerId && ctx.IsTreated == 0).ToListAsync();
    }
  }
}