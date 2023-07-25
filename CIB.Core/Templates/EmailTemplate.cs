using System.IO;
using CIB.Core.Common;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Services.Email.Dto;

namespace CIB.Core.Templates
{
    public static class EmailTemplate
    {
        public static EmailRequestDto LoginMail(string receiverEmail, string fullName, string address)
        {
            var template = new EmailRequestDto
            {
                subject = $"Parallexbank Corporate Banking Login Notification",
                message = $"<p>Dear {fullName},</p>" + $"<p>We noticed a login into your account. Please contact your admin if this wasn't you." +
                $"<br/>" + $"<p>Got questions, reach out to us on support@parallexbank.com</p>",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com"
            };
            return template;
        }
        public static EmailRequestDto LoginCredentialMail(string receiverEmail, string fullName, string username, string password, string customerId, string authUrl)
        {
            var isPasswordNeed = password != "" ? $"<p>Your password is {password}</p><br/>" : "";
            var template = new EmailRequestDto
            {
                subject = $"Parallexbank Corporate Banking Login Credentials",
                message = $"<p>Dear {fullName},</p>" +
                $"<p>Your customer id is {customerId}</p><br/>" +
                $"<p>Your username is {username} " +
                $"{isPasswordNeed}" +
                $"<a href='{authUrl}'><strong>Click To Login</strong></a> <br/>" +
                $"<br/>" +
                $"<p>Got questions, reach out to us on support@parallexbank.com</p>",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com"
            };
            return template;
        }
        public static EmailRequestDto LoginCredentialAdminMail(string receiverEmail, string fullName, string username, string password, string customerId)
        {
            var template = new EmailRequestDto
            {
                subject = $"Parallexbank Corporate Banking Login Credentials",
                message = $"<p>Dear {fullName},</p>" +
                $"<p>Your username is {username} " +
                $"<p>Kindly Login using your Active Directory Credentials" +
                $"<br/>" +
                $"<p>Got questions, reach out to us on support@parallexbank.com</p>",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com"
            };
            return template;
        }
        public static EmailRequestDto ComposeMailForLogin(string receiverEmail, string fullName)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Login Notification",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message = $"<p>Dear {fullName},</p>" +
                $"<p>We noticed a login into your account. Please contact your admin if this wasn't you." +
                $"<br/>" +
                $"<p>Got questions, reach out to us on support@parallexbank.com</p>"
            };
            return template;
        }
        public static EmailRequestDto PasswordResetMail(string receiverEmail, string fullName, string code)
        {
            var template = new EmailRequestDto
            {
                subject = $"Parallexbank Corporate Banking Reset Password Code",
                sender = "e-statement@parallexbank.com",
                recipient = receiverEmail,
                message = $"<p>Dear {fullName},</p>" +
                $"<p>Your reset password code is {code} " +
                $"<br/>" +
                $"<p>Got questions, reach out to us on support@parallexbank.com</p>"
            };
            return template;
        }
        public static EmailRequestDto PasswordResetChangeMail(string receiverEmail, string fullName)
        {
            var template = new EmailRequestDto
            {
                subject = $"Parallexbank Corporate Banking Reset Password Success",
                sender = "e-statement@parallexbank.com",
                recipient = receiverEmail,
                message = $"<p>Dear {fullName},</p>" +
                $"<p>You have successfully reset your password" +
                $"<br/>" +
                $"<p>Got questions, reach out to us on support@parallexbank.com</p>"
            };
            return template;
        }
        public static EmailRequestDto PasswordChangeMail(string receiverEmail, string fullName)
        {
            var template = new EmailRequestDto
            {
                subject = $"Parallexbank Corporate Banking Password Change Success",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message = $"<p>Dear {fullName},</p>" +
                    $"<p>You have successfully changed your password" +
                    $"<br/>" +
                    $"<p>Got questions, reach out to us on support@parallexbank.com</p>"
            };
            return template;

        }
        public static EmailRequestDto ResetPasswordCredentialMail(string receiverEmail, string fullName, string password)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Login Credentials",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message = $"<p>Dear {fullName},</p>" +
                $"<p>A password reset has been initiated." +
                $"<p>Your new password is {password}</p><br/>" +
                $"<br/>" +
                $"<p>Got questions, reach out to us on support@parallexbank.com</p>"
            };
            return template;
        }
        public static EmailRequestDto RequestApproval(string receiverEmail, string fullName, string amount)
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
                                $"<p> A fund transfer of {amount} initiated by {fullName} requires your approval, please login to the parallex bank corporate internet banking to approve.<br /> </p>" +
                                $"<p> Thank you for banking with parallex bank  </p>" +
                            $"</body>" +
                            $"</html>"
            };
            return template;
        }

        public static EmailRequestDto DeclineTransferRequestApproval(string receiverEmail, EmailNotification profile, string amount)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Transfer Approval Request Decline",
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
                                $"<p> A fund transfer of {amount} initiated by {profile.FullName} has been decline<br /> </p>" +
                                $"<p> Reason {profile.Reason} </p>" +
                                $"<p> Thank you for banking with parallex bank  </p>" +
                            $"</body>" +
                            $"</html>"
            };
            return template;
        }


        public static EmailRequestDto Transfer(string receiverEmail, EmailNotification profile, string amount)
        {
            string body = "";
            if (profile.Action == nameof(AuthorizationStatus.Decline))
            {
                body = $"<p> A fund transfer of {amount} initiated by {profile.FullName} has been decline<br /> </p>" +
                        $"<p> Reason {profile.Reason} </p>";
            }

            if (profile.Action == nameof(AuthorizationStatus.Approved))
            {
                body = $"<p> A fund transfer of {amount} initiated by {profile.FullName} requires your approval, please login to the parallex bank corporate internet banking to approve.<br /> </p>";
            }

            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Transfer Approval Request Decline",
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
                        $"{body}" +
                        $"<p> Thank you for banking with parallex bank  </p>" +
                    $"</body>" +
                    $"</html>"
            };
            return template;
        }




        public static EmailRequestDto BankAdminApprovalRequest(string receiverEmail, string fullName, string Role, string Message)
        {

            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>Kindly Approval Pending Request for  {fullName}, Role: {Role} </p>" +
                    $"<p>Customer Id  {fullName} </p>" +
                    $"<p>First Name: </p>" +
                    $"<p>Last Name:  </p>" +
                    $"<p>Middle Name </p>" +
                    $"<p>Approval Limit </p>" +
                    $"<p></p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto BankApprovalRequest(string receiverEmail, EmailNotification profile, string Role, string Message)
        {
            var userRole = profile.Role == "" ? "" : $"<p>Role {profile.Role}</p>";

            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Approval Request to {profile.Action} Corporate Profile",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                $"<meta charset='utf-8' />" +
                $"<title></title>" +
                $"</head>" +
                $"<body>" +
                $"<p>Dear Sir/Madam,</p>" +
                $"<p>Kindly Approved Pending Request to {profile.Action} Corporate Profile </p>" +
                $"<p>Customer Id  {profile.CustomerId} </p>" +
                $"<p>First Name: {profile.FirstName}</p>" +
                $"<p>Last Name:  {profile.LastName}</p>" +
                $"<p>Middle Name {profile.MiddleName}</p>" +
                $"<p>Email {profile.Email}</p>" +
                $"<p>Phone Number {profile.PhoneNumber}</p>" +
                $"<p>Approval Limit {profile.ApprovalLimit}</p>" +
                $"{userRole}" +
                $"<p></p>" +
                $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto BankProfileApprovalRequest(string receiverEmail, string Action, EmailNotification Profile, string Message)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>Kindly Approved Pending Request for {Action} </p>" +
                    $"<p> Name {Profile.FullName}, Email {Profile.Email}, PhoneNuber {Profile.PhoneNumber}, Role {Profile.Role} has been decline</p>" +
                    $"<p>Reason:  {Message} </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto CorporateProfileApprovalRequest(string receiverEmail, string fullName, string Role, string Message)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>Kindly Approval Pending Request for {fullName}, Role: {Role} </p>" +
                    $"<p>Reason:  {Message} </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto CorporateCustomerApprovalRequest(string receiverEmail, string fullName, string Role, string Message)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>Kindly Approval Pending Request for {fullName}, Role: {Role} </p>" +
                    $"<p>Reason:  {Message} </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }
        public static EmailRequestDto BankAdminDeclineRequest(string receiverEmail, string Action, string Profile, string Message)
        {

            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Request Decline",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p> Your Request To {Action} with customer Id {Profile} has been decline</p>" +
                    $"<p>Reason:  {Message} </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto DeclineBankProfilRequest(string receiverEmail, string Action, EmailNotification Profile, string Message)
        {

            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Request Decline",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p> Your Request To {Action} a bank profile</p>" +
                    $"<p> Name {Profile.FullName}, Email {Profile.Email}, PhoneNuber {Profile.PhoneNumber}, Role {Profile.Role} has been decline</p>" +
                    $"<p> Reason:  {Message} </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto DeclineCorporateProfilRequest(string receiverEmail, string Action, EmailNotification Profile, string Message)
        {

            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Request Decline",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p> Your Request To {Action} Corporate profile has been decline</p>" +
                    $"<p> Customer Id {Profile.CustomerId}, FullName {Profile.FullName} , Email {Profile.Email}, PhoneNuber {Profile.PhoneNumber}, Role {Profile.Role}.</p>" +
                    $"<p> Reason:  {Message} </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto DeclineCorporateCustomerRequest(string receiverEmail, string Action, EmailNotification Profile, string Message)
        {

            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Request Decline",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p> Your Request To {Action} Corporate profile</p>" +
                    $"<p> Customer Id {Profile.CustomerId}, Email {Profile.Email}, Account Number {Profile.AccountNumber}, Account Name {Profile.AccountName} has been decline</p>" +
                    $"<p> Reason:  {Message} </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto NewCorporateCustomerApprovalRequest(string receiverEmail, string Company, string CustomerId)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>Kindly Approval Pending Request for New Onboarder Corporate Customer </p>" +
                    $"<p>Company Name: {Company} </p>" +
                    $"<p>Customer Id: {CustomerId}</p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto CorporateAdminApprovalRequest(string receiverEmail, string fullName, string Role)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>Kindly Approval Pending Request for {fullName}, Role: {Role} </p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }


        public static EmailRequestDto WorkflowApprovalRequest(string receiverEmail, EmailNotification Profile, bool isUpdate = false)
        {

            var actionMessage = isUpdate == true ? "for a newly created workflow " : " for updated  workflow";
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Workflow Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>Kindly Approved Pending Request {actionMessage} </p>" +
                    $"<p>Company Name: {Profile.CompanyName}, Customer Id: {Profile.CustomerId}</p>" +
                    $"<p>Workflow Name: {Profile.WorkflowName}</p>" +
                    $"<p>Approval Limit: {Profile.ApprovalLimit}</p>" +
                    $"<p>No Of Authorizers: {Profile.NoOfAuthorizers}</p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto ProfileApprovalRequest(string receiverEmail, EmailNotification Profile, string Action)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Workflow Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>Kindly Approved Profile {Action} Pending Request </p>" +
                    $"<p>Company Name: {Profile.CompanyName}, Customer Id: {Profile.CustomerId}</p>" +
                    $"<p>Corporate Profile: {Profile.FullName}</p>" +
                    $"<p>Email {Profile.Email}</p>" +
                    $"<p>Phone Number {Profile.PhoneNumber}</p>" +
                    $"<p>Role {Profile.Role}</p>" +
                    $"<p>ApprovalLimit: {Profile.ApprovalLimit}</p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        public static EmailRequestDto CorporateCompanyApprovalRequest(string receiverEmail, EmailNotification Profile, string Action)
        {
            var template = new EmailRequestDto
            {
                subject = $"parallexbank Corporate Banking Workflow Approval Request",
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com",
                message =
                $"<!DOCTYPE html>" +
                $" <html>" +
                $"<head>" +
                    $"<meta charset='utf-8' />" +
                    $"<title></title>" +
                $"</head>" +
                $"<body>" +
                    $"<p>Dear Sir/Madam,</p>" +
                    $"<p>Kindly Approved {Action} Request for Corporate Customer </p>" +
                    $"<p>Company Name: {Profile.CompanyName}, Customer Id: {Profile.CustomerId}</p>" +
                    $"<p>MinAccountLimit: {Profile.MinAccountLimit}</p>" +
                    $"<p>MaxAccountLimit {Profile.MaxAccountLimit}</p>" +
                    $"<p>SingleTransDailyLimit {Profile.SingleTransDailyLimit}</p>" +
                    $"<p>BulkTransDailyLimit {Profile.BulkTransDailyLimit}</p>" +
                    $"<p>ApprovalLimit: {Profile.ApprovalLimit}</p>" +
                    $"<p> Thank you for banking with parallex bank  </p>" +
                $"</body>" +
                $"</html>"
            };
            return template;
        }

        // public static EmailRequestDto TestMail(string receiverEmail, EmailNotification Profile, string Action)
        public static EmailRequestDto LoginMailo(string receiverEmail, string fullName, string filePath)
        {
            string body = string.Empty;
            //using streamreader for reading my htmltemplate  
            //string AssemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString();
            // var path = "./htmlTemplate/CustomerLogin.html";
            using (StreamReader reader = new(filePath))
            {
                body = reader.ReadToEnd();

            }
            body = body.Replace("[Name]", fullName); //replacing the required things  
                                                     //return body;
            var template = new EmailRequestDto
            {
                subject = $"Parallexbank Corporate Banking Login Notification",
                message = body,
                recipient = receiverEmail,
                sender = "e-statement@parallexbank.com"
            };
            return template;

        }



    }
}
