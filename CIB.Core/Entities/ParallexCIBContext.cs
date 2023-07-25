using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class ParallexCIBContext : DbContext
    {
        public ParallexCIBContext()
        {
        }

        public ParallexCIBContext(DbContextOptions<ParallexCIBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<TblAuditTrail> TblAuditTrails { get; set; }
        public virtual DbSet<TblBankProfile> TblBankProfiles { get; set; }
        public virtual DbSet<TblBulkFileInfo> TblBulkFileInfos { get; set; }
        public virtual DbSet<TblCorporateApprovalHistory> TblCorporateApprovalHistories { get; set; }
        public virtual DbSet<TblCorporateBulkApprovalHistory> TblCorporateBulkApprovalHistories { get; set; }
        public virtual DbSet<TblCorporateCustomer> TblCorporateCustomers { get; set; }
        public virtual DbSet<TblCorporateCustomerDailyTransLimitHistory> TblCorporateCustomerDailyTransLimitHistories { get; set; }
        public virtual DbSet<TblCorporateProfile> TblCorporateProfiles { get; set; }
        public virtual DbSet<TblCorporateRole> TblCorporateRoles { get; set; }
        public virtual DbSet<TblCorporateRoleUserAccess> TblCorporateRoleUserAccesses { get; set; }
        public virtual DbSet<TblCustAuth> TblCustAuths { get; set; }
        public virtual DbSet<TblEmailLog> TblEmailLogs { get; set; }
        public virtual DbSet<TblFeeCharge> TblFeeCharges { get; set; }
        public virtual DbSet<TblInterbankbeneficiary> TblInterbankbeneficiaries { get; set; }
        public virtual DbSet<TblIntrabankbeneficiary> TblIntrabankbeneficiaries { get; set; }
        public virtual DbSet<TblLoginLog> TblLoginLogs { get; set; }
        public virtual DbSet<TblLoginLogCorp> TblLoginLogCorps { get; set; }
        public virtual DbSet<TblNipbulkCreditLog> TblNipbulkCreditLogs { get; set; }
        public virtual DbSet<TblNipbulkTransferLog> TblNipbulkTransferLogs { get; set; }
        public virtual DbSet<TblPasswordHistory> TblPasswordHistories { get; set; }
        public virtual DbSet<TblPasswordReset> TblPasswordResets { get; set; }
        public virtual DbSet<TblPendingCreditLog> TblPendingCreditLogs { get; set; }
        public virtual DbSet<TblPendingTranLog> TblPendingTranLogs { get; set; }
        public virtual DbSet<TblRole> TblRoles { get; set; }
        public virtual DbSet<TblRoleUserAccess> TblRoleUserAccesses { get; set; }
        public virtual DbSet<TblSecurityQuestion> TblSecurityQuestions { get; set; }
        public virtual DbSet<TblSmslog> TblSmslogs { get; set; }
        public virtual DbSet<TblTempBankProfile> TblTempBankProfiles { get; set; }
        public virtual DbSet<TblTempCorporateCustomer> TblTempCorporateCustomers { get; set; }
        public virtual DbSet<TblTempCorporateProfile> TblTempCorporateProfiles { get; set; }
        public virtual DbSet<TblTempWorkflow> TblTempWorkflows { get; set; }
        public virtual DbSet<TblTempWorkflowHierarchy> TblTempWorkflowHierarchies { get; set; }
        public virtual DbSet<TblTokenBlack> TblTokenBlacks { get; set; }
        public virtual DbSet<TblTokenBlackCorp> TblTokenBlackCorps { get; set; }
        public virtual DbSet<TblTransaction> TblTransactions { get; set; }
        public virtual DbSet<TblUserAccess> TblUserAccesses { get; set; }
        public virtual DbSet<TblWorkflow> TblWorkflows { get; set; }
        public virtual DbSet<TblWorkflowHierarchy> TblWorkflowHierarchies { get; set; }

     

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<TblAuditTrail>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ClientStaffIpaddress).HasColumnName("ClientStaffIPAddress");

                entity.Property(e => e.Ipaddress).HasColumnName("IPAddress");

                entity.Property(e => e.Macaddress).HasColumnName("MACAddress");

                entity.Property(e => e.UserId).HasColumnName("UserID");
            });

            modelBuilder.Entity<TblBankProfile>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.PasswordExpiryDate).HasColumnType("date");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblBulkFileInfo>(entity =>
            {
                entity.ToTable("TblBulkFileInfo");

                entity.Property(e => e.ApprovedDate).HasColumnType("datetime");

                entity.Property(e => e.BulkFileId).HasMaxLength(200);

                entity.Property(e => e.BulkNumber).HasMaxLength(225);

                entity.Property(e => e.CustomerCode).HasMaxLength(100);

                entity.Property(e => e.DateUpload).HasColumnType("datetime");

                entity.Property(e => e.FileName).HasMaxLength(100);

                entity.Property(e => e.IsNameEnquiryCompleted).HasMaxLength(100);

                entity.Property(e => e.Narration).HasColumnType("text");

                entity.Property(e => e.RejectedDate).HasColumnType("datetime");

                entity.Property(e => e.SourceAccount).HasMaxLength(100);

                entity.Property(e => e.SourceAccountName).HasMaxLength(200);

                entity.Property(e => e.SuspenseAccount).HasMaxLength(100);

                entity.Property(e => e.TotalAmount).HasColumnType("decimal(22, 2)");
            });

            modelBuilder.Entity<TblCorporateApprovalHistory>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblCorporateBulkApprovalHistory>(entity =>
            {
                entity.ToTable("TblCorporateBulkApprovalHistory");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ApprovalDate).HasColumnType("datetime");

                entity.Property(e => e.ApproverName).HasMaxLength(220);

                entity.Property(e => e.Comment).HasColumnType("text");

                entity.Property(e => e.Description).HasColumnType("text");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblCorporateCustomer>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.BulkTransDailyLimit).HasColumnType("decimal(20, 2)");

                entity.Property(e => e.MaxAccountLimit).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.MinAccountLimit).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.SingleTransDailyLimit).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblCorporateCustomerDailyTransLimitHistory>(entity =>
            {
                entity.ToTable("TblCorporateCustomerDailyTransLimitHistory");

                entity.Property(e => e.BulkTransAmountLeft).HasColumnType("decimal(20, 2)");

                entity.Property(e => e.BulkTransTotalAmount).HasColumnType("decimal(18, 0)");

                entity.Property(e => e.CustomerId)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Date).HasColumnType("datetime");

                entity.Property(e => e.SingleTransAmountLeft).HasColumnType("decimal(18, 0)");

                entity.Property(e => e.SingleTransTotalAmount).HasColumnType("decimal(18, 0)");
            });

            modelBuilder.Entity<TblCorporateProfile>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.AcctBalance).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.ApprovalLimit).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.PasswordExpiryDate).HasColumnType("date");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblCorporateRole>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ApprovalLimit).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblCorporateRoleUserAccess>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblCustAuth>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.AcctBalance).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblEmailLog>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblFeeCharge>(entity =>
            {
                entity.Property(e => e.FeeAmount).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.MaxAmount).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.MinAmount).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.Vat).HasColumnType("decimal(22, 2)");
            });

            modelBuilder.Entity<TblInterbankbeneficiary>(entity =>
            {
                entity.ToTable("TblInterbankbeneficiary");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.AccountName).HasMaxLength(200);

                entity.Property(e => e.AccountNumber).HasMaxLength(50);

                entity.Property(e => e.DateAdded).HasColumnType("datetime");

                entity.Property(e => e.DestinationInstitutionCode).HasMaxLength(100);

                entity.Property(e => e.DestinationInstitutionName).HasMaxLength(100);

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblIntrabankbeneficiary>(entity =>
            {
                entity.ToTable("TblIntrabankbeneficiary");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.AccountName).HasMaxLength(225);

                entity.Property(e => e.AccountNumber).HasMaxLength(20);

                entity.Property(e => e.DateAdded).HasColumnType("datetime");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblLoginLog>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblLoginLogCorp>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblNipbulkCreditLog>(entity =>
            {
                entity.ToTable("TblNipbulkCreditLog");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.BankVerificationNo).HasMaxLength(200);

                entity.Property(e => e.ChannelCode).HasMaxLength(20);

                entity.Property(e => e.CreditAccountName).HasMaxLength(200);

                entity.Property(e => e.CreditAccountNumber).HasMaxLength(20);

                entity.Property(e => e.CreditAmount).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.CreditBankCode).HasMaxLength(20);

                entity.Property(e => e.CreditBankName).HasMaxLength(200);

                entity.Property(e => e.CreditDate).HasColumnType("datetime");

                entity.Property(e => e.Fee).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.InitiateDate).HasColumnType("datetime");

                entity.Property(e => e.KycLevel).HasMaxLength(20);

                entity.Property(e => e.NameEnquiryRef).HasMaxLength(200);

                entity.Property(e => e.Narration).HasColumnType("text");

                entity.Property(e => e.ResponseCode).HasMaxLength(20);

                entity.Property(e => e.ResponseMessage).HasColumnType("text");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();

                entity.Property(e => e.TransactionReference).HasMaxLength(225);

                entity.Property(e => e.Vat).HasColumnType("decimal(22, 2)");
            });

            modelBuilder.Entity<TblNipbulkTransferLog>(entity =>
            {
                entity.ToTable("TblNIPBulkTransferLog");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.BulkFileName).HasMaxLength(220);

                entity.Property(e => e.BulkFilePath).HasMaxLength(225);

                entity.Property(e => e.Comment).HasColumnType("text");

                entity.Property(e => e.Currency).HasMaxLength(20);

                entity.Property(e => e.DateInitiated).HasColumnType("datetime");

                entity.Property(e => e.DateProccessed).HasColumnType("datetime");

                entity.Property(e => e.DebitAccountName).HasMaxLength(220);

                entity.Property(e => e.DebitAccountNumber).HasMaxLength(20);

                entity.Property(e => e.DebitAmount).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.DebitMode).HasMaxLength(225);

                entity.Property(e => e.ErrorDetail).HasColumnType("text");

                entity.Property(e => e.InitiatorUserName).HasMaxLength(100);

                entity.Property(e => e.InterBankTotalAmount).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.InterBankTryCount).HasColumnName("interBankTryCount");

                entity.Property(e => e.IntraBankTotalAmount).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.IntreBankSuspenseAccountName).HasMaxLength(225);

                entity.Property(e => e.IntreBankSuspenseAccountNumber).HasMaxLength(225);

                entity.Property(e => e.Narration).HasColumnType("text");

                entity.Property(e => e.OriginatorBvn)
                    .HasMaxLength(20)
                    .HasColumnName("OriginatorBVN");

                entity.Property(e => e.PostingType).HasMaxLength(20);

                entity.Property(e => e.ResponseCode).HasMaxLength(225);

                entity.Property(e => e.ResponseDescription).HasMaxLength(225);

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();

                entity.Property(e => e.Status).HasColumnName("status");

                entity.Property(e => e.SuspenseAccountName).HasMaxLength(225);

                entity.Property(e => e.SuspenseAccountNumber).HasMaxLength(120);

                entity.Property(e => e.TotalFee).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.TotalVat).HasColumnType("decimal(22, 2)");

                entity.Property(e => e.TransactionLocation).HasMaxLength(225);

                entity.Property(e => e.TransactionReference).HasMaxLength(225);

                entity.Property(e => e.TransferType).HasMaxLength(20);
            });

            modelBuilder.Entity<TblPasswordHistory>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblPasswordReset>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblPendingCreditLog>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreditAmount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Fee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();

                entity.Property(e => e.Vat).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<TblPendingTranLog>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.DebitAmount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Fee).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();

                entity.Property(e => e.Vat).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<TblRole>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblRoleUserAccess>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblSmslog>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblTempBankProfile>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblTempCorporateCustomer>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ActionResponseDate).HasColumnType("datetime");

                entity.Property(e => e.ApprovalLimit).HasColumnType("decimal(30, 2)");

                entity.Property(e => e.BulkTransDailyLimit).HasColumnType("decimal(30, 2)");

                entity.Property(e => e.DateRequested).HasColumnType("datetime");

                entity.Property(e => e.IsApprovalByLimit).HasColumnType("decimal(30, 2)");

                entity.Property(e => e.MaxAccountLimit).HasColumnType("decimal(30, 2)");

                entity.Property(e => e.MinAccountLimit).HasColumnType("decimal(30, 2)");

                entity.Property(e => e.SingleTransDailyLimit).HasColumnType("decimal(30, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblTempCorporateProfile>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ActionResponseDate).HasColumnType("datetime");

                entity.Property(e => e.ApprovalLimit).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.DateOfBirth).HasColumnType("datetime");

                entity.Property(e => e.DateRequested).HasColumnType("datetime");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblTempWorkflow>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ApprovalLimit).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblTempWorkflowHierarchy>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.AccountLimit).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblTokenBlack>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblTokenBlackCorp>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblTransaction>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();

                entity.Property(e => e.TranAmout).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<TblUserAccess>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblWorkflow>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ApprovalLimit).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<TblWorkflowHierarchy>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.AccountLimit).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Sn).ValueGeneratedOnAdd();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
