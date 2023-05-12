using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.LoginLogCorporate
{
    public interface ILoginLogCorporateRepository :IRepository<TblLoginLogCorp>
    {
        void UpdateLoginLogCorporate(TblLoginLogCorp update);
    }
}