using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;
using CIB.Core.Services.Email.Dto;
using CIB.Core.Enums;

namespace CIB.Core.Templates.Admin.CorporateCustomer
{
    public static class CustomerTemplate
    {
        public static EmailRequestDto ApprovalRequest(string receiverEmail,EmailNotification notify)
        {
            if(notify.Action == nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Request Approval",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = Onboarding(notify,"Kindly Approval Pending Request for Newly Onboarded Corporate Customer")
                };
                return declineTemplate;
            }

            if(notify.Action == nameof(TempTableAction.Update_Account_limit).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Request Approval ",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = AccountLimitUpdate(notify,"Kindly Approval Pending Request for Account Limit Update for a Corporate Customer")
                };
                return declineTemplate;
            }
            
            return  new EmailRequestDto();
        }
        public static EmailRequestDto DeclineRequest(string receiverEmail,EmailNotification notify)
        {
            if(notify.Action == nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = Onboarding(notify,"Your Request to approve Newly Onboarded Corporate Customer has been decline")
                };
                return declineTemplate;
            }
            if(notify.Action == nameof(TempTableAction.Update_Account_limit).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = AccountLimitUpdate(notify,"Your Request to approve Account Limit Update for Corporate Customer has been decline")
                };
                return declineTemplate;
            }
            if(notify.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = AccountLimitUpdate(notify,"Your Request to approve Corporate Customer Reactivation has been decline")
                };
                return declineTemplate;
            }
            
            return  new EmailRequestDto();
        }
    
        public static string Onboarding(EmailNotification notify, string headLine)
        {
            var message =
            $"<!DOCTYPE html>" +
            $" <html>" +
            $"<head>" +
                $"<meta charset='utf-8' />" +
                $"<title></title>" +
            $"</head>" +
            $"<body>" +
                $"<p>Dear Sir/Madam,</p>" +
                $"<p>{headLine}</p>" +
                $"<p>Company Name: {notify.CompanyName} </p>" +
                $"<p>Customer Id: {notify.CustomerId}</p>" +
                $"<p> Thank you for banking with parallex bank  </p>" +
            $"</body>" +
            $"</html>";
            return message;
        }
        public static string AccountLimitUpdate(EmailNotification notify,string headLine)
        {
            var message = 
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>{headLine}</p>" +
                    $"<p>Company Name: {notify.CompanyName}, Customer Id: {notify.CustomerId}</p>" +
                    $"<p>MinAccountLimit: {notify.MinAccountLimit}</p>" +
                    $"<p>MaxAccountLimit {notify.MaxAccountLimit}</p>" +
                    $"<p>SingleTransDailyLimit {notify.SingleTransDailyLimit}</p>" +
                    $"<p>BulkTransDailyLimit {notify.BulkTransDailyLimit}</p>" +
                    $"<p>ApprovalLimit: {notify.ApprovalLimit}</p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>";
            return message;
        }

    }
}