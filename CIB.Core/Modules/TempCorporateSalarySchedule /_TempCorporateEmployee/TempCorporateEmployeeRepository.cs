
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.Cheque.Dto;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;

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
            var checkDuplicateStaffId = _context.TblCorporateCustomerEmployees.Where(ctx => ctx.StaffId == employee.StaffId && ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == employee.CorporateCustomerId).FirstOrDefault();
            var checkDuplicateAccountNumber = _context.TblCorporateCustomerEmployees.Where(ctx => ctx.AccountNumber== employee.AccountNumber && ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == employee.CorporateCustomerId).FirstOrDefault();

            if(checkDuplicateStaffId != null)
            {
                if(IsUpdate)
                {
                    if(employee.Id != checkDuplicateStaffId.Id)
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
                    if(employee.Id != checkDuplicateAccountNumber.Id)
                    {
                        return new CorporateEmployeeDuplicateStatus { Message = "newly updated Account number Already Exit", IsDuplicate = true };
                    }
                }
                else
                {
                    return new CorporateEmployeeDuplicateStatus { Message = "Account number id Already Exit", IsDuplicate =true};
                }
            }

            return new CorporateEmployeeDuplicateStatus { Message = "", IsDuplicate = false };
        }

        public void UpdateCorporateEmployee(TblTempCorporateCustomerEmployee request)
        {
          _context.Update(request).Property(x=>x.Sn).IsModified = false;
        }
  
  }
}