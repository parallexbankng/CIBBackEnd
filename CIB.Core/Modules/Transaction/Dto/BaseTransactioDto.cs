

namespace CIB.Core.Modules.Transaction.Dto
{
    public class BaseTransactioDto
    {
        public string IPAddress { get; set; }
        public string ClientStaffIPAddress { get; set; }
        public string MACAddress { get; set; }
        public string HostName { get; set; }
    }

    public class ValidationStatus
    {
        public bool Status { get; set; }
        public string Message { get; set; }
    }

}