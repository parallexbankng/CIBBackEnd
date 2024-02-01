using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Entities;

namespace CIB.Core.Utils
{
    public static class NipsCharge
    {
        public static ChargesDto Calculate(IReadOnlyList<TblFeeCharge> charges, decimal principalAmount)
        {
            if(principalAmount == 0)
            {
                return new ChargesDto();
            }
            var chr = new ChargesDto();
            if (charges != null)
            {
                if (charges.Count > 0)
                {
                    var charge = charges.Where(x => x.MinAmount != null && x.MinAmount <= principalAmount && x.MaxAmount >= principalAmount)?.SingleOrDefault();
                    if(charge != null)
                    {
                       chr.Fee = (decimal)charge.FeeAmount;
                       chr.Vat = (decimal)charge.Vat;
                    }
                }
            }
            return chr;
        }
    }
    public class ChargesDto 
    {
        public decimal Fee {get;set;}
        public decimal Vat {get;set;}
    }
}