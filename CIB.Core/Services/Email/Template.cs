using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Services.Email;

public static class Template
{
	public static readonly string CustomerLogin = "htmlTemplate/CustomerLogin.html";
	public static readonly string ResetPassword = "htmlTemplate/ResetPassword.html";
	public static readonly string ResendUserName = "htmlTemplate/ResendUserName.html";
	public static readonly string CustomerProfileOnbording = "htmlTemplate/CustomerProfileOnbording.html";
	public static readonly string ResendProfileDetails = "htmlTemplate/ResendProfileDetails.html";
	public static readonly string ResetPin = "htmlTemplate/ResetPin.html";
}

