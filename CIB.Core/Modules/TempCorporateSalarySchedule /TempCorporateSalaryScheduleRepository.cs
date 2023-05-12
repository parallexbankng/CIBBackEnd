using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule.Dto;

namespace CIB.Core.Modules.TempCorporateSalarySchedule
{
    public class TempCorporateSalaryScheduleRepository : Repository<TblTempCorporateSalarySchedule>, ITempCorporateSalaryScheduleRepository
    {
        public TempCorporateSalaryScheduleRepository(ParallexCIBContext context) : base(context)
        {
            
        }
        public ParallexCIBContext context
        {
         get { return _context as ParallexCIBContext; }
        }

        public SalaryScheduleDuplicateStatus CheckDuplicate(TblTempCorporateSalarySchedule schedule, bool isUpdate)
        {
             var checkShedule = _context.TblTempCorporateSalarySchedules.Where(ctx => ctx.Frequency == schedule.Frequency && ctx.Discription.Trim().ToLower() == schedule.Discription.Trim().ToLower() && ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == schedule.CorporateCustomerId).FirstOrDefault();
    
            if(checkShedule != null)
            {
                if(isUpdate)
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

        public void UpdateSTempalarySchedule(TblTempCorporateSalarySchedule update)
        {
            _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }
  }
}