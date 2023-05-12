using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;

namespace CIB.Core.Modules.TempCorporateSalarySchedule._TempCorporateEmployee
{
    public interface ITempCorporateEmployeeRepository : IRepository<TblTempCorporateCustomerEmployee>
    {
        CorporateEmployeeDuplicateStatus CheckDuplicate(TblTempCorporateCustomerEmployee employee,bool IsUpdate = false);
        void UpdateCorporateEmployee(TblTempCorporateCustomerEmployee request);
    }
}