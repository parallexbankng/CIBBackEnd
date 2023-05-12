using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary
{
    public class ScheduleBeneficairyRepository : Repository<TblCorporateSalaryScheduleBeneficiary>, IScheduleBeneficairyRepository
    {
        public ScheduleBeneficairyRepository(ParallexCIBContext context) : base(context)
        {
            
        }
        public ParallexCIBContext context
        {
         get { return _context as ParallexCIBContext; }
        }

        public async Task<List<TblCorporateSalaryScheduleBeneficiary>> GetScheduleBeneficiaries(TblCorporateSalarySchedule entity)
        {
            return _context.TblCorporateSalaryScheduleBeneficiaries.Where(ctx => ctx.CorporateCustomerId == entity.CorporateCustomerId && ctx.ScheduleId == entity.Id).ToList();
        }
  }
}