namespace CIB.Core.Enums
{
    public enum AuditTrailAction
    {
        /// <summary>
        /// Login
        /// </summary>
        Login,
        /// <summary>
        /// Logout
        /// </summary>
        Logout,
        /// <summary>
        /// Password Reset
        /// </summary>
        Password_Reset,
        /// <summary>
        /// Create
        /// </summary>
        Create,
        /// <summary>
        /// Remove
        /// </summary>
        Remove,
        /// <summary>
        /// Update
        /// </summary>
        Update,
        /// <summary>
        /// Update
        /// </summary>
        Request_Approval,
        /// <summary>
        /// Approve
        /// </summary>
        Approve,
        /// <summary>
        /// Approve
        /// </summary>
        Initiate,
        /// <summary>
        /// Decline
        /// </summary>
        Decline,
        /// <summary>
        /// Deactivate
        /// </summary>
        Deactivate,
        /// <summary>
        /// Reactivate
        /// </summary>
        Reactivate,

        /// <summary>
        /// Onboard
        /// </summary>
        Onboard,
        /// <summary>
        /// Set Security Question
        /// </summary>
        Set_Security_Question,
        /// <summary>
        /// Change Password
        /// </summary>
        Change_Password,
        /// <summary>
        /// Intra Bank Transfer
        /// </summary>
        Intra_Bank_Transfer,
        /// <summary>
        /// Inter Bank Transfer
        /// </summary>
        Inter_Bank_Transfer,
        /// <summary>
        /// Bulk Bank Transfer
        /// </summary>
        Bulk_Bank_Transfer,
        /// <summary>
        /// Bulk Bank Transfer
        /// </summary>
        Bulk_Bank_Transfer_With_Duplicate,
         /// <summary>
        /// Bulk Customer Onboard
        /// </summary>
        Bulk_Customer_Onboard
    }

    public enum TempTableAction
    {
       
        /// <summary>
        /// Create
        /// </summary>
        Create,
        /// <summary>
        /// Update
        /// </summary>
        Update,
        /// <summary>
        /// Update
        /// </summary>
        Request_Approval,
        /// <summary>
        /// Approve
        /// </summary>
        Approve,
        /// <summary>
        /// Decline
        /// </summary>
        Decline,
        /// <summary>
        /// Deactivate
        /// </summary>
        Deactivate,
        /// <summary>
        /// Reactivate
        /// </summary>
        Reactivate,
        /// <summary>
        /// Onboard_Corporate_Customer
        /// </summary>
        Onboard_Corporate_Customer,
        /// <summary>
        /// Update_Role
        /// </summary>
        Update_Role,
        /// <summary>
        /// Update_Role
        /// </summary>
        Update_Account_limit,
        /// <summary>
        /// Update_Phone_Number
        /// </summary>
        Update_Phone_Number,
        /// <summary>
        /// Enable_Log_Out
        /// </summary>
        Enable_Log_Out
    }

}