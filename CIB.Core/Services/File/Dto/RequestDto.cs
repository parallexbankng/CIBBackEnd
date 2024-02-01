using System.Security.Cryptography.X509Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CIB.Core.Entities;

namespace CIB.Core.Services.File.Dto
{
  public class UploadCsvFileDto
  {
  }

  public class UploadExcelFileDto
  {
    public string SourceAccountName { get; set; }
    // public string SourceAccountNumber { get; set; }
    public string Narration { get; set; }
    // public string SourceBankName { get; set; }
    public string Amount { get; set; }
    // public string SourceBankCode { get; set; }
    public string TransactionType { get; set; }
    public string WorkflowId {get;set;}
    public string Currency {get;set;}
    public IFormFile files { get; set; }
   }

  public class ExcelBulkUploadParameter
  {
    public string CreditAccount { get; set; }
    public string CreditAccountName { get; set; }
    public decimal CreditAmount { get; set; }
    public string Narration { get; set; }
    public string BankName { get; set; }
    public string BankCode { get; set; }
  }

  public class InitiateBulkTransferDtoResponse : UploadExcelFileDto
  {
    public string Otp { get; set; }
  }
}