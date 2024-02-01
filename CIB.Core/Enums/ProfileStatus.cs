namespace CIB.Core.Enums
{
    public enum ProfileStatus
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending,

        /// <summary>
        /// Active
        /// </summary>
        Active,

        /// <summary>
        /// Modified
        /// </summary>
        Modified,

        /// <summary>
        /// Declined
        /// </summary>
        Declined,

        /// <summary>
        /// Deactivated
        /// </summary>
        Deactivated = -1
    }

    public enum RequestAction
    {
        /// <summary>
        /// Deactivated
        /// </summary>
        Deactivated,

        /// <summary>
        /// Activated
        /// </summary>
        Activated,

        /// <summary>
        /// Approved
        /// </summary>
        Approved,

        /// <summary>
        /// Modified
        /// </summary>
        Modified,
    }

    public enum UserRole
    {
        /// <summary>
        /// Deactivated
        /// </summary>
        Corporate_Admin,

        /// <summary>
        /// Activated
        /// </summary>
        Corporate_Maker,

        /// <summary>
        /// Approved
        /// </summary>
        Corporate_Authorizer,

        
    }
}