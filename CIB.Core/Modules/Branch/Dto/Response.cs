using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Branch.Dto
{
    public partial class BankBranchResponse
    {
        public long Id { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string BranchAddress { get; set; }
        public string SortCode { get; set; }
    }
}