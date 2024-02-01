namespace CIB.Core.Enums
{
    public enum TransactionType
    {
        /// <summary>
        /// Salary
        /// </summary>
        Salary,

        /// <summary>
        /// Bulk Transfer
        /// </summary>
        Bulk_Transfer,

        /// <summary>
        /// Intra Bank Transfer
        /// </summary>
        Intra_Bank_Transfer,

        /// <summary>
        /// Inter Bank Transfer
        /// </summary>
        Inter_Bank_Transfer,

        /// <summary>
        /// Own Transfer
        /// </summary>
        Own_Transfer,
         /// <summary>
        /// OnLending
        /// </summary>
        OnLending
    }

    public enum TransactionStatus
    {
        /// <summary>
        /// Salary
        /// </summary>
        Failed,

        /// <summary>
        /// Bulk Transfer
        /// </summary>
        Successful
    }

    public enum CreditStatus
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending = 0,
        /// <summary>
        /// Successful
        /// </summary>
        Successful,
        /// <summary>
        /// Failed
        /// </summary>
        Failed,
         /// <summary>
        /// Decline
        /// </summary>
        Decline
    }
}