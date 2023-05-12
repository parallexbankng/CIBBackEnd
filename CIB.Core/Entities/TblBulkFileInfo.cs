using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblBulkFileInfo
    {
        public long Id { get; set; }
        public string BulkFileId { get; set; }
        public string CustomerCode { get; set; }
        public string FileName { get; set; }
        public int? DebitMode { get; set; }
        public string BulkNumber { get; set; }
        public string SourceAccount { get; set; }
        public string SuspenseAccount { get; set; }
        public string IsNameEnquiryCompleted { get; set; }
        public string SourceAccountName { get; set; }
        public string Narration { get; set; }
        public int? ApprovalStage { get; set; }
        public int? ApprovalCount { get; set; }
        public int? Status { get; set; }
        public int? ApprovedStatus { get; set; }
        public decimal? TotalAmount { get; set; }
        public Guid? PostedBy { get; set; }
        public DateTime? DateUpload { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public Guid? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
    }
}
