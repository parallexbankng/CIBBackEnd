using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;

using System.Threading.Tasks;
using CIB.Core.Modules.BulkTransaction.Dto;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;
using CIB.Core.Services.File.Dto;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;

namespace CIB.Core.Services.File
{
  public class FileService : IFileService
  {
    public List<VerifyBulkTransactionResponseDto> ReadExcelFile(IFormFile request)
    {
      try
      {
        var excelDataList = new List<VerifyBulkTransactionResponseDto>();
        if(request.FileName.Contains(".xlsx", System.StringComparison.OrdinalIgnoreCase))
        {
          System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
          using var stream = new MemoryStream();
          request.CopyTo(stream);
          stream.Position = 1;
          using var reader = ExcelReaderFactory.CreateReader(stream);
          var flag = 0;
          while (reader.Read()) //Each row of the file
          {
            if(flag != 0){
              // var AccountName = reader.GetValue(0)?.ToString().Trim() ?? "-1";
              // var row = new VerifyBulkTransactionResponseDto
              // {
              //   AccountName = reader.GetValue(0)?.ToString().Trim() ?? "-1",
              //   CreditAccount = reader.GetValue(1)?.ToString().Trim() ?? "-1",
              //   CreditAmount = Convert.ToDecimal(reader.GetValue(2).ToString().Trim()),
              //   BankCode = reader.GetValue(3)?.ToString().Trim() ?? "-1",
              //   Narration = reader.GetValue(4)?.ToString().Trim() ?? "-1",
                
              // };
              // excelDataList.Add(row);
              var row = new VerifyBulkTransactionResponseDto();
              row.AccountName = reader.GetValue(0)?.ToString().Trim() ?? "-1";
              row.CreditAccount = reader.GetValue(1)?.ToString().Trim() ?? "-1";
              try
              {
                  row.CreditAmount = Convert.ToDecimal(reader.GetValue(2)?.ToString()?.Trim());
              }
              catch (Exception)
              {
                  //return new List<ExcelBulkUploadParameter>();
                  throw new Exception($"Unable to read amount on row number {flag}. Please check and make corrections where necessary and try again");
              }
              row.BankCode = reader.GetValue(3)?.ToString().Trim() ?? "-1";
              row.Narration = reader.GetValue(4)?.ToString().Trim() ?? "-1";
              if (row.AccountName == "-1" && row.CreditAmount == 0 && row.BankCode == "-1" && row.Narration == "-1")
              {
                continue;
              }
              excelDataList.Add(row);
            }else {
              flag++;
            }
          }
          if(excelDataList.Count == 0){
            return new List<VerifyBulkTransactionResponseDto>();
          }
          return excelDataList;
        }
        return new List<VerifyBulkTransactionResponseDto>();
      }
      catch (Exception ex)
      {
        return new List<VerifyBulkTransactionResponseDto>();
      }
    }

    public void DeleteFile(string filename)
    {
      try
        {
          if (System.IO.File.Exists(filename))
          {
            // If file found, delete it    
            System.IO.File.Delete(filename);
          }
        }
        catch { }
    }

    public  List<VerifyBulkTransactionResponseDto> ReadAndSaveExcelFile(IFormFile request, string path)
  {
    try
        {
            var excelDataList = new List<VerifyBulkTransactionResponseDto>();
            if (request.FileName.Contains(".xlsx", System.StringComparison.OrdinalIgnoreCase))
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var stream = new FileStream(path, FileMode.Create);
                request.CopyTo(stream);
                stream.Position = 1;
                using var reader = ExcelReaderFactory.CreateReader(stream);
                var flag = 0;
                while (reader.Read())
                {
                  if(flag != 0)
                  {
                
                  var row = new VerifyBulkTransactionResponseDto();
                  row.AccountName = reader.GetValue(0)?.ToString().Trim() ?? "-1";
                  row.CreditAccount = reader.GetValue(1)?.ToString().Trim() ?? "-1";
                  try
                  {
                      row.CreditAmount = Convert.ToDecimal(reader.GetValue(2)?.ToString()?.Trim());
                  }
                  catch (Exception)
                  {
                      //return new List<ExcelBulkUploadParameter>();
                      throw new Exception($"Unable to read amount on row number {flag}. Please check and make corrections where necessary and try again");
                  }
                  row.BankCode = reader.GetValue(3)?.ToString().Trim() ?? "-1";
                  row.Narration = reader.GetValue(4)?.ToString().Trim() ?? "-1";
                  if (row.AccountName == "-1" && row.CreditAmount == 0 && row.BankCode == "-1" && row.Narration == "-1")
                  {
                    continue;
                  }
                  excelDataList.Add(row);
                }else {
                  flag++;
                }

                }
                if (excelDataList.Count == 0)
                {
                    return new List<VerifyBulkTransactionResponseDto>();
                }

                //save stream
                stream.Flush();
                return excelDataList;
            }
            return new List<VerifyBulkTransactionResponseDto>();
        }
        catch (Exception ex)
        {
            return new List<VerifyBulkTransactionResponseDto>();
        }
  }

