using Focus.Common.DataStructs;
using Newtonsoft.Json;
using PrjOutbound.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace PrjOutbound.Controllers
{
    public class TransactionController : Controller
    {
        string AccessToken = "";
        string AccessTokenURL = ConfigurationManager.AppSettings["AccessTokenURL"];
        string PostingURL = ConfigurationManager.AppSettings["PostingURL"];
        string serverip = ConfigurationManager.AppSettings["ServerIP"];
        string serveripp = ConfigurationManager.AppSettings["ServerIPP"];
        string CompanyCode = ConfigurationManager.AppSettings["CompanyCode"];
        string Identifier = ConfigurationManager.AppSettings["Identifier"];
        string Secret = ConfigurationManager.AppSettings["Secret"];
        string Lng = ConfigurationManager.AppSettings["Lng"];
        BL_Registry objreg = new BL_Registry();
        // GET: StockTransfer
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Posting(int CompanyId, string SessionId, int LoginId, int vtype, string DocNo)
        {
            SP(CompanyId);
            SPR(CompanyId);

            objreg.SetLog2("TransactionLog", "Posting Method");
            string errors1 = "";
            string Message = "";
            try
            {
                List<StockDetails> Listsd = new List<StockDetails>();

                #region BaseVoucherDetails
                string Basequery = $@"select iTransactionId
                                from tCore_Header_0 h 
                                join tCore_Data_0 d on h.iHeaderId=d.iHeaderId
                                where iVoucherType={vtype} and sVoucherNo= '{DocNo}'";
                objreg.SetLog2("TransactionLog", Basequery);
                DataSet baseds = objreg.GetData(Basequery, CompanyId, ref errors1);

                foreach (DataRow row in baseds.Tables[0].Rows)
                {
                    StockDetails c = new StockDetails();
                    c.iTransactionId = Convert.ToInt32(row["iTransactionId"]);
                    Listsd.Add(c);
                }
                #endregion
                if (Listsd.Count() == 0)
                {
                    var sMessage = "No Data Found";
                    objreg.SetLog(sMessage);
                    objreg.SetLog2("TransactionLog", sMessage);
                    return Json(new { success = sMessage }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var LinkType = "";
                    int LRCount = 0;
                    int MRCount = 0;

                    #region loadRequestSection
                    string lrquery = $@"declare @Base int;
                    declare @Des int;
                    declare @linkid int;
                    set @Base = (select iVoucherType from cCore_Vouchers_0 where sName = 'LoadRequest');
                    set @Des = (select iVoucherType from cCore_Vouchers_0 where sName = 'Stock Transfer Issue - VAN');
                    select @linkid = ilinkpathid from vmCore_Links_0 where BaseVoucherId = @Base and LinkVoucherId = @Des group by ilinkpathid,Basevoucherid
                    select count(*) [Count] from tCore_Links_0 where iLinkId = @linkid and bBase = 0 and iTransactionId= '{Listsd.First().iTransactionId}'";
                    objreg.SetLog2("TransactionLog", lrquery);
                    DataSet lrds = objreg.GetData(lrquery, CompanyId, ref errors1);

                    foreach (DataRow row in lrds.Tables[0].Rows)
                    {
                        LRCount = Convert.ToInt32(row["Count"]);
                        LinkType = "Load Request";
                    }
                    #endregion

                    if (LRCount == 0)
                    {
                        #region MobileOthersSection
                        string moquery = $@"declare @Base int;
                            declare @Des int;
                            declare @linkid int;
                            set @Base = (select iVoucherType from cCore_Vouchers_0 where sName = 'MobileRequest-Others');
                            set @Des = (select iVoucherType from cCore_Vouchers_0 where sName = 'Stock Transfer Issue - VAN');
                            select @linkid = ilinkpathid from vmCore_Links_0 where BaseVoucherId = @Base and LinkVoucherId = @Des group by ilinkpathid,Basevoucherid
                            select count(*) [Count] from tCore_Links_0 where iLinkId = @linkid and bBase = 0 and iTransactionId= '{Listsd.First().iTransactionId}'";
                        objreg.SetLog2("TransactionLog", moquery);
                        DataSet mods = objreg.GetData(moquery, CompanyId, ref errors1);

                        foreach (DataRow row in mods.Tables[0].Rows)
                        {
                            MRCount = Convert.ToInt32(row["Count"]);
                            LinkType = "Mobile Others";
                        }
                        #endregion
                    }
                    objreg.SetLog2("TransactionLog", "LinkType" + LinkType);

                    if (LinkType == "Load Request")
                    {
                        #region LoadReq
                        AccessToken = GetAccessToken();
                        if (AccessToken == "")
                        {
                            Message = "Invalid Token";
                            objreg.SetLog("Invalid Token");
                            objreg.SetLog2("TransactionLog", Message);
                            var sMessage = "Token Should not be Empty";
                            objreg.SetLog(sMessage);
                            objreg.SetLog2("TransactionLog", sMessage);
                            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                        }

                        StockList clist = new StockList();
                        clist.AccessToken = AccessToken;
                        clist.ObjectType = "vanStock";

                        #region VoucherDetails
                        string query = $@"declare @Base int;
                            declare @Des int;
                            set @Base = (select iVoucherType from cCore_Vouchers_0 where sName = 'Stock Transfer Receipt - VAN');
                            set @Des = (select iVoucherType from cCore_Vouchers_0 where sName = 'Stock Transfer Issue - VAN');
                            declare @to varchar(20) = (select iMasterTypeId from cCore_MasterDef where sMasterName = 'To')
							declare @from varchar(20) = (select iMasterTypeId from cCore_MasterDef where sMasterName = 'From')
                            declare @qry varchar(max);
                            set @qry = '
                            select iTransactionId,ouf.sCode [OutletFrom],outt.sCode [OutletTo],iProduct,u.sCode [Productunit] ,sBatchNo,    
                            case when b.iExpiryDate=0 then ''0'' else convert(varchar,dbo.IntToDate(b.iExpiryDate),23) end [ExpiryDate],    
                            abs(fQuantity) Quantity,ISNULL(sNarration,'''') Narration,convert(varchar,dbo.IntToDate(h.iDate),23) [TransactionDate] from tCore_Header_0 h     
                            join tCore_Data_0 d on h.iHeaderId=d.iHeaderId    
                            join tCore_Indta_0 i on i.iBodyId=d.iBodyId    
                            join tCore_HeaderData'+cast(@Base as varchar)+'_0 hd on hd.iHeaderId=h.iHeaderId    
                            join tCore_Data_Tags_0 dt on dt.iBodyId=d.iBodyId    
                            join mCore_From ouf on ouf.iMasterId=iTag'+@from+' and ouf.iStatus<>5    
                            join mCore_To outt on outt.iMasterId=iTag'+@to+'  and outt.iStatus<>5  
                            join mCore_Units u on u.iMasterId=iUnit    
                            left join tCore_Batch_0 b on b.iBodyId=d.iBodyId    
                            where iVoucherType='+cast(@Base as varchar)+' and d.iTransactionId in(    
                            select ll.iTransactionId from tCore_Header_0 h join tCore_Data_0 d on h.iHeaderId=d.iHeaderId    
                            join tCore_Links_0 l on l.iTransactionId=d.iBodyId and bBase = 1     
                            join tCore_Links_0 ll on ll.iRefId=l.iRefId and ll.bBase=0    
                            where iVoucherType='+cast(@Des as varchar)+' and sVoucherNo=''{DocNo}'')' exec (@qry);";
                        objreg.SetLog2("TransactionLog", query);
                        List<StockDetailsData> lc = new List<StockDetailsData>();
                        DataSet ds = objreg.GetData(query, CompanyId, ref errors1);

                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            var CurrentDate = Convert.ToDateTime(row["TransactionDate"]).ToString();
                            StockDetailsData c = new StockDetailsData();
                            c.SourceStockCode = Convert.ToString(row["OutletFrom"]);
                            c.DestinationStockCode = Convert.ToString(row["OutletTo"]);
                            c.ProductId = Convert.ToInt32(row["iProduct"]);
                            c.ProductUnit = Convert.ToString(row["Productunit"]);
                            c.BatchId = Convert.ToString(row["sBatchNo"]);
                            if (Convert.ToString(row["ExpiryDate"]) == "0")
                            {
                                c.ExpiryDate = null;
                            }
                            else
                            {
                                c.ExpiryDate = Convert.ToString(row["ExpiryDate"]);
                            }
                            c.Qty = Convert.ToDecimal(row["Quantity"]);
                            c.TransactionDateTime = Convert.ToString(CurrentDate);
                            c.Comments = Convert.ToString(row["Narration"]);
                            c.Type = 2;
                            lc.Add(c);
                        }
                        #endregion

                        clist.ItemList = lc;

                        if (clist.ItemList.Count() > 0)
                        {
                            #region PostingSection

                            var sContent = new JavaScriptSerializer().Serialize(clist);
                            using (WebClient client = new WebClient())
                            {
                                client.Headers.Add("Content-Type", "application/json");
                                objreg.SetSuccessLog("TransactionLog.log", "Load Request Posted sContent: " + sContent);
                                objreg.SetLog2("TransactionLog", "Load Request Posted sContent: " + sContent);
                                objreg.SetLog2("TransactionLog", " PostingURL :" + PostingURL);
                                var arrResponse = client.UploadString(PostingURL, sContent);
                                var lng = JsonConvert.DeserializeObject<Result>(arrResponse);

                                if (lng.ResponseStatus.IsSuccess == true)
                                {
                                    foreach (var item in clist.ItemList)
                                    {
                                        FConvert.LogFile("TransactionLog.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Voucher:- " + DocNo);
                                    }
                                    Message = "Posted Successfully";
                                }
                                else
                                {
                                    int ErrorListCount = lng.ErrorMessages.Count();
                                    if (ErrorListCount > 0)
                                    {
                                        var ErrorList = lng.ErrorMessages.ToList();
                                        foreach (var item in ErrorList)
                                        {
                                            objreg.SetErrorLog("TransactionLog.log", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                            objreg.SetLog2("TransactionLog", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                        }
                                    }

                                    int FailedListCount = lng.FailedList.Count();
                                    if (FailedListCount > 0)
                                    {
                                        var FailedList = lng.FailedList.ToList();
                                        foreach (var item in FailedList)
                                        {
                                            objreg.SetErrorLog("TransactionLog.log", "FailedList Message for Voucher No '" + DocNo + "'");
                                            objreg.SetLog2("TransactionLog", "FailedList Message for Voucher No '" + DocNo + "'");
                                        }
                                    }
                                }
                            }

                            #endregion
                        }
                        #endregion
                    }
                    else if (LinkType == "Mobile Others")
                    {
                        #region Mobile Others
                        AccessToken = GetAccessToken();
                        if (AccessToken == "")
                        {
                            Message = "Invalid Token";
                            objreg.SetLog("Invalid Token");
                            objreg.SetLog2("TransactionLog", Message);
                            var sMessage = "Token Should not be Empty";
                            objreg.SetLog(sMessage);
                            objreg.SetLog2("TransactionLog", sMessage);
                            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                        }

                        StockList clist = new StockList();
                        clist.AccessToken = AccessToken;
                        clist.ObjectType = "vanStock";

                        #region VoucherDetails
                        string query = $@"declare @Des int;
                        set @Des = (select iVoucherType from cCore_Vouchers_0 where sName = 'Stock Transfer Issue - VAN');
                        declare @to varchar(20) = (select iMasterTypeId from cCore_MasterDef where sMasterName = 'To')
                        declare @from varchar(20) = (select iMasterTypeId from cCore_MasterDef where sMasterName = 'From')
                        declare @qry varchar(max);
                        declare @Invtag varchar(100) = (SELECT sMasterName  FROM cCore_MasterDef WHERE iMasterTypeId = (SELECT iValue FROM cCore_PreferenceVal_0 WHERE iCategory = 0 and iFieldId = 1))
                        if (@Invtag = 'Outlet')
                        set @Invtag = 'Pos_'+@Invtag;
                        else
                        set @Invtag = 'Core_'+@Invtag;
                        set @qry = '
                        select sVoucherNo,iTransactionId,ouf.sCode [OutletFrom],outt.sCode [OutletTo],iProduct,u.sCode [Productunit] ,sBatchNo,      
                        case when b.iExpiryDate=0 then ''0''  else convert(varchar,dbo.IntToDate(b.iExpiryDate),23) end [ExpiryDate],      
                        abs(fQuantity) Quantity,ISNULL(sNarration,'''') Narration,convert(varchar,dbo.IntToDate(h.iDate),23) [TransactionDate],      
                        case when muo.OutletTypeID=0 then ''Warehouse'' else ''VAN'' end FromOutletType,      
                        case when muto.OutletTypeID=0 then ''Warehouse'' else ''VAN'' end ToOutletType      
                        from tCore_Header_0 h       
                        join tCore_Data_0 d on h.iHeaderId=d.iHeaderId      
                        join tCore_Indta_0 i on i.iBodyId=d.iBodyId      
                        join tCore_HeaderData'+cast(@Des as varchar)+'_0 hd on hd.iHeaderId=h.iHeaderId      
                        join tCore_Data_Tags_0 dt on dt.iBodyId=d.iBodyId      
                        join mCore_From ouf on ouf.iMasterId=iTag'+@from+' and ouf.iStatus<>5    
                        join mCore_To outt on outt.iMasterId=iTag'+@to+'  and outt.iStatus<>5 
                        join m'+@Invtag+' ofr on ofr.sName=ouf.sName and ofr.iStatus<>5    
                        join mu'+@Invtag+' muo on muo.iMasterId=ofr.iMasterId    
                        join m'+@Invtag+' oto on oto.sName=outt.sName and oto.iStatus<>5    
                        join mu'+@Invtag+' muto on muto.iMasterId=oto.iMasterId    
                        join mCore_Units u on u.iMasterId=iUnit  and u.iStatus<>5      
                        left join tCore_Batch_0 b on b.iBodyId=d.iBodyId      
                        where iVoucherType='+cast(@Des as varchar)+' and sVoucherNo= ''{DocNo}'' '
                        exec (@qry);  ";
                        objreg.SetLog2("TransactionLog",  query);
                        List<StockDetailsData> lc = new List<StockDetailsData>();
                        DataSet ds = objreg.GetData(query, CompanyId, ref errors1);

                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            var CurrentDate = Convert.ToDateTime(row["TransactionDate"]).ToString();
                            StockDetailsData c = new StockDetailsData();
                            c.SourceStockCode = Convert.ToString(row["OutletFrom"]);
                            c.DestinationStockCode = Convert.ToString(row["OutletTo"]);
                            c.ProductId = Convert.ToInt32(row["iProduct"]);
                            c.ProductUnit = Convert.ToString(row["Productunit"]);
                            c.BatchId = Convert.ToString(row["sBatchNo"]);
                            if (Convert.ToString(row["ExpiryDate"]) == "0")
                            {
                                c.ExpiryDate = null;
                            }
                            else
                            {
                                c.ExpiryDate = Convert.ToString(row["ExpiryDate"]);
                            }
                            c.Qty = Convert.ToDecimal(row["Quantity"]);
                            c.TransactionDateTime = Convert.ToString(CurrentDate);
                            c.Comments = Convert.ToString(row["Narration"]);
                            if (Convert.ToString(row["FromOutletType"]) == "Warehouse" & Convert.ToString(row["ToOutletType"]) == "VAN")
                            {
                                c.Type = 2;
                            }
                            else if (Convert.ToString(row["FromOutletType"]) == "VAN" & Convert.ToString(row["ToOutletType"]) == "Warehouse")
                            {
                                c.Type = 3;
                            }
                            else if (Convert.ToString(row["FromOutletType"]) == "VAN" & Convert.ToString(row["ToOutletType"]) == "VAN")
                            {
                                c.Type = 6;
                            }
                            lc.Add(c);
                        }
                        #endregion

                        clist.ItemList = lc;

                        if (clist.ItemList.Count() > 0)
                        {
                            #region PostingSection

                            var sContent = new JavaScriptSerializer().Serialize(clist);
                            using (WebClient client = new WebClient())
                            {
                                client.Headers.Add("Content-Type", "application/json");
                                objreg.SetSuccessLog("TransactionLog.log", "Mobile Request-Others Posted sContent: " + sContent);
                                objreg.SetLog2("TransactionLog", "Mobile Request-Others Posted sContent: " + sContent);

                                objreg.SetLog2("TransactionLog", " PostingURL :" + PostingURL);
                                var arrResponse = client.UploadString(PostingURL, sContent);
                                var lng = JsonConvert.DeserializeObject<Result>(arrResponse);

                                if (lng.ResponseStatus.IsSuccess == true)
                                {
                                    foreach (var item in clist.ItemList)
                                    {
                                        FConvert.LogFile("TransactionLog.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Voucher:- " + DocNo);
                                    }
                                    Message = "Posted Successfully";
                                }
                                else
                                {
                                    int ErrorListCount = lng.ErrorMessages.Count();
                                    if (ErrorListCount > 0)
                                    {
                                        var ErrorList = lng.ErrorMessages.ToList();
                                        foreach (var item in ErrorList)
                                        {
                                            objreg.SetErrorLog("TransactionLog.log", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                            objreg.SetLog2("TransactionLog", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                        }
                                    }

                                    int FailedListCount = lng.FailedList.Count();
                                    if (FailedListCount > 0)
                                    {
                                        var FailedList = lng.FailedList.ToList();
                                        foreach (var item in FailedList)
                                        {
                                            objreg.SetErrorLog("TransactionLog.log", "FailedList Message for Voucher No '" + DocNo + "' as :" + item);
                                            objreg.SetLog2("TransactionLog", "FailedList Message for Voucher No '" + DocNo + "' as :" + item);
                                        }
                                    }
                                }
                            }

                            #endregion
                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                objreg.SetLog2("TransactionLog", "Exception Message for Voucher No '" + DocNo + "' as :" + Message);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult StockUpdation(int CompanyId, string SessionId, int LoginId, int vtype, string DocNo)
        {
            objreg.SetLog2("TransactionLog", "Posting Method");
            string errors1 = "";
            string Message = "";
            try
            {
                List<StockDetails> Listsd = new List<StockDetails>();
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog("Invalid Token");
                    objreg.SetLog2("TransactionLog", Message);
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog(sMessage);
                    objreg.SetLog2("TransactionLog", sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                StockList clist = new StockList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "warehouseStock";

                #region VoucherDetails
                string strsql = $@"select  i.iproduct,u.sCode productUnit,b.sBatchNo,case when b.iExpiryDate=0 then '0'  else convert(varchar,dbo.IntToDate(b.iExpiryDate),23) end [ExpiryDate],convert(varchar,dbo.IntToDate(h.iDate),23) [TransactionDate]  from tcore_header_0 h join tcore_data_0 d on d.iHeaderId=h.iheaderid
                                    join tcore_indta_0 i on i.ibodyid=d.ibodyid
                                    join mCore_Units u on u.iMasterId=iUnit    
                                    left join tCore_Batch_0 b on b.iBodyId=d.iBodyId 
                                    where h.ivouchertype={ vtype } and h.svoucherno='{ DocNo }'";
                objreg.SetLog2("TransactionLog", " strsql = " + strsql);
                DataSet ds = objreg.GetData(strsql, CompanyId, ref errors1);
                objreg.SetLog2("TransactionLog", " strsql error= " + errors1);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    string StockOutlet = ConfigurationManager.AppSettings["StockOutlet"];
                    objreg.SetLog2("TransactionLog", " StockOutlet= " + StockOutlet);
                    List<StockDetailsData> lc = new List<StockDetailsData>();
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow row = ds.Tables[0].Rows[i];
                        strsql = $@"declare @invtag varchar(100) = (select(SELECT sMasterName  FROM cCore_MasterDef WHERE iMasterTypeId = (SELECT iValue FROM cCore_PreferenceVal_0 WHERE iCategory = 0 and iFieldId = 1)))
                            if(@invtag = 'Outlet')
                            begin
                            set @invtag = 'Pos_'+@invtag;
                            end
                            else
                            begin
                            set @invtag = 'Core_'+@invtag;
                            end
                            exec('select mp.iMasterId, mp.sCode Product_Code, mp.sName Product_Name,sum(i.fQrec + i.fQiss) Quantity from vCore_ibals_0 i join mCore_Product mp on mp.iMasterId = i.iProduct where i.iproduct={ds.Tables[0].Rows[i]["iproduct"]}  and
                            i.iInvTag=(select iMasterId from m'+@invtag+' where sName = ''{StockOutlet}'') group by mp.iMasterId, mp.sCode , mp.sName')";

                        //select mp.iMasterId, mp.sCode Product_Code, mp.sName Product_Name, sum(i.fQrec + i.fQiss) Quantity from vCore_ibals_0 i join mCore_Product mp on mp.iMasterId = i.iProduct where i.iproduct ={ ds.Tables[0].Rows[i]["iproduct"]}
                        //group by mp.iMasterId, mp.sCode , mp.sName
                        objreg.SetLog2("TransactionLog", " dstock strsql = " + strsql);
                        DataSet dstock = objreg.GetData(strsql, CompanyId, ref errors1);
                        objreg.SetLog2("TransactionLog", " dstock strsql error= " + errors1);
                        StockDetailsData c = new StockDetailsData();
                        if (dstock.Tables.Count > 0)
                        {
                            if (dstock.Tables[0].Rows.Count>0)
                            {
                                if (Convert.ToDecimal(dstock.Tables[0].Rows[0]["Quantity"]) > 0)
                                {
                                    c.Qty = Convert.ToDecimal(dstock.Tables[0].Rows[0]["Quantity"]);
                                }
                                else
                                {
                                    c.Qty = 0;
                                }
                            }
                            else
                            {
                                c.Qty = 0;
                            }
                        }
                        else
                        {
                            dstock = objreg.GetData("select scode from mcore_product where imasterid=" + ds.Tables[0].Rows[i]["iproduct"] + "", CompanyId, ref errors1);
                            if (dstock.Tables[0].Rows.Count > 0)
                            {
                                c.Qty = 0;
                            }
                        }
                        var CurrentDate = Convert.ToDateTime(row["TransactionDate"]).ToString();

                        c.SourceStockCode = "";
                        c.DestinationStockCode = StockOutlet;
                        c.ProductId = Convert.ToInt32(row["iproduct"]);
                        c.ProductUnit = Convert.ToString(row["Productunit"]);
                        c.BatchId = row["sBatchNo"] == null?null:Convert.ToString(row["sBatchNo"]);
                        if (Convert.ToString(row["ExpiryDate"]) == "0")
                        {
                            c.ExpiryDate = null;
                        }
                        else
                        {
                            c.ExpiryDate = Convert.ToString(row["ExpiryDate"]);
                        }
                        c.TransactionDateTime = Convert.ToString(CurrentDate);
                        c.Comments = "";
                        c.Type = 7;
                        lc.Add(c);
                    }


                    clist.ItemList = lc;

                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            objreg.SetSuccessLog("TransactionLog.log", "Load Request Posted sContent: " + sContent);
                            objreg.SetLog2("TransactionLog", "Load Request Posted sContent: " + sContent);
                            objreg.SetLog2("TransactionLog", " PostingURL :" + PostingURL);
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<Result>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {
                                    FConvert.LogFile("TransactionLog.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Voucher:- " + DocNo);
                                }
                                Message = "Posted Successfully";
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        objreg.SetErrorLog("TransactionLog.log", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                        objreg.SetLog2("TransactionLog", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        objreg.SetErrorLog("TransactionLog.log", "FailedList Message for Voucher No '" + DocNo + "'");
                                        objreg.SetLog2("TransactionLog", "FailedList Message for Voucher No '" + DocNo + "'");
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                objreg.SetLog2("TransactionLog", "Exception Message for Voucher No '" + DocNo + "' as :" + Message);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ReceiptPosting(int CompanyId, string SessionId, int LoginId, int vtype, string DocNo)
        {
            objreg.SetLog2("TransactionLog", "Entered ReceiptPosting");
            SP(CompanyId);
            SPR(CompanyId);

            objreg.SetLog2("TransactionLog", "Posting Method");
            string errors1 = "";
            string Message = "";
            try
            {
               
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog("Invalid Token");
                    objreg.SetLog2("TransactionLog", Message);
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog(sMessage);
                    objreg.SetLog2("TransactionLog", sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                ReceiptList clist = new ReceiptList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "payment";

                #region VoucherDetails
                string  query = $@"declare @Base int;
                    set @Base = (select iVoucherType from cCore_Vouchers_0 where sName = 'Cash Receipts VAN');
                    declare @qry varchar(max);
                    set @qry = '
                    select h.sVoucherNo,r.mAmount mAmount1,'''' sChequeNo,'''' sNarration,'''' iDueDate,r.iRefType,r.iRef,ir.iRef iref2,ir.iRefType iRefType2,ih.sVoucherNo InvoiceNo from tCore_Header_0 h 
                    join tCore_Data_0 d on h.iHeaderId=d.iHeaderId
                    join tCore_Refrn_0 r on r.iBodyId= d.iBodyId and iRefType = 2
                    join tCore_Refrn_0 ir on r.iRef = ir.iRef and ir.iRefType <> 2
                    join tCore_Data_0 id on ir.iBodyId = id.iBodyId
                    join tCore_Header_0 ih on ih.iHeaderId = id.iHeaderId
                    where h.iVoucherType='+cast(@Base as varchar)+'  and h.sVoucherNo =''{DocNo}'''
                    exec (@qry);";
                
                objreg.SetLog2("TransactionLog", query);
                List<ReceiptItem> lc = new List<ReceiptItem>();
                DataSet ds = objreg.GetData(query, CompanyId, ref errors1);
                objreg.SetLog2("TransactionLog", "ds.Tables[0].Rows.Count = "+ ds.Tables[0].Rows.Count);
                if (ds.Tables[0].Rows.Count == 0)
                {
                    Message = "New Reference";
                    objreg.SetLog(Message);
                    objreg.SetLog2("TransactionLog", Message);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    var CurrentDate = DateTime.Now.ToString("yyyy-MM-dd");
                    ReceiptItem c = new ReceiptItem();
                    c.Amount = Convert.ToDecimal(row["mAmount1"]);
                    c.ChequeDate = Convert.ToString(row["iDueDate"]); ;
                    c.ChequeNo = Convert.ToString(row["sChequeNo"]); ;
                    c.MobileCode = Convert.ToString(row["sVoucherNo"]);
                    c.Note = Convert.ToString(row["sNarration"]);
                    c.InvoiceMobileCode = Convert.ToString(row["InvoiceNo"]);
                    lc.Add(c);
                }
                #endregion

                clist.ItemList = lc;

                if (clist.ItemList.Count() > 0)
                {
                    #region PostingSection

                    var sContent = new JavaScriptSerializer().Serialize(clist);
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("Content-Type", "application/json");
                        objreg.SetSuccessLog("TransactionLog.log", "Load Request Posted sContent: " + sContent);
                        objreg.SetLog2("TransactionLog", "Load Request Posted sContent: " + sContent);
                        objreg.SetLog2("TransactionLog", " PostingURL :" + PostingURL);
                        var arrResponse = client.UploadString(PostingURL, sContent);
                        var lng = JsonConvert.DeserializeObject<Result>(arrResponse);

                        if (lng.ResponseStatus.IsSuccess == true)
                        {
                            foreach (var item in clist.ItemList)
                            {
                                FConvert.LogFile("TransactionLog.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Voucher:- " + DocNo);
                            }
                            Message = "Posted Successfully";
                        }
                        else
                        {
                            int ErrorListCount = lng.ErrorMessages.Count();
                            if (ErrorListCount > 0)
                            {
                                var ErrorList = lng.ErrorMessages.ToList();
                                foreach (var item in ErrorList)
                                {
                                    objreg.SetErrorLog("TransactionLog.log", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                    objreg.SetLog2("TransactionLog", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                }
                            }

                            int FailedListCount = lng.FailedList.Count();
                            if (FailedListCount > 0)
                            {
                                var FailedList = lng.FailedList.ToList();
                                foreach (var item in FailedList)
                                {
                                    objreg.SetErrorLog("TransactionLog.log", "FailedList Message for Voucher No '" + DocNo + "'");
                                    objreg.SetLog2("TransactionLog", "FailedList Message for Voucher No '" + DocNo + "'");
                                }
                            }
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                objreg.SetLog2("TransactionLog", "Exception Message for Voucher No '" + DocNo + "' as :" + Message);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult PDCPosting(int CompanyId, string SessionId, int LoginId, int vtype, string DocNo)
        {
            objreg.SetLog2("TransactionLog", "Entered ReceiptPosting");
            SP(CompanyId);
            SPR(CompanyId);

            objreg.SetLog2("TransactionLog", "Posting Method");
            string errors1 = "";
            string Message = "";
            try
            {

                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog("Invalid Token");
                    objreg.SetLog2("TransactionLog", Message);
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog(sMessage);
                    objreg.SetLog2("TransactionLog", sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                ReceiptList clist = new ReceiptList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "payment";

                #region VoucherDetails
                string query = $@"declare @Base int;
                set @Base = (select iVoucherType from cCore_Vouchers_0 where sName = 'Post Dated Receipt VAN');
                declare @qry varchar(max);
                set @qry = '
                select h.sVoucherNo,r.mAmount mAmount1,hd.sChequeNo,hd.sNarration,dbo.IntToDate(d.iDueDate) iDueDate,ih.sVoucherNo InvoiceNo from tCore_Header_0 h 
                join tCore_Data_0 d on h.iHeaderId=d.iHeaderId
                join tCore_HeaderData'+cast(@Base as varchar)+'_0 hd on hd.iHeaderId = h.iHeaderId
                join tCore_Refrn_0 r on r.iBodyId= d.iBodyId and iRefType = 2
                join tCore_Refrn_0 ir on r.iRef = ir.iRef and ir.iRefType <> 2
                join tCore_Data_0 id on ir.iBodyId = id.iBodyId
                join tCore_Header_0 ih on ih.iHeaderId = id.iHeaderId
                where h.iVoucherType='+cast(@Base as varchar)+' and h.sVoucherNo =''{DocNo}'''
                exec (@qry);";
                objreg.SetLog2("TransactionLog", query);
                List<ReceiptItem> lc = new List<ReceiptItem>();
                DataSet ds = objreg.GetData(query, CompanyId, ref errors1);
                objreg.SetLog2("TransactionLog", "ds.Tables[0].Rows.Count = " + ds.Tables[0].Rows.Count);
                if (ds.Tables[0].Rows.Count == 0)
                {
                    Message = "New Reference";
                    objreg.SetLog(Message);
                    objreg.SetLog2("TransactionLog", Message);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    var CurrentDate = DateTime.Now.ToString("yyyy-MM-dd");
                    ReceiptItem c = new ReceiptItem();
                    c.Amount = Convert.ToDecimal(row["mAmount1"]);
                    c.ChequeDate = Convert.ToString(row["iDueDate"]); ;
                    c.ChequeNo = Convert.ToString(row["sChequeNo"]); ;
                    c.MobileCode = Convert.ToString(row["sVoucherNo"]);
                    c.Note = Convert.ToString(row["sNarration"]);
                    c.InvoiceMobileCode = Convert.ToString(row["InvoiceNo"]);
                    lc.Add(c);
                }
                #endregion

                clist.ItemList = lc;

                if (clist.ItemList.Count() > 0)
                {
                    #region PostingSection

                    var sContent = new JavaScriptSerializer().Serialize(clist);
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("Content-Type", "application/json");
                        objreg.SetSuccessLog("TransactionLog.log", "Load Request Posted sContent: " + sContent);
                        objreg.SetLog2("TransactionLog", "Load Request Posted sContent: " + sContent);
                        objreg.SetLog2("TransactionLog", " PostingURL :" + PostingURL);
                        var arrResponse = client.UploadString(PostingURL, sContent);
                        var lng = JsonConvert.DeserializeObject<Result>(arrResponse);

                        if (lng.ResponseStatus.IsSuccess == true)
                        {
                            foreach (var item in clist.ItemList)
                            {
                                FConvert.LogFile("TransactionLog.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Voucher:- " + DocNo);
                            }
                            Message = "Posted Successfully";
                        }
                        else
                        {
                            int ErrorListCount = lng.ErrorMessages.Count();
                            if (ErrorListCount > 0)
                            {
                                var ErrorList = lng.ErrorMessages.ToList();
                                foreach (var item in ErrorList)
                                {
                                    objreg.SetErrorLog("TransactionLog.log", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                    objreg.SetLog2("TransactionLog", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                }
                            }

                            int FailedListCount = lng.FailedList.Count();
                            if (FailedListCount > 0)
                            {
                                var FailedList = lng.FailedList.ToList();
                                foreach (var item in FailedList)
                                {
                                    objreg.SetErrorLog("TransactionLog.log", "FailedList Message for Voucher No '" + DocNo + "'");
                                    objreg.SetLog2("TransactionLog", "FailedList Message for Voucher No '" + DocNo + "'");
                                }
                            }
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                objreg.SetLog2("TransactionLog", "Exception Message for Voucher No '" + DocNo + "' as :" + Message);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult InvoicePosting(int CompanyId, string SessionId, int LoginId, int vtype, string DocNo)
        {
            objreg.SetLog2("TransactionLog", "Entered InvoicePosting");
            SP(CompanyId);
            SPR(CompanyId);

            objreg.SetLog2("TransactionLog", "Posting Method");
            string errors1 = "";
            string Message = "";
            try
            {
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog("Invalid Token");
                    objreg.SetLog2("TransactionLog", Message);
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog(sMessage);
                    objreg.SetLog2("TransactionLog", sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                InvoiceList clist = new InvoiceList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "invoice";

                #region VoucherDetails
                string query = $@"
                declare @Base int;
                set @Base = (select iVoucherType from cCore_Vouchers_0 where sName = 'Sales Invoice - VAN');
                declare @qry varchar(max);
                set @qry = '
                select h.sVoucherNo,sum(i.mGross) totalsales,sum(f.mval9) disc,sum(f.mVal14) nettotal,sum(f.mVal15) vatvalue,sum(f.mVal16) grandtotal from tCore_Header_0 h 
                join tCore_Data_0 d on h.iHeaderId=d.iHeaderId
                join tCore_Indta_0 i on i.iBodyId=d.iBodyId
                join tCore_IndtaBodyScreenData_0 f on f.iBodyId = d.iBodyId
                where iVoucherType='+cast(@Base as varchar)+' and  sVoucherNo=''{DocNo}''
                group by h.sVoucherNo,iVoucherType

                select h.sVoucherNo,i.mGross itemvalue,f.mval9 disc,f.mVal14 nettotal,f.mVal15 vatvalue,f.mVal16 grandtotal,i.mRate price,f.mval1 focqty,f.mval0 soldqty,i.iProduct,u.sCode unit,isnull(b.sBatchNo,'''')sBatchNo,case when b.sBatchNo is null then '''' else dbo.IntToDate(b.iExpiryDate) end iExpiryDate,tx.sCode taxcode from tCore_Header_0 h 
                join tCore_Data_0 d on h.iHeaderId=d.iHeaderId
                join tCore_Indta_0 i on i.iBodyId=d.iBodyId
                join tCore_Data_Tags_0 dt on dt.iBodyId=d.iBodyId
                join mCore_Units u on u.iMasterId=iUnit
                left join tCore_Batch_0 b on b.iBodyId=d.iBodyId
                join tCore_IndtaBodyScreenData_0 f on f.iBodyId = d.iBodyId
                join tCore_Data'+cast(@Base as varchar)+'_0 d0 on d0.iBodyId=d.iBodyId
                join mCore_TaxCode tx on tx.iMasterId = d0.TaxCode
                where iVoucherType='+cast(@Base as varchar)+' and  sVoucherNo=''{DocNo}'''
                exec (@qry);";
                List<InvoiceItem> lc = new List<InvoiceItem>();
                DataSet ds = objreg.GetData(query, CompanyId, ref errors1);
                List<InvoiceLineItems> _invlist = new List<InvoiceLineItems>();
                foreach (DataRow row in ds.Tables[1].Rows)
                {
                    InvoiceLineItems c = new InvoiceLineItems();
                    c.Discount = Convert.ToDecimal(row["disc"]);
                    c.OfferedQty = Convert.ToDecimal(row["focqty"]);
                    c.ProductId = Convert.ToInt32(row["iProduct"]);
                    c.ProductUnit = Convert.ToString(row["unit"]);
                    c.SoldQty = Convert.ToDecimal(row["soldqty"]);
                    c.ReturnedQty = 0;
                    c.ItemValue = Convert.ToDecimal(row["itemvalue"]);
                    c.Price = Convert.ToDecimal(row["price"]);
                    c.BatchId = Convert.ToString(row["sBatchNo"]);
                    c.ExpiryDate = Convert.ToString(row["iExpiryDate"]); 
                    c.TaxValue = Convert.ToDecimal(row["vatvalue"]);
                    c.TaxCategoryCode = Convert.ToString(row["taxcode"]);
                    _invlist.Add(c);
                }
                InvoiceItem inv = new InvoiceItem();
                DataRow dr = (DataRow) ds.Tables[0].Rows[0];
                inv.MobileCode = dr["sVoucherNo"].ToString();
                inv.TotalSales = Convert.ToDecimal(dr["totalsales"].ToString());
                inv.Discount = Convert.ToDecimal(dr["disc"].ToString());
                inv.NetTotal = Convert.ToDecimal(dr["nettotal"].ToString());
                inv.VatValue = Convert.ToDecimal(dr["vatvalue"].ToString());
                inv.GrandTotal = Convert.ToDecimal(dr["grandtotal"].ToString());
                inv.InvoiceLineItems = _invlist;
                lc.Add(inv);
                #endregion

                clist.ItemList = lc;

                if (clist.ItemList.Count() > 0)
                {
                    #region PostingSection

                    var sContent = new JavaScriptSerializer().Serialize(clist);
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("Content-Type", "application/json");
                        objreg.SetSuccessLog("TransactionLog.log", "Load Request Posted sContent: " + sContent);
                        objreg.SetLog2("TransactionLog", "Load Request Posted sContent: " + sContent);
                        objreg.SetLog2("TransactionLog", " PostingURL :" + PostingURL);
                        var arrResponse = client.UploadString(PostingURL, sContent);
                        var lng = JsonConvert.DeserializeObject<Result>(arrResponse);

                        if (lng.ResponseStatus.IsSuccess == true)
                        {
                            foreach (var item in clist.ItemList)
                            {
                                FConvert.LogFile("TransactionLog.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Voucher:- " + DocNo);
                            }
                            Message = "Posted Successfully";
                        }
                        else
                        {
                            int ErrorListCount = lng.ErrorMessages.Count();
                            if (ErrorListCount > 0)
                            {
                                var ErrorList = lng.ErrorMessages.ToList();
                                foreach (var item in ErrorList)
                                {
                                    objreg.SetErrorLog("TransactionLog.log", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                    objreg.SetLog2("TransactionLog", "ErrorList Message for Voucher No '" + DocNo + "' data as :- " + item);
                                }
                            }

                            int FailedListCount = lng.FailedList.Count();
                            if (FailedListCount > 0)
                            {
                                var FailedList = lng.FailedList.ToList();
                                foreach (var item in FailedList)
                                {
                                    objreg.SetErrorLog("TransactionLog.log", "FailedList Message for Voucher No '" + DocNo + "'");
                                    objreg.SetLog2("TransactionLog", "FailedList Message for Voucher No '" + DocNo + "'");
                                }
                            }
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                objreg.SetLog2("TransactionLog", "Exception Message for Voucher No '" + DocNo + "' as :" + Message);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }

        public string GetAccessToken()
        {
            string AccessToken = "";
            Datum datanum = new Datum();
            datanum.CompanyCode = CompanyCode;
            datanum.Identifier = Identifier;
            datanum.Secret = Secret;
            datanum.Lng = Lng;
            string sContent = JsonConvert.SerializeObject(datanum);
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                var sMessage = " AccessTokenURL :" + AccessTokenURL;
                objreg.SetLog2("TransactionLog", " AccessTokenURL :" + AccessTokenURL);
                var arrResponse = client.UploadString(AccessTokenURL, sContent);

                Resultlogin lng = JsonConvert.DeserializeObject<Resultlogin>(arrResponse);

                AccessToken = lng.AccessToken;
                if (lng.AccessToken == null || lng.AccessToken == "" || lng.AccessToken == "-1")
                {
                    return "";
                }
                else
                {
                }
            }

            return AccessToken;
        }

        public void SP(int CompanyId)
        {
            string errors1 = "";
            string StrSP = "SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[sp_GetStockTransferIssueVAN]')";
            DataSet dsSP = objreg.GetData(StrSP, CompanyId, ref errors1);
            if (dsSP == null || dsSP.Tables.Count == 0 || dsSP.Tables[0].Rows.Count == 0)
            {
                #region SP
                string StrAmt = string.Format(@"Create PROCEDURE [dbo].sp_GetStockTransferIssueVAN  @VoucherNo varchar(100)       
                                                as   
                                                begin  
                                                select sVoucherNo,iTransactionId,ouf.sCode [OutletFrom],outt.sCode [OutletTo],iProduct,u.sCode [Productunit] ,sBatchNo,  
                                                case when b.iExpiryDate=0 then '0'  else convert(varchar,dbo.IntToDate(b.iExpiryDate),23) end [ExpiryDate],  
                                                abs(fQuantity) Quantity,ISNULL(sNarration,'') Narration,convert(varchar,dbo.IntToDate(h.iDate),23) [TransactionDate],  
                                                case when muo.OutletTypeID=0 then 'Warehouse' else 'VAN' end FromOutletType,  
                                                case when muto.OutletTypeID=0 then 'Warehouse' else 'VAN' end ToOutletType  
                                                from tCore_Header_0 h   
                                                join tCore_Data_0 d on h.iHeaderId=d.iHeaderId  
                                                join tCore_Indta_0 i on i.iBodyId=d.iBodyId  
                                                join tCore_HeaderData5383_0 hd on hd.iHeaderId=h.iHeaderId  
                                                join tCore_Data_Tags_0 dt on dt.iBodyId=d.iBodyId  
                                                join mCore_From ouf on ouf.iMasterId=iTag3026 and ouf.iStatus<>5
                                                join mCore_To outt on outt.iMasterId=iTag3027  and outt.iStatus<>5
                                                join mCore_Warehouse ofr on ofr.sName=ouf.sName and ofr.iStatus<>5
                                                join mucore_Warehouse muo on muo.iMasterId=ofr.iMasterId
                                                join mCore_Warehouse oto on oto.sName=outt.sName and oto.iStatus<>5
                                                join mucore_Warehouse muto on muto.iMasterId=oto.iMasterId
                                                join mCore_Units u on u.iMasterId=iUnit  and u.iStatus<>5  
                                                left join tCore_Batch_0 b on b.iBodyId=d.iBodyId  
                                                where iVoucherType=5383 and sVoucherNo= @VoucherNo  
                                                end   ");
                #endregion
                int amt = objreg.GetExecute(StrAmt, CompanyId, ref errors1);
            }
            else
            {

            }
        }

        public void SPR(int CompanyId)
        {
            string errors1 = "";
            string StrSP = "SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[sp_GetStockTransferReceiptVAN]')";
            DataSet dsSP = objreg.GetData(StrSP, CompanyId, ref errors1);
            if (dsSP == null || dsSP.Tables.Count == 0 || dsSP.Tables[0].Rows.Count == 0)
            {
                #region SP
                string StrAmt = string.Format(@"Create PROCEDURE [dbo].sp_GetStockTransferReceiptVAN  @VoucherNo varchar(100)
                                                as 
                                                begin
                                                select iTransactionId,ouf.sCode [OutletFrom],outt.sCode [OutletTo],iProduct,u.sCode [Productunit] ,sBatchNo,
                                                case when b.iExpiryDate=0 then '0' else convert(varchar,dbo.IntToDate(b.iExpiryDate),23) end [ExpiryDate],
                                                abs(fQuantity) Quantity,ISNULL(sNarration,'') Narration,convert(varchar,dbo.IntToDate(h.iDate),23) [TransactionDate] from tCore_Header_0 h 
                                                join tCore_Data_0 d on h.iHeaderId=d.iHeaderId
                                                join tCore_Indta_0 i on i.iBodyId=d.iBodyId
                                                join tCore_HeaderData2055_0 hd on hd.iHeaderId=h.iHeaderId
                                                join tCore_Data_Tags_0 dt on dt.iBodyId=d.iBodyId
                                                join mCore_From ouf on ouf.iMasterId=iTag3026  and ouf.iStatus<>5
                                                join mCore_To outt on outt.iMasterId=iTag3027  and outt.iStatus<>5
                                                join mCore_Units u on u.iMasterId=iUnit
                                                left join tCore_Batch_0 b on b.iBodyId=d.iBodyId
                                                where iVoucherType=2055 and d.iTransactionId in(
                                                select ll.iTransactionId from tCore_Header_0 h join tCore_Data_0 d on h.iHeaderId=d.iHeaderId
                                                join tCore_Links_0 l on l.iTransactionId=d.iBodyId and bBase = 1 
                                                join tCore_Links_0 ll on ll.iRefId=l.iRefId and ll.bBase=0
                                                where iVoucherType=5383 and sVoucherNo=@VoucherNo)
                                                end
                                                GO");
                #endregion
                int amt = objreg.GetExecute(StrAmt, CompanyId, ref errors1);
            }
            else
            {

            }
        }
    }
}