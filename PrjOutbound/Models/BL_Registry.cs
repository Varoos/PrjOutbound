using Focus.Common.DataStructs;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Collections;

namespace PrjOutbound.Models
{
    public class BL_Registry
    {
        public DataSet GetData(string strSelQry, int CompId, ref string error)
        {
            error = "";
            try
            {
                Database obj = Focus.DatabaseFactory.DatabaseWrapper.GetDatabase(CompId);
                return (obj.ExecuteDataSet(CommandType.Text, strSelQry));
            }
            catch (Exception e)
            {
                error = e.Message;
                FConvert.LogFile(".log", "err : " + "[" + System.DateTime.Now + "] - GetData :" + error + "---" + strSelQry);
                return null;
            }
        }

        public int GetExecute(string strSelQry, int CompId, ref string error)
        {
            error = "";
            try
            {
                Database obj = Focus.DatabaseFactory.DatabaseWrapper.GetDatabase(CompId);
                return (obj.ExecuteNonQuery(CommandType.Text, strSelQry));
            }
            catch (Exception e)
            {
                error = e.Message;
                FConvert.LogFile(".log", DateTime.Now.ToString() + " GetExecute :" + error + "---" + strSelQry);
                return 0;
            }
        }
        public void SetLog(string content)
        {
            StreamWriter objSw = null;
            try
            {
                string sFilePath = System.IO.Path.GetTempPath()  + "AlSalamLog" + DateTime.Now.Date.ToString("ddMMyyyy") + ".txt";
                objSw = new StreamWriter(sFilePath, true);
                objSw.WriteLine(DateTime.Now.ToString() + " " + content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
            finally
            {
                if (objSw != null)
                {
                    objSw.Flush();
                    objSw.Dispose();
                }
            }
        }

        public void SetLog(string LogName, string content)
        {
            SetLog2(LogName, content);
        }

        public void SetSuccessLog(string LogName, string content)
        {
            SetLog2(LogName, content);
        }

        public void SetErrorLog(string LogName, string content)
        {
            SetLog2(LogName, content);
        }
        public void SetLog2(string LogName, string content)
        {
            string str = "Logs/" + LogName + ".txt";
            FileStream stream = new FileStream(AppDomain.CurrentDomain.BaseDirectory.ToString() + str, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter writer = new StreamWriter(stream);
            writer.BaseStream.Seek(0L, SeekOrigin.End);
            writer.WriteLine(DateTime.Now.ToString() + " - " + content);
            writer.Flush();
            writer.Close();
        }
        public class SaveMasterByNamesResult
        {
            public int lResult { get; set; }
            public string sValue { get; set; }
        }
        public class MRootObject
        {
            public SaveMasterByNamesResult SaveMasterByNamesResult { get; set; }
        }
        public class HashData
        {
            //public string url { get; set; }
            public List<Hashtable> data { get; set; }
            public int result { get; set; }
            public string message { get; set; }
        }
        public string GetTagName(int ccode, ref string err, string file )
        {
            string TagName = "";
            try
            {
                string qry = "select(SELECT sMasterName  FROM cCore_MasterDef WHERE iMasterTypeId = (SELECT iValue FROM cCore_PreferenceVal_0 WHERE iCategory = 0 and iFieldId = 1)) Invtag,(SELECT sMasterName FROM cCore_MasterDef WHERE iMasterTypeId = (SELECT iValue FROM cCore_PreferenceVal_0 WHERE iCategory = 0 and iFieldId = 0)) FinTag";
                SetLog(file, qry);
                string er = "";
                DataSet dss = GetData(qry, ccode,ref err);
                if (dss.Tables[0].Rows.Count > 0)
                {
                    TagName = dss.Tables[0].Rows[0]["Invtag"].ToString() + "," + dss.Tables[0].Rows[0]["FinTag"].ToString();
                }
            }
            catch (Exception ex)
            {
                SetLog(file, ex.Message);
            }
            return TagName;
        }
    }
}