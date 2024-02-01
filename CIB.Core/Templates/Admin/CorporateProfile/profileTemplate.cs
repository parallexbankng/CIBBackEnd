using CIB.Core.Common;
using CIB.Core.Enums;
using CIB.Core.Services.Email.Dto;

namespace CIB.Core.Templates.Admin.CorporateProfile
{
	public static class profileTemplate
	{
		public static EmailRequestDto ApprovalRequest(string receiverEmail, EmailNotification notify)
		{
			if (notify.Action == nameof(TempTableAction.Create).Replace("_", " "))
			{
				var declineTemplate = new EmailRequestDto
				{
					subject = $"parallexbank Corporate Banking Request Approval for Corporate Profile",
					recipient = receiverEmail,
					sender = "no-reply@parallexbank.com",
					message = Onboarding(notify, "Kindly Approval Request for Newly Onboarded Corporate Profile")
				};
				return declineTemplate;
			}
			if (notify.Action == nameof(TempTableAction.Update).Replace("_", " "))
			{
				var declineTemplate = new EmailRequestDto
				{
					subject = $"parallexbank Corporate Banking Request Approval for Corporate Profile Update ",
					recipient = receiverEmail,
					sender = "no-reply@parallexbank.com",
					message = Onboarding(notify, "Kindly Approval Pending Request for Corporate Profile Update")
				};
				return declineTemplate;
			}
			if (notify.Action == nameof(TempTableAction.Update_Role).Replace("_", " "))
			{
				var template = new EmailRequestDto
				{
					subject = $"parallexbank Corporate Banking Request Approval for Corporate Profile Role Change ",
					recipient = receiverEmail,
					sender = "no-reply@parallexbank.com",
					message = RoleUpdate(notify, "Kindly Approval Pending Request for Corporate Profile Role Change")
				};
				return template;
			}
			if (notify.Action == nameof(TempTableAction.Update_User_Name).Replace("_", " "))
			{
				var template = new EmailRequestDto
				{
					subject = $"parallexbank Corporate Banking Request Approval for Corporate Profile Role Change ",
					recipient = receiverEmail,
					sender = "no-reply@parallexbank.com",
					message = UserNameUpdate(notify, "Kindly Approval Pending Request for Corporate Profile User Name Change")
				};
				return template;
			}
			if (notify.Action == nameof(TempTableAction.Pin_Reset).Replace("_", " "))
			{
				var template = new EmailRequestDto
				{
					subject = $"parallexbank Corporate Banking Request Approval for Corporate Pin Reset ",
					recipient = receiverEmail,
					sender = "no-reply@parallexbank.com",
					message = ResetDetail(notify, "Kindly Approval Pending Request for Corporate Profile Pin Reset")
				};
				return template;
			}
			if (notify.Action == nameof(TempTableAction.Password_Reset).Replace("_", " "))
			{
				var template = new EmailRequestDto
				{
					subject = $"parallexbank Corporate Banking Request Approval for Corporate Password Reset ",
					recipient = receiverEmail,
					sender = "no-reply@parallexbank.com",
					message = ResetDetail(notify, "Kindly Approval Pending Request for Corporate Profile Password Reset")
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
					subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate Profile Onboarded",
					recipient = receiverEmail,
					sender = "no-reply@parallexbank.com",
					message = Onboarding(notify, "Your Request to approve Newly Onboarded Corporate Profile has been decline")
				};
				return declineTemplate;
			}
			if (notify.Action == nameof(TempTableAction.Update).Replace("_", " "))
			{
				var declineTemplate = new EmailRequestDto
				{
					subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate Profile Update",
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
			if (notify.Action == nameof(TempTableAction.Pin_Reset).Replace("_", " "))
			{
				var declineTemplate = new EmailRequestDto
				{
					subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate Profile Pin Reset",
					recipient = receiverEmail,
					sender = "no-reply@parallexbank.com",
					message = Onboarding(notify, "Your Request to approve Corporate Profile Pin Reset has been decline")
				};
				return declineTemplate;
			}
			if (notify.Action == nameof(TempTableAction.Password_Reset).Replace("_", " "))
			{
				var declineTemplate = new EmailRequestDto
				{
					subject = $"parallexbank Corporate Banking Approval Request Decline for Corporate Profile Password Reset",
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
		public static string ResetDetail(EmailNotification notify, string headLine)
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
							$"<p> Thank you for banking with parallex bank  </p>" +
					$"</body>" +
					$"</html>";
			return message;
		}
		public static string UserNameUpdate(EmailNotification notify, string headLine)
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
							$"<p>Previous UserName: {notify.PreviouseUserName}, New UserName: {notify.UserName}</p>" +
							$"<p>Customer Id: {notify.CustomerId}, Company Name: {notify.CompanyName} </p>" +
							$"<p>First Name: {notify.FirstName}</p>" +
							$"<p>Last Name: {notify.LastName}</p>" +
							$"<p>Middle Name: {notify.MiddleName}</p>" +
							$"<p>Email: {notify.Email}</p>" +
							$"<p>Phone Number: {notify.PhoneNumber}</p>" +
							$"<p>Approval Limit: {notify.ApprovalLimit}</p>" +
							$"<p> Thank you for banking with parallex bank  </p>" +
					$"</body>" +
					$"</html>";
			return message;
		}
	}
}