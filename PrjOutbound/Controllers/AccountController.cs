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
    public class AccountController : Controller
    {
        string errors1 = "";
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
        // GET: Employee
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult MasterPosting(int CompanyId, string Code)
        {
            string Message = "";
            try
            {
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog("Customer.log", "Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog("Customer.log", sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                MenuAccount.AccountList clist = new MenuAccount.AccountList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "customer";

                string sql = $@"exec pCore_CommonSp @Operation=getOneAcToPost, @p2='{Code}'";
                objreg.SetLog("Customer.log", " sql query :" + sql);
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuAccount.Account> lc = new List<MenuAccount.Account>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuAccount.Account c = new MenuAccount.Account();
                        c.FldIntegrationPartyId = Convert.ToInt32(row["iMasterId"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldCustomerCode = Convert.ToString(row["sCode"]);
                        c.FldContactPerson = Convert.ToString(row["ContactPerson"]);
                        c.FldAddress = Convert.ToString(row["sAddress"]);
                        c.FldMobilePhone = Convert.ToString(row["MobileNo"]);
                        c.FldTelephone = Convert.ToString(row["sTelNo"]);
                        c.FldEmail = Convert.ToString(row["sEMail"]);
                        c.FldShipToName = Convert.ToString(row["ShipToName"]);
                        c.FldShipToAddress = Convert.ToString(row["ShipToAddress"]);
                        c.FldShipToEmail = Convert.ToString(row["ShipToEmail"]);
                        c.FldShipToTel = Convert.ToString(row["ShipToTel"]);
                        c.FldTrn = Convert.ToString(row["TRN"]);
                        c.FldCreditDays = Convert.ToInt32(row["iCreditDays"]);
                        c.FldCreditLimit = Convert.ToDecimal(row["fCreditLimit"]);
                        c.FldCreditCategory = Convert.ToInt32(row["CreditCategory"]);
                        c.FldPlaceOfSupply = Convert.ToString(row["PlaceOfSupply"]);
                        c.FldBranchId = 0;
                        c.FldCategoryId = Convert.ToInt32(row["CustomerCategory"]);
                        c.FldActivityId = Convert.ToString(row["ActivityCode"]);
                        //c.FldAdminAreaId = Convert.ToString(row["CityCode"]);
                        c.FldWorkingAreaCode = Convert.ToString(row["WorkingArea"]);
                        c.FldPriceBookId = 3;
                        c.FldCurrencyId = 7;
                        //c.FldRouteId = null;
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection
                        JavaScriptSerializer jsJson = new JavaScriptSerializer();
                        jsJson.MaxJsonLength = 2147483647;
                        var sContent = jsJson.Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            objreg.SetSuccessLog("Customer.log", "New Posted sContent: " + sContent);
                            objreg.SetLog("Customer.log", " PostingURL :" + PostingURL);
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuProduct.ProductResult>(arrResponse);
                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                Message = "Posted Successfully";
                                objreg.SetLog("Customer.log", " Posted Successfully");
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        objreg.SetErrorLog("Customer.log", "ErrorList Message for Account Master  :- " + item);
                                        Message = Message+item.Substring(item.IndexOf("Error:"));
                                    }
                                }
                                Message = Message + " Posting Failed";
                                objreg.SetLog("Customer.log", Message + " Posting Failed");
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        Message = "Posting Failed";
                        objreg.SetLog("Customer.log", " Posting Failed. No data found");
                    }
                }
            }
            catch (Exception e)
            {
                errors1 = e.Message;
                Message = e.Message;
                objreg.SetErrorLog("Customer.log", " Error 1 :" + errors1);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult WorkingAreaPosting(int CompanyId, string Code)
        {
            string Message = "";
            try
            {
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog("Customer.log", "Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog("Customer.log", sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                WorkingArea.MenuWorkingAreaList clist = new WorkingArea.MenuWorkingAreaList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "workingArea";

                string sql = $@"exec pCore_CommonSp @Operation=getOneWorkingArea, @p2='{Code}'";
                objreg.SetLog("Customer.log", " sql query :" + sql);
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<WorkingArea.MenuWorkingArea> lc = new List<WorkingArea.MenuWorkingArea>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        WorkingArea.MenuWorkingArea c = new WorkingArea.MenuWorkingArea();
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.FldAdminAreaId = Convert.ToString(row["CityCode"]);
                        c.FldBranchId = Convert.ToInt32(row["Fatagid"]);
                        c.FldRouteId = Convert.ToInt32(row["Salesman"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection
                        JavaScriptSerializer jsJson = new JavaScriptSerializer();
                        jsJson.MaxJsonLength = 2147483647;
                        var sContent = jsJson.Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            objreg.SetSuccessLog("Customer.log", "New Posted sContent: " + sContent);
                            objreg.SetLog("Customer.log", " PostingURL :" + PostingURL);
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuProduct.ProductResult>(arrResponse);
                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                Message = "Posted Successfully";
                                objreg.SetLog("Customer.log", " Posted Successfully");
                            }
                            else
                            {
                                Message = "Posting Failed";
                                objreg.SetLog("Customer.log", " Posting Failed");
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        Message = "Posting Failed";
                        objreg.SetLog("Customer.log", " Posting Failed. No data found");
                    }
                }
            }
            catch (Exception e)
            {
                errors1 = e.Message;
                Message = e.Message;
                objreg.SetErrorLog("Customer.log", " Error 1 :" + errors1);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult CustomerCatPosting(int CompanyId, string Code)
        {
            string Message = "";
            int isGroup = 0;
            try
            {
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog("Customer.log", "Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog("Customer.log", sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                CategoryList clist = new CategoryList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "category";

                string sql = $@"select * from mCore_CustomerCategory where sCode='{Code}'";
                objreg.SetLog("Customer.log", " sql query :" + sql);
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<Category> lc = new List<Category>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Category c = new Category();
                        c.FldId = Convert.ToInt32(row["iMasterId"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.FldType = 1;
                        isGroup = Convert.ToInt32(row["bGroup"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (isGroup == 0)
                    {
                        if (clist.ItemList.Count() > 0)
                        {
                            #region PostingSection
                            JavaScriptSerializer jsJson = new JavaScriptSerializer();
                            jsJson.MaxJsonLength = 2147483647;
                            var sContent = jsJson.Serialize(clist);
                            using (WebClient client = new WebClient())
                            {
                                client.Headers.Add("Content-Type", "application/json");
                                objreg.SetSuccessLog("Customer.log", "New Posted sContent: " + sContent);
                                objreg.SetLog("Customer.log", " PostingURL :" + PostingURL);
                                var arrResponse = client.UploadString(PostingURL, sContent);
                                var lng = JsonConvert.DeserializeObject<MenuProduct.ProductResult>(arrResponse);
                                if (lng.ResponseStatus.IsSuccess == true)
                                {
                                    Message = "Posted Successfully";
                                    objreg.SetLog("Customer.log", " Posted Successfully");
                                }
                                else
                                {
                                    Message = "Posting Failed";
                                    objreg.SetLog("Customer.log", " Posting Failed");
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            Message = "Posting Failed";
                            objreg.SetLog("Customer.log", " Posting Failed. No data found");
                        }
                    }
                    else
                    {
                        Message = "Data is Group";
                    }
                }
            }
            catch (Exception e)
            {
                errors1 = e.Message;
                Message = e.Message;
                objreg.SetErrorLog("Customer.log", " Error 1 :" + errors1);
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
                objreg.SetLog("Customer.log", " AccessTokenURL :" + AccessTokenURL);
                var arrResponse = client.UploadString(AccessTokenURL, sContent);
                Resultlogin lng = JsonConvert.DeserializeObject<Resultlogin>(arrResponse);

                AccessToken = lng.AccessToken;
                if (lng.AccessToken == null || lng.AccessToken == "" || lng.AccessToken == "-1")
                {
                    return AccessToken;
                }
                else
                {
                }
            }

            return AccessToken;
        }

        public static int GetDateToInt(DateTime dt)
        {
            int val;
            val = Convert.ToInt16(dt.Year) * 65536 + Convert.ToInt16(dt.Month) * 256 + Convert.ToInt16(dt.Day);
            return val;
        }

        public Int64 GetDateTimetoInt(DateTime dt)
        {
            Int64 val;
            val = Convert.ToInt64(dt.Year) * 8589934592 + Convert.ToInt64(dt.Month) * 33554432 + Convert.ToInt64(dt.Day) * 131072 + Convert.ToInt64(dt.Hour) * 4096 + Convert.ToInt64(dt.Minute) * 64 + Convert.ToInt64(dt.Second);
            return val;
        }
    }
}