using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Transaction.Dto.BulkUpload
{
    public class BulkUploadDto
    {
        public string phone { get; set; }
        public string BulkTranLogId { get; set; }
    }

    public class VerifyBulkUploadDto
    {
        public string phone { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Narration { get; set; }
        public string OriginatorAccountNumber { get; set; }
        public string OriginatorAccountName { get; set; }
        public string OriginatorBVN { get; set; }
        public string CreditUpload { get; set; }
    }
}