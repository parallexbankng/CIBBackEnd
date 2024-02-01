using CIB.Core.Entities;
using CIB.Core.Enums;

namespace CIB.Core.Utils
{
    public class AccountStatus
    {
        public  StatusResponse CheckCorporateUserAccountStatus(TblCorporateProfile  profile){
            if(profile.Status != 1)
            {
                // if(profile.Status == 0 || profile.Status == null)
                // {
                //     return new StatusResponse(false, "Your account has not been approved yet");
                // }
                // if (profile.Status == 2)
                // {
                //     return new StatusResponse(false, "Your account has not been declined");
                // }
                if (profile.Status == -1)
                {
                    return new StatusResponse(false, "Your account has been suspended");
                }   
            }
            return new StatusResponse(true, "ok");
            
        }
        public  StatusResponse CheckCorporateCustomerAccountStatus(TblCorporateCustomer  profile){
            if (profile != null)
            {
                if (profile.Status == (int)ProfileStatus.Deactivated )
                {
                    return new StatusResponse(false, "Your organisation has been active");
                }
            }
            return new StatusResponse(true, "ok");
        }
        public  StatusResponse CheckAdminAccountStatus(TblBankProfile  profile)
        {
            if(profile.Status != 1)
            {
                // if(profile.Status == 0 || profile.Status == null)
                // {
                //     return new StatusResponse(false, "Your account has not been approved yet");
                // }
                // if (profile.Status == 2)
                // {
                //     return new StatusResponse(false, "Your account has not been declined");
                // }
                if (profile.Status == -1)
                {
                    return new StatusResponse(false, "Your account has been suspended");
                }   
            }
            return new StatusResponse(true, "ok");
        }
    }

    public class StatusResponse 
    {
        public bool Status;
        public string Message;
        public StatusResponse(bool status, string message){
            Status = status;
            Message = message;
        }
    }
}