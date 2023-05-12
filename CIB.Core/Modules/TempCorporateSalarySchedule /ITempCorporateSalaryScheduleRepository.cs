using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule.Dto;

namespace CIB.Core.Modules.TempCorporateSalarySchedule
{
    public interface ITempCorporateSalaryScheduleRepository : IRepository<TblTempCorporateSalarySchedule>
    {
        SalaryScheduleDuplicateStatus CheckDuplicate(TblTempCorporateSalarySchedule schedule, bool isUpdate=false);
        void UpdateSTempalarySchedule(TblTempCorporateSalarySchedule update);
    }
}