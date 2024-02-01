using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.ApprovalHistory.Corporate;
using CIB.Core.Modules.AuditTrial;
using CIB.Core.Modules.Authentication;
using CIB.Core.Modules.BankAdminProfile;
using CIB.Core.Modules.CorporateBulkApprovalHistory;
using CIB.Core.Modules.CorporateCustomer;
using CIB.Core.Modules.CorporateCustomer._InterBankBeneficiary;
using CIB.Core.Modules.CorporateCustomer._IntraBankBeneficiary;
using CIB.Core.Modules.CorporateProfile;
using CIB.Core.Modules.CorporateRole;
using CIB.Core.Modules.CorporateUserRoleAccess;
using CIB.Core.Modules.LoginLogCorporate;
using CIB.Core.Modules.PasswordHistory;
using CIB.Core.Modules.PasswordReset;
using CIB.Core.Modules.Role;
using CIB.Core.Modules.SecurityQuestion;
using CIB.Core.Modules.TemBankAdminProfile;
using CIB.Core.Modules.TemCorporateCustomer;
using CIB.Core.Modules.TemCorporateProfile;
using CIB.Core.Modules.TempWorkFlow;
using CIB.Core.Modules.TempWorkflowHierarchy;
using CIB.Core.Modules.TokenBlack;
using CIB.Core.Modules.TokenBlackCoporate;
using CIB.Core.Modules.Transaction;
using CIB.Core.Modules.Transaction._NipbulkCreditLog;
using CIB.Core.Modules.Transaction._NipbulkTransferLog;
using CIB.Core.Modules.Transaction._PendingCreditLog;
using CIB.Core.Modules.Transaction._PendingTranLog;
using CIB.Core.Modules.TransactionLimitHistory;
using CIB.Core.Modules.UserAccess;
using CIB.Core.Modules.UserRoleAccess;
using CIB.Core.Modules.Workflow;
using CIB.Core.Modules.WorkflowHierarchy;
using CIB.Core.Modules.TempWorkflow;
using CIB.Core.Modules.NipsFeeCharge;
using CIB.Core.Modules.Branch;
using CIB.Core.Modules.Cheque;
using CIB.Core.Modules.AccountAggregation.Accounts;
using CIB.Core.Modules.AccountAggregation.Aggregations;
using CIB.Core.Modules.AccountAggregationTemp.Accounts;
using CIB.Core.Modules.AccountAggregationTemp.Aggregations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CIB.Core.Common.Repository
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly ParallexCIBContext _dbContext;
		public UnitOfWork(ParallexCIBContext context)
		{
			_dbContext = context;
			BankProfileRepo = new BankProfileRepository(_dbContext);
			UserAccessRepo = new UserAccessRepository(_dbContext);
			UserRoleAccessRepo = new UserRoleAccessRepository(_dbContext);
			CorporateProfileRepo = new CorporateProfileRepository(_dbContext);
			CorporateCustomerRepo = new CorporateCustomerRepository(_dbContext);
			CorporateUserRoleAccessRepo = new CorporateUserRoleAccess(_dbContext);
			BankAuthenticationRepo = new BankAuthenticationRepository(_dbContext);
			CustomerAuthenticationRepo = new CustomerAuthenticationRepository(_dbContext);
			RoleRepo = new RoleRepository(_dbContext);
			CorporateRoleRepo = new CorporateRoleRepository(_dbContext);
			PasswordResetRepo = new PasswordResetRepository(_dbContext);
			AuditTrialRepo = new AuditTrialRepository(_dbContext);
			SecurityQuestionRepo = new SecurityQuestionRespository(_dbContext);
			WorkFlowRepo = new WorkFlowRepository(_dbContext);
			WorkFlowHierarchyRepo = new WorkflowHierarchyRespository(_dbContext);
			TokenBlackCorporateRepo = new TokenBlackCorporateRepository(_dbContext);
			TokenBlackRepo = new TokenBlackRepository(_dbContext);
			LoginLogCorporate = new LoginLogCorporateRepository(_dbContext);
			PendingCreditLogRepo = new PendingCreditLogRepository(_dbContext);
			PendingTranLogRepo = new PendingTranLogRepository(_dbContext);
			TransactionRepo = new TransactionRepository(_dbContext);
			CorporateApprovalHistoryRepo = new CorporateApprovalHistoryRepository(_dbContext);
			NipBulkCreditLogRepo = new NipBulkCreditLogRepository(_dbContext);
			NipBulkTransferLogRepo = new NipBulkTransferRespository(_dbContext);
			InterBankBeneficiaryRepo = new InterBankBeneficiaryRepository(_dbContext);
			IntraBankBeneficiaryRepo = new IntraBankBeneficiaryRepository(_dbContext);
			CorporateBulkApprovalHistoryRepo = new CorporateBulkApprovalHistoryRepository(_dbContext);
			TransactionHistoryRepo = new TransactionHistoryRepository(_dbContext);
			TemBankAdminProfileRepo = new TemBankAdminProfileRepository(_dbContext);
			TemCorporateCustomerRepo = new TemCorporateCustomerRespository(_dbContext);
			TemCorporateProfileRepo = new TemCorporateProfileRepository(_dbContext);
			PasswordHistoryRepo = new PasswordHistoryRepository(_dbContext);
			TempWorkflowRepo = new TemWorkflowRepository(_dbContext);
			TempWorkflowHierarchyRepo = new TempWorkflowHierarchyRepository(_dbContext);
			NipsFeeChargeRepo = new NispFeeChargeRepository(_dbContext);
			BranchRepo = new BranchRepository(_dbContext);
			ChequeRequestRepo = new ChequeRequestRepository(_dbContext);
			TempChequeRequestRepo = new TempChequeRequestRepository(_dbContext);
			CorporateAggregationRepo = new CorporateAggregationRepository(_dbContext);
			AggregatedAccountRepo = new AggregatedAccountRepository(_dbContext);
			TempAggregatedAccountRepo = new TempAggregatedAccountRepository(_dbContext);
			TempCorporateAggregationRepo = new TempCorporateAggregationRepository(_dbContext);
		}

		public IBankProfileRepository BankProfileRepo { get; protected set; }
		public ITemBankAdminProfileRepository TemBankAdminProfileRepo { get; protected set; }
		public ITemCorporateCustomerRespository TemCorporateCustomerRepo { get; protected set; }
		public ITemCorporateProfileRepository TemCorporateProfileRepo { get; protected set; }
		public IUserAccessRepository UserAccessRepo { get; protected set; }
		public IUserRoleAccessRepository UserRoleAccessRepo { get; protected set; }
		public ICorporateProfileRepository CorporateProfileRepo { get; protected set; }
		public ICorporateCustomerRepository CorporateCustomerRepo { get; protected set; }
		public ICorporateUserRoleAccess CorporateUserRoleAccessRepo { get; protected set; }
		public IBankAuthenticationRepository BankAuthenticationRepo { get; protected set; }
		public ICustomerAuthenticationRepository CustomerAuthenticationRepo { get; protected set; }
		public IRoleRepository RoleRepo { get; protected set; }
		public ICorporateRoleRepository CorporateRoleRepo { get; protected set; }
		public IPasswordResetRepository PasswordResetRepo { get; protected set; }
		public IAuditTrialRepository AuditTrialRepo { get; protected set; }
		public ISecurityQuestionRespository SecurityQuestionRepo { get; protected set; }
		public IWorkFlowRepository WorkFlowRepo { get; protected set; }
		public IWorkflowHierarchyRepository WorkFlowHierarchyRepo { get; protected set; }
		public ITokenBlackCorporateRepository TokenBlackCorporateRepo { get; protected set; }
		public ILoginLogCorporateRepository LoginLogCorporate { get; protected set; }
		public IPendingCreditLogRepository PendingCreditLogRepo { get; protected set; }
		public IPendingTranLogRepository PendingTranLogRepo { get; protected set; }
		public ITransactionRepository TransactionRepo { get; protected set; }
		public ICorporateApprovalHistoryRepository CorporateApprovalHistoryRepo { get; protected set; }
		public INipBulkCreditLogRepository NipBulkCreditLogRepo { get; protected set; }
		public INipBulkTransferLogRespository NipBulkTransferLogRepo { get; protected set; }
		public ITokenBlackRepository TokenBlackRepo { get; protected set; }
		public IInterBankBeneficiaryRepository InterBankBeneficiaryRepo { get; protected set; }
		public IIntraBankBeneficiaryRepository IntraBankBeneficiaryRepo { get; protected set; }
		public ICorporateBulkApprovalHistoryRepository CorporateBulkApprovalHistoryRepo { get; protected set; }
		public ITransactionHistoryRepository TransactionHistoryRepo { get; protected set; }
		public IPasswordHistoryRepository PasswordHistoryRepo { get; protected set; }
		public ITempWorkflowRepository TempWorkflowRepo { get; protected set; }
		public ITempWorkflowHierarchyRepository TempWorkflowHierarchyRepo { get; protected set; }
		public INipsFeeChargeRepository NipsFeeChargeRepo { get; protected set; }
		public IBranchRepository BranchRepo { get; protected set; }
		public IChequeRequestRepository ChequeRequestRepo { get; protected set; }
		public ITempChequeRequestRepository TempChequeRequestRepo { get; protected set; }
		public ICorporateAggregationRepository CorporateAggregationRepo { get; protected set; }
		public IAggregatedAccountRepository AggregatedAccountRepo { get; protected set; }
		public ITempAggregatedAccountRepository TempAggregatedAccountRepo { get; protected set; }
		public ITempCorporateAggregationRepository TempCorporateAggregationRepo { get; protected set; }
		public int Complete()
		{
			return _dbContext.SaveChanges();
		}
		public void Dispose()
		{
			_dbContext.Dispose();
		}
	}
}