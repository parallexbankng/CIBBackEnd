using CIB.Core.Common;
using CIB.Core.Enums;
using CIB.Core.Services.Email.Dto;

namespace CIB.Core.Templates.Corporate.workflow
{
    public static class WorkflowTemplate
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
                    message = Create(notify,"Kindly Approval Request for Newly Onboarded Corporate Profile")
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
                    message = WorkflowUpdate(notify,"Kindly Approval Pending Request for Corporate Profile Update")
                };
                return declineTemplate;
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
                    message = Create(notify,"Your Request to approve Newly Onboarded Corporate Profile has been decline")
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
                    message = WorkflowUpdate(notify,"Your Request to approve Corporate Profile Update has been decline")
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
                    message = Create(notify,"Your Request to approve Corporate Profile Reactivation has been decline")
                };
                return declineTemplate;
            }
            return  new EmailRequestDto();
        }
        public static string Create(EmailNotification notify, string headLine)
        {
            var  message = 
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
                    $"<p>Workflow Name: {notify.WorkflowName}</p>" +
                    $"<p>Approval Limit: {notify.ApprovalLimit}</p>" +
                    $"<p>No Of Authorizers: {notify.NoOfAuthorizers}</p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>";
          return message;
        }
        public static string WorkflowUpdate(EmailNotification notify,string headLine)
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
                    $"<p>Workflow Name: {notify.WorkflowName}</p>" +
                    $"<p>Approval Limit: {notify.ApprovalLimit}</p>" +
                    $"<p>No Of Authorizers: {notify.NoOfAuthorizers}</p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>";
            return message;
        }
   
    }
}