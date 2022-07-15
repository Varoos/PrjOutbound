﻿var baseUrl = 'http://localhost/PrjOutbound';
var loginDetails = {};
var CompId = 0;
var SessionId = "";
var docNo = "0";
var iMasterId = "0";
var Name = "";
var Code = "";
var logDetails = {};
var selectedRow = 1;
var requestIds = [];
var requestsProcessed = [];
var requestId = 0;
var sAbbr = "";
var bodyRequests = [];
var requestId = 1;
var requestsProcessed = [];

function isRequestCompleted(iRequestId, processedRequestsArray) {
    return processedRequestsArray.indexOf(iRequestId) === -1 ? false : true;
}

function isRequestProcessed(iRequestId) {
    for (let i = 0; i < requestsProcessed.length; i++) {
        if (requestsProcessed[i] == iRequestId) {
            return true;
        }
    } return false;
}

function LoadCategoryMaster(obj, a, b) {
    debugger
    CompId = obj.CompanyId;
    SessionId = obj.SessionId;
    GetSetActMasterFields();
}

function GetSetActMasterFields() {
    try {
        debugger
        iCounter = 0;
        iSessionId = '';
        iCompId = 0;

        sCode = '',
        sName = '',

        ArrHeaderReq = [];
        ArrHeaderReq.length = 0;
        ArrHeaderRes = [];
        ArrHeaderRes.length = 0;

        ArrHeaderReq.push(111);

        Focus8WAPI.getFieldValue("fnGetValueCallBackCategory", ["sName", "sCode"], 1, false, 111);
    }
    catch (err) {
        alert("Exception: " + err.message);
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
    }
}


function fnGetValueCallBackCategory(objWrapperResult) {
    try {
        debugger

        var responseData = objWrapperResult.data;
        if (responseData != null && responseData.length > 0) {
            if (objWrapperResult.requestType == 1) {
                if (objWrapperResult.iRequestId == 111) {
                    for (var dataindex = 0; dataindex < responseData.length; dataindex++) {
                        var objdata = responseData[dataindex];

                        if (objdata.sFieldName == "sCode") {
                            Code = objdata.FieldValue;
                        }

                        if (objdata.sFieldName == "sName") {
                            Name = objdata.FieldValue;
                        }
                    }

                    $.ajax({
                        url: baseUrl + "/Category/MasterPosting",
                        cache: false,
                        type: "POST",
                        datatype: 'JSON',
                        contenttype: 'application/json; charset=utf-8',
                        async: true,
                        data: { CompanyId: CompId, SessionId: SessionId, Name: Name, Code: Code },
                        success: function (data) {
                            debugger
                            if (data.Message == "Data Posted Successful") {
                                alert('Data posted / updated to mobile app device');
                                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
                            }
                            else if (data.Message == "Data is Group") {
                                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
                            }
                            else {
                                //alert('Data not posted / not updated to mobile app device. Please check the log in temp folder and resave again');
                                alert('Data Not Posted. Reason:- ' + data.Message);
                                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
                            }
                        },
                        error: function () {
                            alert("Error occured while fetching data!");
                            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, false);
                        }
                    });
                }
            }
        }
    }
    catch (err) {
        alert("Exception:  " + err.message);
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
    }
}

function DeleteCategoryMaster(obj, a, b) {
    debugger
    CompId = obj.CompanyId;
    SessionId = obj.SessionId;
    DeleteGetSetActMasterFields();
}

function DeleteGetSetActMasterFields() {
    try {
        debugger
        iCounter = 0;
        iSessionId = '';
        iCompId = 0;

        sCode = '',
        sName = '',

        ArrHeaderReq = [];
        ArrHeaderReq.length = 0;
        ArrHeaderRes = [];
        ArrHeaderRes.length = 0;

        ArrHeaderReq.push(111);

        Focus8WAPI.getFieldValue("fnGetValueCallBackDeleteCategory", ["sName", "sCode"], 1, false, 111);
    }
    catch (err) {
        alert("Exception: " + err.message);
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
    }
}

function fnGetValueCallBackDeleteCategory(objWrapperResult) {
    try {
        debugger

        var responseData = objWrapperResult.data;
        if (responseData != null && responseData.length > 0) {
            if (objWrapperResult.requestType == 1) {
                if (objWrapperResult.iRequestId == 111) {
                    for (var dataindex = 0; dataindex < responseData.length; dataindex++) {
                        var objdata = responseData[dataindex];

                        if (objdata.sFieldName == "sCode") {
                            Code = objdata.FieldValue;
                        }

                        if (objdata.sFieldName == "sName") {
                            Name = objdata.FieldValue;
                        }
                    }

                    $.ajax({
                        url: baseUrl + "/Category/MasterDelete",
                        cache: false,
                        type: "POST",
                        datatype: 'JSON',
                        contenttype: 'application/json; charset=utf-8',
                        async: true,
                        data: { CompanyId: CompId, SessionId: SessionId, Name: Name, Code: Code },
                        success: function (data) {
                            if (data.ResponseStatus == true) {
                                alert(data.Message);
                                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
                            }
                            else {
                                //alert('Data not posted / not updated to mobile app device. Please check the log in temp folder and resave again');
                                alert('Data Not Posted. Reason:- '+ data.Message);
                                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
                            }
                        },
                        error: function () {
                            alert("Error occured while fetching data!");
                            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, false);
                        }
                    });
                }
            }
        }
    }
    catch (err) {
        alert("Exception:  " + err.message);
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
    }
}


function BeforeDelete() {
    debugger
    Focus8WAPI.getFieldValue("CategoryDataCallbackBeforeDelete", ["sName", "sCode", "iMasterId"], Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, false, 111);
}

function CategoryDataCallbackBeforeDelete(response1) {
    debugger;
    try {
        debugger;
        Name = response1.data[0].FieldValue;
        Code = response1.data[1].FieldValue;
        iMasterId = response1.data[2].FieldValue;
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
    } catch (e) {
        console.log("headerCallback", e.message)
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
    }
}

function CategoryDelete() {
    debugger;
    Focus8WAPI.getGlobalValue("CategoryCallbackDelete", '*', 1);
}

function CategoryCallbackDelete(response) {
    debugger;
    companyId = response.data.CompanyId;
    sessionId = response.data.SessionId;

    $.ajax({
        url: baseUrl + "/Category/MasterDelete",
        type: "POST",
        data: { companyId: companyId, Name: Name, Code: Code },
        success: function (data) {
            debugger;
            if (data.ResponseStatus == true) {
                alert(data.Message);
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
            }
            else {
                alert('Data not posted / not updated to mobile app device. Please check the log in temp folder and resave again');
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
            }
        },
        error: function (e) {
            debugger;
            console.log(e);
        }
    });
}