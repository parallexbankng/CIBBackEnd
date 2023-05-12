using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.SecurityQuestion
{
  public class SecurityQuestionRespository : Repository<TblSecurityQuestion>,ISecurityQuestionRespository
  {
    public SecurityQuestionRespository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public TblSecurityQuestion GetById(int id)
    {
      return _context.TblSecurityQuestions.FirstOrDefault(ctx => ctx.Id == id);
    }
  }
}