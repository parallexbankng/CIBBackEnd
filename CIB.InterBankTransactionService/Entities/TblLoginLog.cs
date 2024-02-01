﻿using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.InterBankTransactionService.Entities
{
    public partial class TblLoginLog
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CustAuth { get; set; }
        public DateTime? LoginTime { get; set; }
        public int? NotificationStatus { get; set; }
        public string Channel { get; set; }
    }
}
