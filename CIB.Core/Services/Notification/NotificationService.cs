using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CIB.Core.Common;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.BankAdminProfile.Dto;
using CIB.Core.Services.Email;
using CIB.Core.Templates;
using CIB.Core.Templates.Admin.BankUser;
using CIB.Core.Templates.Admin.CorporateCustomer;
using CIB.Core.Templates.Admin.CorporateProfile;
using CIB.Core.Templates.Admin.Workflow;
using CIB.Core.Templates.Corporate.transfer;

namespace CIB.Core.Services.Notification
{
  public class NotificationService : INotificationService
  {
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IEmailService emailService,IUnitOfWork unitOfWork)
    {
      this._emailService = emailService;
      this._unitOfWork = unitOfWork;
    }

    public void NotifyBankAdminAuthorizer(TblTempBankProfile profile,bool IsRole, string Message)
    {
      var bankAuthorization = this.GetSuperAdminAuthorizer();
      var userRole = _unitOfWork.RoleRepo.GetByIdAsync(Guid.Parse(profile.UserRoles));
      foreach(var bank in bankAuthorization)
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.BankAdminApprovalRequest(bank.Email,profile.FullName,userRole.RoleName,Message)));
      }
       
    }

    public void NotifyBankAdminAuthorizerNewCorporateCustomer(TblTempCorporateCustomer customer)
    {
        var bankAuthorization = this.GetBankAdminAuthorizer();
        foreach(var bank in bankAuthorization){
          ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.NewCorporateCustomerApprovalRequest(bank.Email,customer.CompanyName,customer.CustomerId)));
        }
    }
    public void NotifyCorporateAuthorizer(TblCorporateProfile profile, Guid UserId)
    {
      var corporateAuthorization = this.GetCorporateAuthorizer();
      //var profile = _unitOfWork.BankProfileRepo.GetByIdAsync(UserId);
      var userRole = _unitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)profile.CorporateRole);
      foreach(var bank in corporateAuthorization)
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.CorporateAdminApprovalRequest(bank.Email,profile.FullName,userRole.RoleName)));
      }
    }

    public List<TblBankProfile> GetBankAdminAuthorizer()
    {
      var bankProfiles = _unitOfWork.BankProfileRepo.GetAllBankAdminProfilesByRole("bank admin authorizer").ToList();
      if(bankProfiles.Count == 0)
      {
        return new List<TblBankProfile>();
      }
     
      return bankProfiles;
    }

    public List<TblBankProfile> GetSuperAdminAuthorizer()
    {
      var bankProfiles = _unitOfWork.BankProfileRepo.GetAllBankAdminProfilesByRole("super admin authorizer").ToList();
      if(bankProfiles.Count == 0)
      {
        return new List<TblBankProfile>();
      }
     
      return bankProfiles;
    }
    public List<TblCorporateProfile> GetCorporateAuthorizer()
    {
      var corporateProfiles = _unitOfWork.CorporateProfileRepo.GetCorporateProfilesByRole("super admin authorizer").ToList();
      if(corporateProfiles.Count == 0)
      {
        return new List<TblCorporateProfile>();
      }
     
      return corporateProfiles;
    }

    public void NotifyBankAdminAuthorizerForCorporate(TblTempCorporateProfile profile = null,TblCorporateCustomer customer = null,EmailNotification notify = null, bool isRole = false, string reason = null)
    {
      var bankAuthorization = this.GetBankAdminAuthorizer();
      foreach(var bank in bankAuthorization)
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.BankApprovalRequest(bank.Email,notify,null,reason)));
      }
    }

    public void NotifyBankMaker(TblBankProfile user, string action, EmailNotification profile , string reason)
    {
      //var bankAuthorization = this.GetSuperAdminAuthorizer();

      if( string.IsNullOrEmpty(profile.CustomerId))
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.DeclineBankProfilRequest(user.Email,action,profile,reason)));
      }

      if( string.IsNullOrEmpty(profile.Role))
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.DeclineCorporateCustomerRequest(user.Email,action,profile,reason)));
      }
      else
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.DeclineCorporateProfilRequest(user.Email,action,profile,reason)));
      }
      
    }

    public void NotifyCorporateMaker(TblCorporateProfile user, string action, EmailNotification profile, string reason)
    {
      ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.DeclineCorporateProfilRequest(user.Email,action,profile,reason)));
    }

    public void NotifyCorporateTransfer(TblCorporateProfile user =null,TblCorporateProfile approva = null,EmailNotification profile = null,string reason = null)
    {
      if(approva != null)
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.Transfer(user.Email,profile,reason)));
      }

      if(user != null)
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.Transfer(user.Email,profile,reason)));
      }
      
    }


    public void NotifyBankAdminAuthorizer(string action,TblTempBankProfile profile = null, TblTempWorkflow workflow = null, TblCorporateCustomer customer = null)
    {
      var bankAuthorization = this.GetBankAdminAuthorizer();
      var dto = new EmailNotification
      {
        CompanyName = customer?.CompanyName,
        CustomerId = customer?.CustomerId
      };

      if(workflow != null)
      {
        dto.WorkflowName = workflow.Name;
        dto.ApprovalLimit = workflow.ApprovalLimit;
        dto.NoOfAuthorizers = workflow.NoOfAuthorizers;
        if(action == nameof(TempTableAction.Create).Replace("_", " "))
        {
          foreach(var bank in bankAuthorization)
          {
            ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.WorkflowApprovalRequest(bank.Email,dto)));
          }
        }
        if(action == nameof(TempTableAction.Update).Replace("_", " "))
        { 
          foreach(var bank in bankAuthorization)
          {
            ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.WorkflowApprovalRequest(bank.Email,dto,true)));
          }
        }
      }

    }

    public void NotifyBankAuthorizerForCorporate(string Action, TblTempCorporateProfile profile = null, TblCorporateCustomer customer = null, TblTempCorporateCustomer temCustomer = null, TblTempWorkflow workflow = null, string Role =null)
    {
      var bankAuthorization = this.GetBankAdminAuthorizer();
      var dto = new EmailNotification
      {
        CompanyName = customer.CompanyName,
        CustomerId = customer.CustomerId
      };
      
      if(workflow != null)
      {
        dto.WorkflowName = workflow.Name;
        dto.ApprovalLimit = workflow.ApprovalLimit;
        dto.NoOfAuthorizers = workflow.NoOfAuthorizers;
        if(Action == nameof(TempTableAction.Create).Replace("_", " "))
        {
          foreach(var bank in bankAuthorization)
          {
            ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.WorkflowApprovalRequest(bank.Email,dto)));
          }
        }
        if(Action == nameof(TempTableAction.Update).Replace("_", " "))
        { 
          foreach(var bank in bankAuthorization)
          {
            ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.WorkflowApprovalRequest(bank.Email,dto,true)));
          }
        }
      }
   
      if(profile != null)
      {
        dto.FullName = profile.FullName;
        dto.Email = profile.Email;
        dto.PhoneNumber = profile.Phone1;
        dto.Role = Role == "" ? "": Role;
        dto.ApprovalLimit = profile.ApprovalLimit;
        foreach(var bank in bankAuthorization)
        {
          ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.ProfileApprovalRequest(bank.Email,dto,Action)));
        }
      }

      if(temCustomer != null)
      {
        dto.MaxAccountLimit = customer.MaxAccountLimit;
        dto.MinAccountLimit = customer.MinAccountLimit;
        dto.SingleTransDailyLimit = customer.SingleTransDailyLimit;
        dto.BulkTransDailyLimit = customer.BulkTransDailyLimit;
        foreach(var bank in bankAuthorization)
        {
          ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.CorporateCompanyApprovalRequest(bank.Email,dto,Action)));
        }
      }

      // if(customer != null && profile != null)
      // {
      //   dto.MaxAccountLimit = customer.MaxAccountLimit;
      //   dto.MinAccountLimit = customer.MinAccountLimit;
      //   dto.SingleTransDailyLimit = customer.SingleTransDailyLimit;
      //   dto.BulkTransDailyLimit = customer.BulkTransDailyLimit;
      //   foreach(var bank in bankAuthorization)
      //   {
      //     ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.CorporateCompanyApprovalRequest(bank.Email,dto,Action)));
      //   }
      // }
   
    }




    //NOTIFY BANK ADMIN AUTHORIZER FOR CORPORATE CUSTOMER
    public void NotifyBankAdminAuthorizerForCorporateCustomerApproval(TblTempCorporateCustomer customer, EmailNotification profile = null)
    {
      var bankAuthorization = this.GetBankAdminAuthorizer();
      foreach(var bank in bankAuthorization)
      {
        ThreadPool.QueueUserWorkItem(_ =>_emailService.SendEmail(CustomerTemplate.ApprovalRequest(bank.Email,profile)));
      }
      
    }
    public void NotifyBankAdminAuthorizerForCorporateCustomerDecline(TblBankProfile bank, EmailNotification profile = null)
    {
      //_emailService.SendEmail(CustomerTemplate.DeclineRequest("kufy201@gmail.com",profile));
      ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(CustomerTemplate.DeclineRequest(bank.Email,profile)));
    }
 
    //NOTIFY BANK ADMIN AUTHORIZER FOR CORPORATE PROFILE
    public void NotifyBankAdminAuthorizerForCorporateProfileApproval(EmailNotification profile = null)
    {
      var bankAuthorization = this.GetBankAdminAuthorizer();
      foreach(var bank in bankAuthorization)
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(profileTemplate.ApprovalRequest(bank.Email,profile)));
      }
    }
    public void NotifyBankAdminAuthorizerForCorporateProfileDecline(TblBankProfile profile, EmailNotification notify = null)
    {
      ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(profileTemplate.DeclineRequest(profile.Email,notify)));
    }
 
    //NOTIFY BANK ADMIN AUTHORIZER FOR CORPORATE WORKFLOW
    public void NotifyBankAdminAuthorizerForCorporateWorkflowApproval(EmailNotification profile = null)
    {
      var bankAuthorization = this.GetBankAdminAuthorizer();
      foreach(var bank in bankAuthorization)
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(WorkflowTemplate.ApprovalRequest(bank.Email,profile)));
      }
    }
    public void NotifyBankAdminAuthorizerForCorporateWorkflowDecline(TblBankProfile profile, EmailNotification notify = null)
    {
      ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(WorkflowTemplate.DeclineRequest(profile.Email,notify)));
    }
 
    //NOTIFY SUPER ADMIN BANK AUTHORIZER FOR BANK PROFILE
    public void NotifySuperAdminBankAuthorizerForBankProfileApproval(EmailNotification profile = null)
    {
      var bankAuthorization = this.GetSuperAdminAuthorizer();
      foreach(var bank in bankAuthorization)
      {
        ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(BankTemplate.ApprovalRequest(bank.Email,profile)));
      }
    }
    public void NotifySuperAdminBankAuthorizerForBankProfileDecline(TblBankProfile profile, EmailNotification notify = null)
    {
      ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(BankTemplate.DeclineRequest(profile.Email,notify)));
    }
 
    //NOTIFY CORPORATE AUTHORIZER FOR TRANSFER 
    public void NotifyCorporateAuthorizerForTransferApproval(TblCorporateProfile profile, EmailNotification notify = null)
    {
      ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(Transfer.ApprovalRequest(profile.Email,notify)));
    }
    public void NotifyCorporateAuthorizerForTransferDecline(TblCorporateProfile profile, EmailNotification notify = null)
    {
      
      ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(Transfer.DeclineApproval(profile.Email,notify)));
      
    }
 

  }
}
