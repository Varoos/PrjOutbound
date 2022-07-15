using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjOutbound.Models
{
    public class WorkingArea
    {
        public class MenuWorkingAreaList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<MenuWorkingArea> ItemList { get; set; }
        }

        public class MenuWorkingArea
        {
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public string FldAdminAreaId { get; set; }
            public int FldBranchId { get; set; }
            public int FldRouteId { get; set; }
        }
    }
}