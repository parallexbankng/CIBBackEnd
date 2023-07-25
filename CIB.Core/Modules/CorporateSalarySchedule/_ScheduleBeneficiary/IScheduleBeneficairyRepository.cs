
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary.Dto;

namespace CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary
{
    public interface IScheduleBeneficairyRepository : IRepository<TblCorporateSalaryScheduleBeneficiary>
    {
        Task<List<TblCorporateSalaryScheduleBeneficiary>> GetScheduleBeneficiaries(TblCorporateSalarySchedule entity);
        TblCorporateSalaryScheduleBeneficiary CheckScheduleBeneficiary(Guid corporateCustomerId,Guid scheduleId, Guid employee);
        Task<List<ScheduleBeneficiaryResponse>> GetScheduleBeneficiaryDetails(TblCorporateSalarySchedule entity);
        
    }
}