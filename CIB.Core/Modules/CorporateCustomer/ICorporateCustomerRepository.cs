using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.CorporateCustomer
{
    public interface ICorporateCustomerRepository : IRepository<TblCorporateCustomer>
    {
        IEnumerable<TblCorporateCustomer> GetAllCorporateCustomers();
        TblCorporateCustomer GetCorporateCustomerByCompanyName(string companyName);
        TblCorporateCustomer GetCorporateCustomerByCustomerID(string id);
        TblCorporateCustomer GetCustomerByCustomerId(string customerId);
        CorporateUserStatus CheckDuplicate(TblCorporateCustomer profile, bool IsUpdate = false);
        void UpdateCorporateCustomer(TblCorporateCustomer update);
    }
}