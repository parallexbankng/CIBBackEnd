using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.TransactionReversalService.Entities
{
    public partial class TblTransaction
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CustAuthId { get; set; }
        public decimal? TranAmout { get; set; }
        public string SourceAccountNo { get; set; }
        public string SourceAccountName { get; set; }
        public string SourceBank { get; set; }
        public DateTime? TranDate { get; set; }
        public string TranType { get; set; }
        public string Narration { get; set; }
        public string DestinationAcctNo { get; set; }
        public string DestinationAcctName { get; set; }
        public string DesctionationBank { get; set; }
        public string Channel { get; set; }
        public string TransactionReference { get; set; }
        public Guid? BatchId { get; set; }
        public string ElectricityToken { get; set; }
        public string ProviderRef { get; set; }
        public string TransactionStatus { get; set; }
        public string NapsbatchId { get; set; }
        public int? ElectricityTokenSendSms { get; set; }
        public int? ElectricityTokenSmsretryCount { get; set; }
        public string AuthType { get; set; }
        public Guid? CorporateCustomerId { get; set; }
    }
}
