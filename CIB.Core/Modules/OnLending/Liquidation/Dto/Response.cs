using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.OnLending.Liquidation.Dto
{
  public class VerifyDisbursmentResponse
  {
    public decimal? TotalAmount { get; set; }
    public decimal? TotalInterest { get; set; }
    public int TotalItem { get; set; }

  }
}

// total principale
// total intrest
// total item process
//messag