    DataTable IFileService.ConvertXSLXtoDataTable(string strFilePath, string connString)
    {
      throw new NotImplementedException();
    }

    public  List<VerifyBulkCorporateEmployeeResponseDto> ReadEmployeeExcelFile(IFormFile request)
    {
      var excelDataList = new List<VerifyBulkCorporateEmployeeResponseDto>();
      if (request.FileName.Contains(".xlsx", System.StringComparison.OrdinalIgnoreCase))
      {
         System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
          using var stream = new MemoryStream();
          request.CopyTo(stream);
          stream.Position = 1;
          using var reader = ExcelReaderFactory.CreateReader(stream);
          var flag = 0;
          while (reader.Read())
          {
            if(flag != 0)
            {
          
            var row = new VerifyBulkCorporateEmployeeResponseDto();
            row.FirstName = reader.GetValue(0)?.ToString().Trim() ?? "-1";
            row.LastName = reader.GetValue(1)?.ToString().Trim() ?? "-1";
            row.StaffId = reader.GetValue(1)?.ToString().Trim() ?? "-1";
            row.Department = reader.GetValue(1)?.ToString().Trim() ?? "-1";
            row.AccountName = reader.GetValue(1)?.ToString().Trim() ?? "-1";
            row.AccountNumber = reader.GetValue(1)?.ToString().Trim() ?? "-1";
            row.BankCode = reader.GetValue(1)?.ToString().Trim() ?? "-1";
            try
            {
                row.SalaryAmount = Convert.ToDecimal(reader.GetValue(2)?.ToString()?.Trim());
            }
            catch (Exception)
            {
              throw new Exception($"Unable to read amount on row number {flag}. Please check and make corrections where necessary and try again");
            }
            row.GradeLevel = reader.GetValue(3)?.ToString().Trim() ?? "-1";
            row.Description = reader.GetValue(4)?.ToString().Trim() ?? "-1";
            if (row.AccountName == "-1" && row.SalaryAmount == 0 && row.BankCode == "-1" && row.Description == "-1")
            {
              continue;
            }
            excelDataList.Add(row);
          }
          else 
          {
            flag++;
          }

          }
          if (excelDataList.Count == 0)
          {
              return new List<VerifyBulkCorporateEmployeeResponseDto>();
          }

          //save stream
          stream.Flush();
          return excelDataList;
      }
      return new List<VerifyBulkCorporateEmployeeResponseDto>();
    }


    public List<BulkCustomerOnboading> ReadCorporateCustomerExcelFile(IFormFile request)
    {
      if(request.FileName.Contains(".xlsx", System.StringComparison.OrdinalIgnoreCase))
        {
          var excelDataList = new List<BulkCustomerOnboading>();
          System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
          using var stream = new MemoryStream();
          request.CopyTo(stream);
          stream.Position = 1;
          using var reader = ExcelReaderFactory.CreateReader(stream);
          var flag = 0;
          while (reader.Read()) //Each row of the file
          {
            if(flag != 0){

            var row = new BulkCustomerOnboading
            {
              CompanyName = reader.GetValue(0)?.ToString().Trim() ?? " ",
              CustomerId = reader.GetValue(1)?.ToString().Trim() ?? " ",
              DefaultAccountName = reader.GetValue(2)?.ToString().Trim() ?? " ",
              DefaultAccountNumber = reader.GetValue(3)?.ToString().Trim() ?? " ",
              AuthorizationType = reader.GetValue(4)?.ToString().Trim() ?? " ",
              Username = reader.GetValue(5)?.ToString().Trim() ?? " ",
              CorporateEmail = reader.GetValue(6)?.ToString().Trim() ?? " ",
              FirstName = reader.GetValue(7)?.ToString().Trim() ?? " ",
              LastName = reader.GetValue(8)?.ToString().Trim() ?? " ",
              MiddleName = reader.GetValue(9)?.ToString().Trim() ?? " ",
              Email = reader.GetValue(10)?.ToString().Trim() ?? " ",
              PhoneNumber = reader.GetValue(11)?.ToString().Trim() ?? " "
            };
            try
              {
                row.SingleTransDailyLimit = Convert.ToDecimal(reader.GetValue(12)?.ToString()?.Trim());
                row.BulkTransDailyLimit = Convert.ToDecimal(reader.GetValue(13)?.ToString()?.Trim());
                row.MinAccountLimit = Convert.ToDecimal(reader.GetValue(14)?.ToString()?.Trim());
                row.MaxAccountLimit = Convert.ToDecimal(reader.GetValue(15)?.ToString()?.Trim());
              }
              catch (Exception)
              {
                  //return new List<ExcelBulkUploadParameter>();
                  throw new Exception($"Unable to read amount on row number {flag}. Please check and make corrections where necessary and try again");
              }
              
              if (row.DefaultAccountName == " " && row.MinAccountLimit == 0 && row.CustomerId == " " && row.FirstName == " ")
              {
                continue;
              }
              excelDataList.Add(row);
            }else {
              flag++;
            }
          }
         
          return excelDataList;
        }
        return new List<BulkCustomerOnboading>();
    }
  }
}