
using CIB.Core.Modules.Authentication.Dto;
using CIB.Core.Modules.BankAdminProfile.Dto;
using CIB.Core.Modules.SecurityQuestion.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.Authentication.Validation
{
    public class BankLoginValidation : AbstractValidator<BankUserLoginParam>
    {
        public BankLoginValidation()
        {
            RuleFor(p => p.Username)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MinimumLength(5).WithMessage("{PropertyName} must be more than 5 characters.");
            RuleFor(p => p.Token)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.");
        }
    }



    public class ResetPasswordValidation : AbstractValidator<ResetPasswordModel>
    {
         public ResetPasswordValidation()
        {
            RuleFor(p => p.Email.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Code.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.Password.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MinimumLength(5).WithMessage("{PropertyName} must be more than 5 characters.");
            RuleFor(p => p.CustomerId.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    }

    public class ForgotPasswordValidation : AbstractValidator<ForgetPassword>
    {
         public ForgotPasswordValidation()
        {
            RuleFor(p => p.Email.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.CustomerId.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    }

    public class SetSecurityQuestionValidation : AbstractValidator<SetSecurityQuestion>
    {

        //  public string UserName { get; set; }
        // public string CustomerId { get; set; }
        // public string Password { get; set; }
        // public int SecurityQuestionId { get; set; }
        // public string Answer { get; set; }
        // public int SecurityQuestionId2 { get; set; }
        // public string Answer2 { get; set; }
        // public int SecurityQuestionId3 { get; set; }
        // public string Answer3 { get; set; }



         public SetSecurityQuestionValidation()
        {
            RuleFor(p => p.UserName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.Password.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MinimumLength(5).WithMessage("{PropertyName} must be more than 5 characters.");
            RuleFor(p => p.CustomerId.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    }



    public class FirstLoginPasswordChangeValidation : AbstractValidator<FirstLoginPasswordChangeModel>
    {
         public FirstLoginPasswordChangeValidation()
        {
            RuleFor(p => p.UserName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.CurrentPassword.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MinimumLength(5).WithMessage("{PropertyName} must be more than 5 characters.");
            RuleFor(p => p.NewPassword.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MinimumLength(5).WithMessage("{PropertyName} must be more than 5 characters.");
            RuleFor(p => p.CustomerId.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    }

    public class ChangeUserPasswordValidation : AbstractValidator<CustomerLoginParam>
    {
         public ChangeUserPasswordValidation()
        {
            RuleFor(p => p.Username.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.Password.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MinimumLength(5).WithMessage("{PropertyName} must be more than 5 characters.");
            RuleFor(p => p.CustomerID.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    }


    




    



    










}