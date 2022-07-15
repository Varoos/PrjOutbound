﻿using Focus.Common.DataStructs;
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
    public class OutletController : Controller
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
        string file = "Outlet.log";
        BL_Registry objreg = new BL_Registry();

        // GET: Outlet
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult MasterPosting(int CompanyId, string SessionId, string Name, string Code)
        {
            string Message = "";
            int isGroup = 0;
            long CreatedDate = 0;
            long ModifiedDate = 0;
            int strdate = GetDateToInt(DateTime.Now);

            try
            {
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog(file, "Invalid Token");
                    //FConvert.LogFile(file, DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog(file, sMessage);
                    //FConvert.LogFile(file, DateTime.Now.ToString() + " : " + sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                OutletList clist = new OutletList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "warehouse";
                string Tags = objreg.GetTagName(CompanyId, ref errors1, file);
                objreg.SetLog(file, Tags);
                string InvTag = Tags.Split(',')[0];
                string InvType = InvTag.Trim().ToLower() == "outlet".Trim().ToLower() ? "Pos" : "core";
                string tbl = InvType + "_" + InvTag;
                string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
                                Employee,iCreatedDate,iModifiedDate,bGroup from m{tbl} o
                                join mu{tbl} mo on mo.iMasterId=o.iMasterId
                                where o.iMasterId>0 and iStatus<>5 and o.sName='{Name}' and o.sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<Outlet> lc = new List<Outlet>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    Outlet c = new Outlet();
                    c.FldId = Convert.ToInt32(row["iMasterId"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
                    c.FldBranchId = Convert.ToInt32(row["OutletId"]);
                    c.FldType = Convert.ToInt32(row["OutletTypeID"]);
                    c.FldPlateNumber = Convert.ToString(row["PlateNumber"]);
                    c.FldRegistrationDate = Convert.ToString(row["RegistrationDate"]);
                    c.FldRouteId = Convert.ToInt32(row["Employee"]);
                    isGroup = Convert.ToInt32(row["bGroup"]);
                    CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                    ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                    lc.Add(c);
                }
                clist.ItemList = lc;
                if (isGroup == 0)
                {
                    var sContent = new JavaScriptSerializer().Serialize(clist);
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("Content-Type", "application/json");
                        objreg.SetLog(file, " PostingURL :" + PostingURL);
                        var arrResponse = client.UploadString(PostingURL, sContent);
                        var lng = JsonConvert.DeserializeObject<OutletResult>(arrResponse);

                        if (lng.ResponseStatus.IsSuccess == true)
                        {
                            int res = 0;
                            if (CreatedDate == ModifiedDate)
                            {
                                string UpSql = $@"update mu{tbl} set Posted=1,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldId}";
                                res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                            }
                            else
                            {
                                string UpSql = $@"update mu{tbl} set Posted=2,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldId}";
                                res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                            }
                            if (res == 1)
                            {
                                objreg.SetLog(file, "Data posted / updated to mobile app device");
                                //FConvert.LogFile(file, DateTime.Now.ToString() + "Data posted / updated to mobile app device");
                            }
                            Message = "Data Posted Successful";
                        }
                        else
                        {
                            var ErrorMessagesList = lng.ErrorMessages.ToList();
                            foreach (var item in ErrorMessagesList)
                            {
                                objreg.SetLog(file, "Error Message for Outlet Master:" + item);
                                //FConvert.LogFile(file, DateTime.Now.ToString() + "Error Message for Outlet Master:" + item);

                                string UpSql = $@"update mu{tbl} set Posted=2 where iMasterId={clist.ItemList.First().FldId}";
                                int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                if (res == 1)
                                {
                                    objreg.SetLog(file, "Data posted / updated to mobile app device");
                                    //FConvert.LogFile(file, DateTime.Now.ToString() + "Data posted / updated to mobile app device");
                                }
                                Message = item;
                            }

                            int FailedListCount = lng.FailedList.Count();
                            if (FailedListCount > 0)
                            {
                                var FailedList = lng.FailedList.ToList();
                                foreach (var item in FailedList)
                                {
                                    objreg.SetLog(file, "FailedList Message for Outlet Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                    //FConvert.LogFile(file, DateTime.Now.ToString() + "FailedList Message for Outlet Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                    var FieldMasterId = item.FldId;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Message = "Data is Group";
                }
                return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                errors1 = e.Message;
                Message = e.Message;
                objreg.SetLog(file, " Error :" + errors1);
                //FConvert.LogFile(file, DateTime.Now.ToString() + " Error :" + errors1);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult MasterDelete(int companyId, string Name, string Code)
        {
            string Message = "";
            bool ResponseStatus = false;
            long CreatedDate = 0;
            long ModifiedDate = 0;

            try
            {
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog(file, "Invalid Token");
                    //FConvert.LogFile(file, DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog(file, sMessage);
                    //FConvert.LogFile(file, DateTime.Now.ToString() + " : " + sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                DeleteOutletList clist = new DeleteOutletList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "warehouse";
                string Tags = objreg.GetTagName(companyId, ref errors1, file);
                objreg.SetLog(file, Tags);
                string InvTag = Tags.Split(',')[0];
                string InvType = InvTag.Trim().ToLower() == "outlet".Trim().ToLower() ? "Pos" : "core";
                string tbl = InvType + "_" + InvTag;
                string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
                                Employee,iCreatedDate,iModifiedDate from m{tbl} o
                                join mu{tbl} mo on mo.iMasterId=o.iMasterId
                                where o.iMasterId>0  and o.bGroup=0 and o.sName='{Name}' and o.sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, companyId, ref errors1);
                List<DeleteOutlet> lc = new List<DeleteOutlet>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    DeleteOutlet c = new DeleteOutlet();
                    c.FldId = Convert.ToInt32(row["iMasterId"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
                    c.FldBranchId = Convert.ToInt32(row["OutletId"]);
                    c.FldType = Convert.ToInt32(row["OutletTypeID"]);
                    c.FldPlateNumber = Convert.ToString(row["PlateNumber"]);
                    c.FldRegistrationDate = Convert.ToString(row["RegistrationDate"]);
                    c.FldRouteId = Convert.ToInt32(row["Employee"]);
                    c.FldIsDeleted = 1;
                    CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                    ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                    lc.Add(c);
                }
                clist.ItemList = lc;

                var sContent = new JavaScriptSerializer().Serialize(clist);
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json");
                    objreg.SetLog(file, "Delete PostingURL :" + PostingURL);
                    var arrResponse = client.UploadString(PostingURL, sContent);
                    var lng = JsonConvert.DeserializeObject<DeleteOutletResult>(arrResponse);

                    if (lng.ResponseStatus.IsSuccess == true)
                    {
                        Message = lng.ResponseStatus.StatusMsg;
                        ResponseStatus = lng.ResponseStatus.IsSuccess;
                        objreg.SetLog(file, "Delete Response" + Message);
                        //FConvert.LogFile(file, DateTime.Now.ToString() + "Delete Response" + Message);
                    }
                    else
                    {
                        objreg.SetLog(file, "Delete Failed Response" + lng.ResponseStatus.StatusMsg);
                        //FConvert.LogFile(file, DateTime.Now.ToString() + "Delete Failed Response" + lng.ResponseStatus.StatusMsg);

                        var ErrorMessagesList = lng.ErrorMessages.ToList();
                        foreach (var item in ErrorMessagesList)
                        {
                            Message = item;
                            objreg.SetLog(file, "Error Message for Outlet Master:" + item);
                            //FConvert.LogFile(file, DateTime.Now.ToString() + "Error Message for Employee Master:" + item);
                        }

                        int FailedListCount = lng.FailedList.Count();
                        if (FailedListCount > 0)
                        {
                            var FailedList = lng.FailedList.ToList();
                            foreach (var item in FailedList)
                            {
                                objreg.SetLog(file, "FailedList Message for Outlet Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                //FConvert.LogFile(file, DateTime.Now.ToString() + "FailedList Message for Outlet Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                var FieldMasterId = item.FldId;
                            }
                        }
                    }
                }

                return Json(new { Message = Message, ResponseStatus = ResponseStatus }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                errors1 = e.Message;
                Message = e.Message;
                objreg.SetLog(file, " Error :" + errors1);
                //FConvert.LogFile(file, DateTime.Now.ToString() + " Error :" + errors1);
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
                objreg.SetLog(file, " AccessTokenURL :" + AccessTokenURL);
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