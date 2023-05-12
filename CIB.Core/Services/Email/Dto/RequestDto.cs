using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Services.Email.Dto
{
    public class EmailRequestDto
    {
        public string sender { get; set; }
        public string subject { get; set; }
        public string recipient { get; set; }
        public string message { get; set; }
        public bool isHtml { get; set; }

  }
}