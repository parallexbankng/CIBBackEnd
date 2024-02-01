using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Services.File.Dto
{
    public class UploadExcelFileResponseDto
    {
        public string RequestId { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string SourceAccountName { get; set; }
        public string SourceAccountNumber { get; set; }
        public string Narration { get; set; }
        public string SourceBankName { get; set; }
        public string SourceBankCode { get; set; }
        public string TransactionType { get; set; }
        public List<NameEnquireResponseDto> NameEnquire{ get; set; }
  }
    public class NameEnquireResponseDto
    {
        public string CreditAccountName { get; set; }
        public string CreditAccountNumber { get; set; }
        public string Narration { get; set; }
        public string SourceBankName { get; set; }
        public string SourceBankCode { get; set; }
        public string BVN { get; set; }
        public string KYCLevel { get; set; }
    }
}