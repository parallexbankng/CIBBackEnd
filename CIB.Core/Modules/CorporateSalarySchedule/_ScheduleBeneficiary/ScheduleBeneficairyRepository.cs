
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary.Dto;
using Microsoft.EntityFrameworkCore;

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
            return  await _context.TblCorporateSalaryScheduleBeneficiaries.Where(ctx => ctx.CorporateCustomerId == entity.CorporateCustomerId && ctx.ScheduleId == entity.Id).ToListAsync();
        }

        public async Task<List<ScheduleBeneficiaryResponse>> GetScheduleBeneficiaryDetails(TblCorporateSalarySchedule entity)
        {
            var list  = await _context.TblCorporateSalaryScheduleBeneficiaries.Where(ctx => ctx.CorporateCustomerId == entity.CorporateCustomerId && ctx.ScheduleId == entity.Id)
                .Join(_context.TblCorporateCustomerEmployees, ra => ra.EmployeeId, ua => ua.Id, (ra, ua) => new ScheduleBeneficiaryResponse{Id = ra.Id, FullName = $"{ua.FirstName} {ua.LastName}", Amount=ra.Amount, CorporateCustomerId = ra.CorporateCustomerId, EmployeeId = ra.EmployeeId}).ToListAsync();
            return list;
        }

        public TblCorporateSalaryScheduleBeneficiary CheckScheduleBeneficiary(Guid corporateCustomerId,Guid scheduleId, Guid employee)
        {
            return _context.TblCorporateSalaryScheduleBeneficiaries.Where(ctx => ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == corporateCustomerId && ctx.ScheduleId == scheduleId && ctx.EmployeeId == employee).FirstOrDefault();
        }
  }
}