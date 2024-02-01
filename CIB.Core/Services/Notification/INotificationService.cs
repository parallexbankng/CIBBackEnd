using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;
using CIB.Core.Entities;
using CIB.Core.Modules.BankAdminProfile.Dto;

namespace CIB.Core.Services.Notification
{
	public interface INotificationService
	{
		void NotifyBankAdminAuthorizer(TblTempBankProfile profile, bool isRole, string reason);
		void NotifyBankAdminAuthorizerForCorporate(TblTempCorporateProfile profile = null, TblCorporateCustomer customer = null, EmailNotification notify = null, bool isRole = false, string reason = null);
		void NotifyBankMaker(TblBankProfile user, string action, EmailNotification profile, string reason);
		void NotifyCorporateMaker(TblCorporateProfile user, string action, EmailNotification profile, string reason);
		void NotifyCorporateTransfer(TblCorporateProfile user = null, TblCorporateProfile approva = null, EmailNotification profile = null, string reason = null);
		List<TblBankProfile> GetBankAdminAuthorizer();
		List<TblBankProfile> GetSuperAdminAuthorizer();
		List<TblCorporateProfile> GetCorporateAuthorizer();
		void NotifyBankAdminAuthorizerNewCorporateCustomer(TblTempCorporateCustomer customer);

		void NotifyCorporateAuthorizer(TblCorporateProfile profile, Guid userId);
		void NotifyBankAdminAuthorizer(string action, TblTempBankProfile profile = null, TblTempWorkflow workflow = null, TblCorporateCustomer customer = null);
		void NotifyBankAuthorizerForCorporate(string action, TblTempCorporateProfile profile = null, TblCorporateCustomer customer = null, TblTempCorporateCustomer temCustomer = null, TblTempWorkflow workflow = null, string role = null);


		void NotifyBankAdminAuthorizerForCorporateCustomerApproval(TblTempCorporateCustomer customer, EmailNotification profile = null);
		void NotifyBankAdminAuthorizerForCorporateCustomerAggregationApproval(EmailNotification profile = null);

		void NotifyBankAdminAuthorizerForCorporateCustomerDecline(TblBankProfile customer, EmailNotification profile = null);

		void NotifyBankAdminAuthorizerForCorporateProfileApproval(EmailNotification profile = null);
		void NotifyBankAdminAuthorizerForCorporateProfileDecline(TblBankProfile profile, EmailNotification notify = null);

		void NotifyBankAdminAuthorizerForCorporateWorkflowApproval(EmailNotification profile = null);
		void NotifyBankAdminAuthorizerForCorporateWorkflowDecline(TblBankProfile profile, EmailNotification notify = null);

		void NotifySuperAdminBankAuthorizerForBankProfileApproval(EmailNotification profile = null);
		void NotifySuperAdminBankAuthorizerForBankProfileDecline(TblBankProfile profile, EmailNotification notify = null);

		void NotifyOnlendingAuthorizerApprovalRequest(EmailNotification profile = null, EmailNotification notify = null);
		void NotifyOnlendingInitiatorForDeclineRequest(TblBankProfile profile, EmailNotification notify = null);




	}
}
