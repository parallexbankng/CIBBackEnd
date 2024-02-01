using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Services.Email.Dto
{
  public class EmailResponseDto
  {
    public string  ResponseCode {get;set;}
    public string  ResponseDescription {get;set;}
    public bool IsSuccess { get; set; }

  }
}