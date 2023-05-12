using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;
using CIB.Core.Services.Email.Dto;

namespace CIB.Core.Templates.Corporate.transfer
{
    public static class Transfer
    {
        public static EmailRequestDto ApprovalRequest(string receiverEmail, EmailNotification notify)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message = $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p> A fund transfer of {notify.Amount} initiated by {notify.FullName} requires your approval, please login to the parallex bank corporate internet banking to approve.<br /> </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }
        public static EmailRequestDto DeclineApproval(string receiverEmail, EmailNotification notify)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message = $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p> A fund transfer of {notify.Amount} initiated by {notify.FullName} requires your approval, please login to the parallex bank corporate internet banking to approve.<br /> </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }
    }
}