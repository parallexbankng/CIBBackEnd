using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Common.Dto
{
    public class ErrorResponse
    {
        public ErrorResponse (string responsecode,string responseDescription, bool responseStatus)
        {
            Responsecode = responsecode;
            ResponseDescription = responseDescription;
            ResponseStatus = responseStatus;
        }
        public string Responsecode {get;set;}
        public string ResponseDescription {get;set;}
        public bool ResponseStatus {get;set;}
    }
}