using System;
using CIB.Core.Modules.ApprovalHistory.Corporate;
using CIB.Core.Modules.AuditTrial;
using CIB.Core.Modules.Authentication;
using CIB.Core.Modules.BankAdminProfile;
using CIB.Core.Modules.CorporateCustomer;
using CIB.Core.Modules.CorporateProfile;
using CIB.Core.Modules.CorporateRole;
using CIB.Core.Modules.CorporateUserRoleAccess;
using CIB.Core.Modules.LoginLogCorporate;
using CIB.Core.Modules.PasswordReset;
using CIB.Core.Modules.Role;
using CIB.Core.Modules.SecurityQuestion;
using CIB.Core.Modules.TokenBlackCoporate;
using CIB.Core.Modules.TokenBlack;
using CIB.Core.Modules.Transaction;
using CIB.Core.Modules.Transaction._NipbulkCreditLog;
using CIB.Core.Modules.Transaction._NipbulkTransferLog;
using CIB.Core.Modules.Transaction._PendingCreditLog;
using CIB.Core.Modules.Transaction._PendingTranLog;
using CIB.Core.Modules.UserAccess;
using CIB.Core.Modules.UserRoleAccess;
using CIB.Core.Modules.Workflow;
using CIB.Core.Modules.WorkflowHierarchy;
using CIB.Core.Modules.CorporateCustomer._InterBankBeneficiary;
using CIB.Core.Modules.CorporateCustomer._IntraBankBeneficiary;
using CIB.Core.Modules.CorporateBulkApprovalHistory;
using CIB.Core.Modules.TransactionLimitHistory;
using CIB.Core.Modules.TemBankAdminProfile;
using CIB.Core.Modules.TemCorporateCustomer;
using CIB.Core.Modules.TemCorporateProfile;
using CIB.Core.Modules.PasswordHistory;
using CIB.Core.Modules.TempWorkFlow;
using CIB.Core.Modules.TempWorkflowHierarchy;
using CIB.Core.Modules.NipsFeeCharge;

namespace CIB.Core.Common.Interface
{
    public interface IUnitOfWork : IDisposable
    {
        IBankProfileRepository BankProfileRepo { get; }
        ITemBankAdminProfileRepository TemBankAdminProfileRepo { get; }
        ITemCorporateCustomerRespository TemCorporateCustomerRepo { get; }
        ITemCorporateProfileRepository TemCorporateProfileRepo { get; }
        IUserAccessRepository UserAccessRepo { get; }
        IUserRoleAccessRepository UserRoleAccessRepo { get; }
        ICorporateProfileRepository CorporateProfileRepo { get; }
        ICorporateCustomerRepository CorporateCustomerRepo { get; }
        ICorporateUserRoleAccess CorporateUserRoleAccessRepo { get; }
        IBankAuthenticationRepository BankAuthenticationRepo { get; }
        ICustomerAuthenticationRepository CustomerAuthenticationRepo { get; }
        IRoleRepository RoleRepo { get; }
        ICorporateRoleRepository CorporateRoleRepo { get; }
        IPasswordResetRepository PasswordResetRepo { get; }
        IAuditTrialRepository AuditTrialRepo { get; }
        ISecurityQuestionRespository SecurityQuestionRepo { get; }
        IWorkFlowRepository WorkFlowRepo { get; }
        IWorkflowHierarchyRepository WorkFlowHierarchyRepo { get; }
        ITokenBlackCorporateRepository  TokenBlackCorporateRepo{ get; }
        ITokenBlackRepository  TokenBlackRepo{ get; }
        ILoginLogCorporateRepository LoginLogCorporate { get; }
        IPendingCreditLogRepository PendingCreditLogRepo { get; }
        IPendingTranLogRepository PendingTranLogRepo { get; }
        ITransactionRepository TransactionRepo { get; }
        ICorporateApprovalHistoryRepository CorporateApprovalHistoryRepo { get; }
        INipBulkCreditLogRepository NipBulkCreditLogRepo { get; }
        INipBulkTransferLogRespository NipBulkTransferLogRepo { get;  }
        IInterBankBeneficiaryRepository InterBankBeneficiaryRepo { get; }
        IIntraBankBeneficiaryRepository IntraBankBeneficiaryRepo { get; }
        ICorporateBulkApprovalHistoryRepository CorporateBulkApprovalHistoryRepo { get; }
        ITransactionHistoryRepository TransactionHistoryRepo { get; }
        IPasswordHistoryRepository PasswordHistoryRepo { get; }
        ITempWorkflowRepository TempWorkflowRepo{get;}
        ITempWorkflowHierarchyRepository  TempWorkflowHierarchyRepo {get;}
        INipsFeeChargeRepository  NipsFeeChargeRepo {get;}
        int Complete();
        new void Dispose();
  }
}