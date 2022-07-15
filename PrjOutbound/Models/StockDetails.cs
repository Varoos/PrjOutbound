using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjOutbound.Models
{
    public class StockDetails
    {
        public int iTransactionId { get; set; }
    }

    //public class Datum
    //{
    //    public string CompanyCode { get; set; }
    //    public string Identifier { get; set; }
    //    public string Secret { get; set; }
    //    public string Lng { get; set; }
    //}

    //public class Resultlogin
    //{
    //    public string AccessToken { get; set; }
    //    public bool IsSuccess { get; set; }
    //    public string StatusMsg { get; set; }
    //}

    public class StockList
    {
        public string AccessToken { get; set; }
        public string ObjectType { get; set; }
        public List<StockDetailsData> ItemList { get; set; }
    }

    public class StockDetailsData
    {
        public string SourceStockCode { get; set; }
        public string DestinationStockCode { get; set; }
        public int ProductId { get; set; }
        public string ProductUnit { get; set; }
        public string BatchId { get; set; }
        public string ExpiryDate { get; set; }
        public decimal Qty { get; set; }
        public string TransactionDateTime { get; set; }
        public string Comments { get; set; }
        public int Type { get; set; }
    }

    //public class Result
    //{
    //    public ResponseStatus ResponseStatus { get; set; }
    //    public List<string> ErrorMessages { get; set; }
    //    public List<FailedList> FailedList { get; set; }
    //}
    //public class ResponseStatus
    //{
    //    public bool IsSuccess { get; set; }
    //    public string StatusMsg { get; set; }
    //    public string ErrorCode { get; set; }
    //}
    //public class FailedList
    //{
    //    public string SourceStockCode { get; set; }
    //    public string DestinationStockCode { get; set; }
    //    public int ProductId { get; set; }
    //    public string ProductUnit { get; set; }
    //    public string BatchId { get; set; }
    //    public string ExpiryDate { get; set; }
    //    public decimal Qty { get; set; }
    //    public string TransactionDateTime { get; set; }
    //    public string Comments { get; set; }
    //    public int Type { get; set; }
    //}
    public class ReceiptItem
    {
        public string MobileCode { get; set; }
        public string ChequeNo { get; set; }
        public decimal Amount { get; set; }
        public string ChequeDate { get; set; }
        public string Note { get; set; }
        public string InvoiceMobileCode { get; set; }
    }
    public class ReceiptList
    {
        public string AccessToken { get; set; }
        public string ObjectType { get; set; }
        public List<ReceiptItem> ItemList { get; set; }
    }
    public class InvoiceLineItems
    {
        public decimal Discount { get; set; }
        public decimal OfferedQty { get; set; }
        public int ProductId { get; set; }
        public string ProductUnit { get; set; }
        public decimal SoldQty { get; set; }
        public decimal ReturnedQty { get; set; }
        public decimal ItemValue { get; set; }
        public decimal Price { get; set; }
        public string BatchId { get; set; }
        public string ExpiryDate { get; set; }
        public decimal TaxValue { get; set; }
        public string TaxCategoryCode { get; set; }
    }
    public class InvoiceItem
    {
        public string MobileCode { get; set; }
        public decimal TotalSales { get; set; }
        public decimal Discount { get; set; }
        public decimal NetTotal { get; set; }
        public decimal VatValue { get; set; }
        public decimal GrandTotal { get; set; }
        public List<InvoiceLineItems> InvoiceLineItems { get; set; }
    }
    public class InvoiceList
    {
        public string AccessToken { get; set; }
        public string ObjectType { get; set; }
        public List<InvoiceItem> ItemList { get; set; }
    }
}