using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule.Dto;

namespace CIB.Core.Modules.CorporateSalarySchedule
{
    public interface ICorporateSalaryScheduleRepository : IRepository<TblCorporateSalarySchedule>
    {
        SalaryScheduleDuplicateStatus CheckDuplicate(TblCorporateSalarySchedule schedule, bool isUpdate=false);
        void UpdateCorporateSalarySchedule(TblCorporateSalarySchedule request);
       
    }
}