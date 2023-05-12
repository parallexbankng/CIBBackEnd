
using System.Collections.Generic;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary
{
    public interface IScheduleBeneficairyRepository : IRepository<TblCorporateSalaryScheduleBeneficiary>
    {
        Task<List<TblCorporateSalaryScheduleBeneficiary>> GetScheduleBeneficiaries(TblCorporateSalarySchedule entity);
    }
}