using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule.Dto;
using Microsoft.EntityFrameworkCore;

namespace CIB.Core.Modules.CorporateSalarySchedule
{
    public class CorporateSalaryScheduleRepository : Repository<TblCorporateSalarySchedule>, ICorporateSalaryScheduleRepository
    {
        public CorporateSalaryScheduleRepository(ParallexCIBContext context) : base(context)
        {
            
        }
        public ParallexCIBContext context
        {
         get { return _context as ParallexCIBContext; }
        }
        public SalaryScheduleDuplicateStatus CheckDuplicate(TblCorporateSalarySchedule schedule,bool IsUpdate)
        {
            var checkShedule = _context.TblCorporateSalarySchedules.Where(ctx => ctx.Frequency == schedule.Frequency && ctx.Discription.Trim().ToLower() == schedule.Discription.Trim().ToLower() && ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == schedule.CorporateCustomerId).FirstOrDefault();
    
            if(checkShedule != null)
            {
                if(IsUpdate)
                {
                    if(schedule.Id != checkShedule.Id)
                    {
                        return new SalaryScheduleDuplicateStatus { Message = "Schedule Already Exit", IsDuplicate = true };
                    }
                }
                else
                {
                    return new SalaryScheduleDuplicateStatus { Message = "Schedule Already Exit", IsDuplicate =true};
                }
            }
            return new SalaryScheduleDuplicateStatus { Message = "", IsDuplicate = false };
        }

        public void UpdateCorporateSalarySchedule(TblCorporateSalarySchedule request)
        {
          _context.Update(request).Property(x=>x.Sn).IsModified = false;
        }

        public async Task<List<TblCorporateSalarySchedule>> GetCorporateSalarySchedules(Guid CorporateCustomerId)
        {
            return await _context.TblCorporateSalarySchedules.Where(ctx => ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == CorporateCustomerId).ToListAsync();
        }
  }
}