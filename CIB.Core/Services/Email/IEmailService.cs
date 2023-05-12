using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Services.Email.Dto;

namespace CIB.Core.Services.Email
{
    public interface IEmailService
    {
        Task<EmailResponseDto> SendEmail(EmailRequestDto mail);
    }
}