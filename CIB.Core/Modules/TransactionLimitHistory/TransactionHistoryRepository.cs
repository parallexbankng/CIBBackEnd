using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.TransactionLimitHistory
{
  public class TransactionHistoryRepository : Repository<TblCorporateCustomerDailyTransLimitHistory>, ITransactionHistoryRepository
  {
    public TransactionHistoryRepository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }
    public TblCorporateCustomerDailyTransLimitHistory GetTransactionHistory(Guid corporateCustomerId, DateTime date)
    {
      return _context.TblCorporateCustomerDailyTransLimitHistories.FirstOrDefault(ctx => ctx.CorporateCustomerId == corporateCustomerId && ctx.Date != null && ctx.Date.Value.Date == date.Date);
    }
    public void SetOrUpdateDailyBulkTransLimitHistory(TblCorporateCustomer customer, decimal transactionAmount)
    {
      var dailyLimitHistory = GetTransactionHistory((Guid)customer.Id, DateTime.Now);
      if (dailyLimitHistory == null)
      {
        dailyLimitHistory = new TblCorporateCustomerDailyTransLimitHistory
        {
          Id = 0,
          SingleTransTotalAmount = 0,
          CorporateCustomerId = customer.Id,
          CustomerId = customer.CustomerId,
          SingleTransTotalCount = 1,
          BulkTransTotalAmount = transactionAmount,
          BulkTransTotalCount = 0,
          Date = DateTime.Now,
          BulkTransAmountLeft = customer.BulkTransDailyLimit ?? 0,
          SingleTransAmountLeft = customer.SingleTransDailyLimit == null || customer.SingleTransDailyLimit == 0 ? 0 : (decimal)customer.SingleTransDailyLimit - transactionAmount
        };
        _context.TblCorporateCustomerDailyTransLimitHistories.Add(dailyLimitHistory);
      }
      else
      {
        dailyLimitHistory.BulkTransTotalAmount += transactionAmount;
        dailyLimitHistory.BulkTransTotalCount += 1;
        dailyLimitHistory.Date = DateTime.Now;
        //dailyLimitHistory.BulkTransAmountLeft = customer.BulkTransDailyLimit == null || customer.BulkTransDailyLimit == 0 ? 0 : (decimal)customer.BulkTransDailyLimit - (decimal)dailyLimitHistory.BulkTransTotalAmount;

      }
    }
    public void SetOrUpdateDailySingleTransLimitHistory(TblCorporateCustomer customer, decimal transactionAmount)
    {

      var dailyLimitHistory = GetTransactionHistory((Guid)customer.Id, DateTime.Now);
      if (dailyLimitHistory == null)
      {
        var dailyLimit = new TblCorporateCustomerDailyTransLimitHistory
        {
          BulkTransTotalAmount = 0,
          CorporateCustomerId = customer.Id,
          CustomerId = customer.CustomerId,
          BulkTransTotalCount = 1,
          SingleTransTotalAmount = transactionAmount,
          SingleTransTotalCount = 0,
          Date = DateTime.Now,
          //SingleTransAmountLeft = customer.SingleTransDailyLimit ?? 0,
          // BulkTransAmountLeft = customer.BulkTransDailyLimit == null || customer.BulkTransDailyLimit == 0 ? 0 : (decimal)customer.BulkTransDailyLimit - transactionAmount
        };
        _context.TblCorporateCustomerDailyTransLimitHistories.Add(dailyLimit);
      }
      else
      {
        dailyLimitHistory.SingleTransTotalAmount += transactionAmount;
        dailyLimitHistory.SingleTransTotalCount += 1;
        dailyLimitHistory.Date = DateTime.Now;
        ///dailyLimitHistory.SingleTransAmountLeft = customer.SingleTransDailyLimit == null || customer.SingleTransDailyLimit == 0 ? 0 : (decimal)customer.SingleTransDailyLimit - (decimal)dailyLimitHistory.SingleTransTotalAmount;
      }
      //_context.SaveChanges();
      // _context.Dispose();
    }
  }
}
