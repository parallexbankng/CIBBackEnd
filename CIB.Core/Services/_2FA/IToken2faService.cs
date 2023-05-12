using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Services._2FA.Dto;

namespace CIB.Core.Services._2FA
{
    public interface IToken2faService
    {
        Task<_2faResponseDto> TokenAuth(string UserName, string Token);
    }
}