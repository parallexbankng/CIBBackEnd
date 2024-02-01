namespace CIB.Core.Enums
{
    public enum AuthorizationType
    {
        /// <summary>
        /// Single Signatory
        /// </summary>
        Single_Signatory,
        /// <summary>
        /// Multiple Signatory
        /// </summary>
        Multiple_Signatory
    }
    public enum SpecialFeature
    {
        /// <summary>
        /// OnLending
        /// </summary>
        OnLending
    }
    public enum AuthorizationStatus
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending,

        /// <summary>
        /// Approved
        /// </summary>
        Approved,

        /// <summary>
        /// Decline
        /// </summary>
        Decline,
        /// <summary>
        ///Request_Approval
        /// </summary>
        Request_Approval
    }
}