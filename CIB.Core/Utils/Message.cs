using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Utils
{
  public static class Message
  {
    public static string Success = "Request Successful";
    public static string NotFound = "Resource Not Found";
    public static string InvalidId = "Invalid Id";
    public static string NotAuthenticated = "User is not authenticated";
    public static string DeclineReason = "Reason for declining is required";
    public static string UnAuthorized = "UnAuthorized Access";
    public static string ProfileDactivated = "Profile is already de-activated";
    public static string ProfileActivated = "Profile is activated Successful";
    public static string ProfileAlreadyActivated = "Profile is already active";
    public static string ProfileNotActivated = "Profile was not deactivated";
    public static string ProfileAwaitingApproval = "Profile is not awaiting approval and cannot be declined";
    public static string ProfileDeactivationReason = "Reason for de-activating profile is required";
    public static string ServerError = "An unknown error was encountered. Please try again."; 
    public static string ApiError = "Api Service Is Down. Please try again."; 
  }
  
  public static class ResponseCode
  {
    public static string SUCCESS = "00";
    public static string INVALID_ATTEMPT = "10";
    public static string NOT_PROFILE = "11";
    public static string NOT_APPROVED_PROFILE  = "12";
    public static string DECLINE_PROFILE = "13";
    public static string DEACTIVATED_PROFILE = "14";
    public static string DUPLICATE_VALUE = "15";
    public static string SECURITY_QUESTION = "16";
    public static string SERVER_ERROR = "17";
    public static string INACTIVE_ACCOUNT = "18";
    public static string API_ERROR = "19";
    public static string Aunthorized = "20";
    public static string NotAuthenticated = "21";
  }
}