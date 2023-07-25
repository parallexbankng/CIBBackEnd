using System.Collections.Generic;
using System.Data;
using CIB.Core.Modules.BulkTransaction.Dto;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;
using CIB.Core.Modules.OnLending.Beneficiary.Dto;
using Microsoft.AspNetCore.Http;

namespace CIB.Core.Services.File
{
  public interface IFileService
  {
    List<VerifyBulkTransactionResponseDto> ReadExcelFile(IFormFile request);
    List<BeneficiaryDto> ReadOnlendingBeneficiariesExcelFile(IFormFile request);
    List<BulkCustomerOnboading> ReadCorporateCustomerExcelFile(IFormFile request);
    List<VerifyBulkTransactionResponseDto> ReadAndSaveExcelFile(IFormFile request, string path);
    List<VerifyBulkCorporateEmployeeResponseDto> ReadEmployeeExcelFile(IFormFile request);
    DataTable ConvertXSLXtoDataTable(string strFilePath, string connString);
    void DeleteFile(string filename);
  }
}