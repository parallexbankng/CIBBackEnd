using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;

namespace CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee
{
    public interface ICorporateEmployeeRepository : IRepository<TblCorporateCustomerEmployee>
    {
        CorporateEmployeeDuplicateStatus CheckDuplicate(TblCorporateCustomerEmployee employee,bool IsUpdate = false);
        void UpdateCorporateEmployee(TblCorporateCustomerEmployee request);
        Task<List<TblCorporateCustomerEmployee>> GetCorporateCustomerEmployees(Guid corporateCustomerId);
    }
}