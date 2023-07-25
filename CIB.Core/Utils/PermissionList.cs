using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Utils
{
    public static class Permission
    {
        public static string ViewBankAdminProfile = "View Bank Admin Profile";
        public static string ViewCorporateUserProfile = "View Corporate User Profile";
        //public static string ViewAllProfile = "View All Profile";
        public static string CreateBankAdminProfile = "Create Bank Admin Profile";
        public static string CreateCorporateUserProfile = "Create Corporate User Profile";
        public static string CreateCorporateUserAdminProfile = "Create Corporate User Admin Profile";
        public static string UpdateBankAdminProfile = "Update Bank Admin Profile";
        public static string UpdateCorporateUserProfile = "Update Corporate User Profile";
        public static string ViewCorporateCustomer = "View Corporate Customer";
        public static string CreateCorporateCustomer = "Create Corporate Customer";
        public static string UpdateCorporateCustomer = "Update Corporate Customer";
        public static string ApproveCorporateCustomer = "Approve Corporate Customer";
        public static string DeclineCorporateCustomer = "Decline Corporate Customer";
        public static string DeactivateCorporateCustomer = "Deactivate Corporate Customer";
        public static string RequestCorporateCustomerApproval = "Request Corporate Customer Approval";
        public static string ApproveBankAdminProfile = "Approve Bank Admin Profile";
        public static string ReActivateBankAdminProfile = "ReActivate Bank Admin Profile";
        public static string DeclineBankAdminProfile = "Decline Bank Admin Profile";
        public static string DeactivateBankAdminProfile = "Deactivate Bank Admin Profile";
        public static string RequestBankAdminProfileApproval = "Request Bank Admin Profile Approval";
        public static string ApproveCorporateUserProfile = "Approve Corporate User Profile";
        public static string ReActivateCorporateUserProfile = "ReActivate Corporate User Profile";
        public static string DeclineCorporateUserProfile = "Decline Corporate User Profile";
        public static string DeactivateCorporateUserProfile = "Deactivate Corporate User Profile";
        public static string RequestCorporateUserProfileApproval = "Request Corporate User Profile Approval";
        public static string UpdateBankAdminUserRole = "Update Bank Admin User Role";
        public static string UpdateCorporateUserRole = "Update Corporate User Role";
        public static string EnableLoggedOutCorporateUser = "Enable Logged Out Corporate User";
        public static string EnableLoggedOutBankAdminUser = "Enable Logged Out Bank Admin User";
        public static string UpdateCorporateCustomerAccountLimit = "Update Corporate Customer Account Limit";
        public static string SetCorporateCustomerAccountLimit = "Set Corporate Customer Account Limit";
        public static string OnboardCorporateCustomer = "Onboard Corporate Customer";
        public static string ViewCorporateRole = "View Corporate Role";
        public static string UpdateCorporateRole = "Update Corporate Role";
        public static string CreateCorporateRole = "Create Corporate Role";
        public static string ViewRole = "View Role";
        public static string UpdateRole = "Update Role";
        public static string CreateRole = "Create Role";
        public static string ViewWorkflow = "View Workflow";
        public static string DeleteWorkflowHierarchy = "Delete Workflow Hierarchy";
        public static string CreateWorkflow = "Create Workflow";
        public static string UpdateWorkflow = "Update Workflow";
        public static string ApproveWorkflow = "Approve Workflow";
        public static string DeclineWorkflow = "Decline Workflow";
        public static string RequestWorkflowApproval = "Request Workflow Approval";
        public static string ViewSector = "View Sector";
        public static string CreateSector = "Create Sector";
        public static string UpdateSector = "Update Sector";
        public static string ViewWorkflowByCorporateAdmin = "View Workflow By Corporate Admin";
        public static string CreateWorkflowByCorporateAdmin = "Create Workflow By Corporate Admin";
        public static string UpdateWorkflowByCorporateAdmin = "Update Workflow By Corporate Admin";
        public static string ApproveWorkflowByCorporateAdmin = "Approve Workflow By Corporate Admin";
        public static string DeclineWorkflowByCorporateAdmin = "Decline Workflow By Corporate Admin";
        public static string RequestWorkflowApprovalByCorporateAdmin = "Request Workflow Approval";
        public static string ChangePassword = "Change Password";
        public static string ResetUserPassword = "Reset User Password";
        public static string ResetCorporateUserPassword = "Reset Corporate User Password";


        public static string RequestRoleApproval = "Request Role Approval";
        public static string ApproveRole = "Approve Role";
        public static string ActivateRole = "Activate Role";
        public static string DeclineRole = "Decline Role";
        public static string DeactivateRole = "Deactivate Role";
        public static string ViewRolePermissions = "View Role Permissions";
        public static string RequestCorporateRoleApproval = "Request Corporate Role Approval";
        public static string ApproveCorporateRole = "Approve Corporate Role";
        public static string ActivateCorporateRole = "Activate Corporate Role";
        public static string DeclineCorporateRole = "Decline Corporate Role";
        public static string DeactivateCorporateRole = "Deactivate Corporate Role";
        public static string ViewCorporateRolePermissions = "View Corporate Role Permissions";
        public static string AddRolePermissions = "Add Role Permissions"; 
        public static string AddCorporateRolePermissions = "Add Corporate Role Permissions";
        //public static string ViewUserAccesses = "View User Accesses";
        public static string ViewCorporateAccount = "View Corporate Account";
        public static string ViewTransactionHistory = "View Transaction History";
        public static string InitiateTransaction = "Initiate Transaction";
        public static string ApproveTransaction = "Approve Transaction";
        public static string ViewPendingTransaction = "View Pending Transaction";
        public static string SetCorporateUserAccess = "Set Corporate User Access";
        public static string CanCreateWorkflowByCorporateAdmin = "Can Create Workflow By Corporate Admin";
        public static string CanViewWorkflowByCorporateAdmin = "Can View Workflow By Corporate Admin";
        public static string CanUpdateWorkflowByCorporateAdmin = "Can Update Workflow By Corporate Admin";
        public static string CanRequestWorkflowApprovalByCorporateAdmin = "Update Workflow";
        public static string CanApproveWorkflowByCorporateAdmin = "Can Approve Workflow By Corporate Admin";
        public static string CanDeclineWorkflowByCorporateAdmin = "Can Decline Workflow By Corporate Admin";

        public static string CanCreateStaff = "Can Create Staff";
        public static string CanViewStaff = "Can View Staff";
        public static string CanApproveStaff = "Can Approve Staff";
        public static string CanDeactivateStaff = "Can Deactivate Staff";
        public static string CanReactivateStaff = "Can Reactivate Staff";
        public static string CanDeclineStaff = "Can Decline Staff";
        public static string CanRequestStaffApproval = "Request Staff Approval ";
        public static string CanCreateSchedule = "Can Create Schedule";
        public static string CanViewSchedule = "Can View Schedule";
        public static string CanInitiateSchedule = "Can Initiate Schedule";
        public static string CanApprovedSchedule = "Can Approved Schedule";
        public static string CanDeclineSchedule = "Can Decline Schedule";
        public static string CanDeactivateSchedule = "Can Deactivate Schedule";
        public static string CanReactivateSchedule = "Can Reactivate Schedule";
        public static string CanRequestScheduleApproval = "Can Request Schedule Approval";


        public static string CanCreateBeneficiary = "Can Create Schedule Beneficiary";
        public static string CanViewBeneficiary = "Can View Schedule Beneficiary";
        public static string CanRemoveBeneficiary = "Can Remove Schedule Beneficiary";
        public static string CanApprovedBeneficiary = "Can Approved Schedule Beneficiary";
        public static string CanDeclineBeneficiary = "Can Decline Schedule Beneficiary";
        public static string CanDeactivateBeneficiary = "Can Deactivate Schedule Beneficiary";
        public static string CanRequestBeneficiaryApproval = "Can Request Schedule Beneficiary Approval";

        public static string CanInitiateOnlendingPaymentDisbursement = "Can Initiate Onlending Payment Disbursement";
        public static string CanApproveOnlendingPaymentDisbursement = "Can Approve Onlending Payment Disbursement";
        public static string CanExtendOnlendingRepaymentData = "Can Extend Onlending Repayment Date";
        public static string CanDeclineOnlendingRepaymentDateExtension = "Can Decline Onlending Repayment Date Extension";
        public static string CanApproveOnlendingRepaymentDateExtension = "Can Approve Onlending Repayment Date Extension";
        public static string CanRequestForApprovalOnlendingRepaymentDateExtension = "Can Request For Approval Onlending Repayment Date Extension";
        public static string CanCreateOnlendingbeneficiary = "Can Create Onlending beneficiary";
        public static string CanApproveOnlendingbeneficiary = "Can Approve Onlending beneficiary";
        public static string CanRequestForApprovalofOnlendingbeneficiary = "Can Request For Approval of Onlending beneficiary";
        public static string CanDeclineOnlendingBeneficiary = "Can Decline Onlending beneficiary";
        public static string CanInitiateOnlendingLiquidation = "Can Initiate Onlending Liquidation";
        public static string ViewAuditTrail = "View Audit Trail";
    }
}