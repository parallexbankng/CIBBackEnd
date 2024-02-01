using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;
using CIB.Core.Enums;
using CIB.Core.Services.Email.Dto;

namespace CIB.Core.Templates.Corporate.profile
{
    public static class Profile
    {
        public static EmailRequestDto ApprovalRequest(string receiverEmail, EmailNotification notify)
        {
            if (notify.Action == nameof(TempTableAction.Create).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank corporate banking request approval for corporate profile",
                    recipient = receiverEmail,
                    sender = "no-reply@parallexbank.com",
                    message = Onboarding(notify, "Kindly approval request for newly onboarded corporate profile")
                };
                return declineTemplate;
            }
            if (notify.Action == nameof(TempTableAction.Update).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank corporate banking request approval for corporate profile update ",
                    recipient = receiverEmail,
                    sender = "no-reply@parallexbank.com",
                    message = Onboarding(notify, "Kindly approval pending request for corporate profile update")
                };
                return declineTemplate;
            }
            if (notify.Action == nameof(TempTableAction.Update_Role).Replace("_", " "))
            {
                var template = new EmailRequestDto
                {
                    subject = $"parallexbank corporate banking request approval for corporate profile role change ",
                    recipient = receiverEmail,
                    sender = "no-reply@parallexbank.com",
                    message = RoleUpdate(notify, "Kindly approval pending request for corporate profile role change")
                };
                return template;
            }
            return new EmailRequestDto();
        }
        public static EmailRequestDto DeclineRequest(string receiverEmail, EmailNotification notify)
        {
            if (notify.Action == nameof(TempTableAction.Create).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank corporate banking approval request decline for corporate profile onboarded",
                    recipient = receiverEmail,
                    sender = "no-reply@parallexbank.com",
                    message = Onboarding(notify, "Your request to approve newly onboarded corporate profile has been decline")
                };
                return declineTemplate;
            }
            if (notify.Action == nameof(TempTableAction.Update).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank corporate banking approval request decline for corporate profile update",
                    recipient = receiverEmail,
                    sender = "no-reply@parallexbank.com",
                    message = Onboarding(notify, "Your Request to approve Corporate Profile Update has been decline")
                };
                return declineTemplate;
            }
            if (notify.Action == nameof(TempTableAction.Update_Role).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate profile role change",
                    recipient = receiverEmail,
                    sender = "no-reply@parallexbank.com",
                    message = RoleUpdate(notify, "Your Request to approve corporate profile role change, has been decline")
                };
                return declineTemplate;
            }
            if (notify.Action == nameof(TempTableAction.Enable_Log_Out).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate profile log out enable",
                    recipient = receiverEmail,
                    sender = "no-reply@parallexbank.com",
                    message = Onboarding(notify, "Your Request to approve Corporate profile log out enable has been decline")
                };
                return declineTemplate;
            }
            if (notify.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
            {
                var declineTemplate = new EmailRequestDto
                {
                    subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate Profile Reactivation",
                    recipient = receiverEmail,
                    sender = "no-reply@parallexbank.com",
                    message = Onboarding(notify, "Your Request to approve Corporate Profile Reactivation has been decline")
                };
                return declineTemplate;
            }
            return new EmailRequestDto();
        }
        public static string Onboarding(EmailNotification notify, string headLine)
        {
            var userRole = notify.Role == "" ? "" : $"<p>Role {notify.Role}</p>";
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
              $"<p>Customer Id: {notify.CustomerId} </p>" +
              $"<p>First Name: {notify.FirstName}</p>" +
              $"<p>Last Name: {notify.LastName}</p>" +
              $"<p>Middle Name: {notify.MiddleName}</p>" +
              $"<p>Email: {notify.Email}</p>" +
              $"<p>Phone Number: {notify.PhoneNumber}</p>" +
              $"<p>Approval Limit: {notify.ApprovalLimit}</p>" +
              $"{userRole}" +
              $"<p></p>" +
              $"<p> Thank you for banking with parallex bank  </p>" +
              $"</body>" +
              $"</html>";
            return message;
        }
        public static string RoleUpdate(EmailNotification notify, string headLine)
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
                $"<p>Customer Id: {notify.CustomerId}, Company Name: {notify.CompanyName} </p>" +
                $"<p>First Name: {notify.FirstName}</p>" +
                $"<p>Last Name: {notify.LastName}</p>" +
                $"<p>Middle Name: {notify.MiddleName}</p>" +
                $"<p>Email: {notify.Email}</p>" +
                $"<p>Phone Number: {notify.PhoneNumber}</p>" +
                $"<p>Approval Limit: {notify.ApprovalLimit}</p>" +
                $"<p>Previous Role: {notify.PreviousRole}, New Role: {notify.Role}</p>" +
                $"<p> Thank you for banking with parallex bank  </p>" +
            $"</body>" +
            $"</html>";
            return message;
        }
    }
}