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
    public class BranchController : Controller
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
        string file = "Branch.log";
        BL_Registry objreg = new BL_Registry();
        // GET: Branch
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

                BranchList clist = new BranchList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "branch";
                string Tags = objreg.GetTagName(CompanyId,ref errors1, file);
                objreg.SetLog(file, Tags);
                string fatag = Tags.Split(',')[1];
                string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate,cm.bGroup
                                from mCore_{fatag} cm
                                join muCore_{fatag} mucm on cm.iMasterId=mucm.iMasterId
                                join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
                                where cm.iMasterId>0 and cm.iStatus<>5  and  cm.sName='{Name}' and cm.sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<Branch> lc = new List<Branch>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    Branch c = new Branch();
                    c.FldId = Convert.ToInt32(row["iMasterId"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldAddress = Convert.ToString(row["Address"]);
                    c.FldTelephone = Convert.ToString(row["Telephone"]);
                    c.FldTaxRegisterationNumber = Convert.ToString(row["TRN"]);
                    c.FldJurisdiction = Convert.ToString(row["Jurisdiction"]);
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
                        var lng = JsonConvert.DeserializeObject<BranchResult>(arrResponse);

                        if (lng.ResponseStatus.IsSuccess == true)
                        {
                            int res = 0;
                            if (CreatedDate == ModifiedDate)
                            {
                                string UpSql = $@"update muCore_{fatag} set Posted=1,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldId}";
                                res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                            }
                            else
                            {
                                string UpSql = $@"update muCore_{fatag} set Posted=2,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldId}";
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
                                objreg.SetLog(file, "Error Message for Branch Master:" + item);
                                //FConvert.LogFile(file, DateTime.Now.ToString() + "Error Message for Branch Master:" + item);

                                string UpSql = $@"update muCore_{fatag} set Posted=2,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldId}";
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
                                    objreg.SetLog(file, "FailedList Message for Branch Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                    //FConvert.LogFile(file, DateTime.Now.ToString() + "FailedList Message for Branch Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                    var FieldMasterId = item.FldId;
                                   // Message = "Data not posted";
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

                DeleteBranchList clist = new DeleteBranchList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "branch";
                string Tags = objreg.GetTagName(companyId, ref errors1, file);
                objreg.SetLog(file, Tags);
                string fatag = Tags.Split(',')[1];
                string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate
                                from mCore_{fatag} cm
                                join muCore_{fatag} mucm on cm.iMasterId=mucm.iMasterId
                                join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
                                where cm.iMasterId>0  and cm.bGroup=0 and cm.sName='{Name}' and cm.sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, companyId, ref errors1);
                List<DeleteBranch> lc = new List<DeleteBranch>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    DeleteBranch c = new DeleteBranch();
                    c.FldId = Convert.ToInt32(row["iMasterId"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldAddress = Convert.ToString(row["Address"]);
                    c.FldTelephone = Convert.ToString(row["Telephone"]);
                    c.FldTaxRegisterationNumber = Convert.ToString(row["TRN"]);
                    c.FldJurisdiction = Convert.ToString(row["Jurisdiction"]);
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
                    var lng = JsonConvert.DeserializeObject<DeleteBranchResult>(arrResponse);

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
                            objreg.SetLog(file, "Error Message for Employee Master:" + item);
                            //FConvert.LogFile(file, DateTime.Now.ToString() + "Error Message for Employee Master:" + item);
                            Message = item;
                        }

                        int FailedListCount = lng.FailedList.Count();
                        if (FailedListCount > 0)
                        {
                            var FailedList = lng.FailedList.ToList();
                            foreach (var item in FailedList)
                            {
                                objreg.SetLog(file, "FailedList Message for Employee Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                //FConvert.LogFile(file, DateTime.Now.ToString() + "FailedList Message for Employee Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
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