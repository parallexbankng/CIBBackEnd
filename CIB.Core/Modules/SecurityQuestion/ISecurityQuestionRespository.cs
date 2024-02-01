
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.SecurityQuestion
{
    public interface ISecurityQuestionRespository : IRepository<TblSecurityQuestion>
    {
        TblSecurityQuestion GetById(int id);
    }
}