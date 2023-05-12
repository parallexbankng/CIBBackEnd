using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;
using CIB.Core.Enums;
using CIB.Core.Services.Email.Dto;

namespace CIB.Core.Templates.Admin.BankUser
{
    public static class BankTemplate
    {
        public static EmailRequestDto ApprovalRequest(string receiverEmail,EmailNotification notify)
        {
            if(notify.Action == nameof(TempTableAction.Create).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Request Approval for Corporate Profile",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = Onboarding(notify,"Kindly Approval Request for Newly Onboarded Corporate Profile")
                };
                return declineTemplate;
            }
            if(notify.Action == nameof(TempTableAction.Update).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Request Approval for Corporate Profile Update ",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = Onboarding(notify,"Kindly Approval Pending Request for Corporate Profile Update")
                };
                return declineTemplate;
            }
            if(notify.Action == nameof(TempTableAction.Update_Role).Replace("_", " "))
            {
                var template = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Request Approval for Corporate Profile Role Change ",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = RoleUpdate(notify,"Kindly Approval Pending Request for Corporate Profile Role Change")
                };
                return template;
            }
            return  new EmailRequestDto();
        }
        public static EmailRequestDto DeclineRequest(string receiverEmail,EmailNotification notify)
        {
            if(notify.Action == nameof(TempTableAction.Create).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate Profile Onboarded",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = Onboarding(notify,"Your Request to approve Newly Onboarded Corporate Profile has been decline")
                };
                return declineTemplate;
            }
            if(notify.Action == nameof(TempTableAction.Update).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate Profile Update",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = Onboarding(notify,"Your Request to approve Corporate Profile Update has been decline")
                };
                return declineTemplate;
            }
            if(notify.Action == nameof(TempTableAction.Update_Role).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate profile role change",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = RoleUpdate(notify,"Your Request to approve corporate profile role change, has been decline")
                };
                return declineTemplate;
            }
            if(notify.Action == nameof(TempTableAction.Enable_Log_Out).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate profile log out enable",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = Onboarding(notify,"Your Request to approve Corporate profile log out enable has been decline")
                };
                return declineTemplate;
            }
            if(notify.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate Profile Reactivation",
                    recipient = receiverEmail,
                    sender = "e-statement@parallexbank.com",
                    message = Onboarding(notify,"Your Request to approve Corporate Profile Reactivation has been decline")
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
                $"<p>FirstName: {notify.FullName}</p>" +
                $"<p>LastName: {notify.FullName}</p>" +
                $"<p>MiddleName: {notify.FullName}</p>" +
                $"<p>Email {notify.Email}</p>" +
                $"<p>Phone Number {notify.PhoneNumber}</p>" +
                $"<p>Role {notify.Role}</p>" +
                $"<p> Thank you for banking with parallex bank  </p>" +
            $"</body>" +
            $"</html>";
            return message;
        }
    
        public static string RoleUpdate(EmailNotification notify,string headLine)
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
                $"<p>FirstName: {notify.FullName}</p>" +
                $"<p>LastName: {notify.FullName}</p>" +
                $"<p>MiddleName: {notify.FullName}</p>" +
                $"<p>Email {notify.Email}</p>" +
                $"<p>Phone Number {notify.PhoneNumber}</p>" +
                $"<p>Previuos Role {notify.PreviousRole}, New Role  {notify.Role}</p>" +
                $"<p> Thank you for banking with parallex bank  </p>" +
            $"</body>" +
            $"</html>";
            return message;
        }

    }
}