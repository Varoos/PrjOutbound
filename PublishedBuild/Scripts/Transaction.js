var baseUrl = 'http://localhost/PrjOutbound';
var loginDetails = {};
var docNo = "0";
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

function openPopupCallBack(objWrapperResult) {
    try {
        debugger;
        console.log(objWrapperResult);
    }
    catch (err) {
        alert("Exception :: openPopupCallBack :" + err.message);
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true);
    }
}

function setFieldCallback(res) {
    console.log(JSON.stringify(res));
}

function openPopupCallBackBeforeSave(objWrapperResult) {
    try {
        debugger;
        console.log(objWrapperResult);
    }
    catch (err) {
        alert("Exception :: openPopupCallBackBeforeSave :" + err.message);
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true);
    }
}

function AfterSave() {
    debugger
    Focus8WAPI.getFieldValue("setAfterCallback", ["", "DocNo"], Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false, requestId);
}

function setAfterCallback(response) {
    debugger
    console.log(response);
    logDetails = response.data[0];
    docNo = response.data[1].FieldValue;
    $.ajax({
        url: baseUrl + "/Transaction/Posting",
        type: "POST",
        datatype: 'JSON',
        data: { "CompanyId": logDetails.CompanyId, "SessionId": logDetails.SessionId, "LoginId": logDetails.LoginId, "vtype": logDetails.iVoucherType, "DocNo": docNo },

        success: function (response) {
            debugger
            console.log(response);
            if (response.Message == "Posted Successfully") {
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else if (response.Message == "Invalid Token") {
                alert(response.Message);
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else {
                alert("Posting Failed.Please Check the Log");
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false)
            }
        },
        error: function (error) {
            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false)
            console.log('Error :: ', error);
        }
    });
}

function InvoiceAfterSave() {
    debugger
    Focus8WAPI.getFieldValue("setInvoiceCallback", ["", "DocNo"], Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false, requestId);
}

function setInvoiceCallback(response) {
    debugger
    console.log(response);
    logDetails = response.data[0];
    docNo = response.data[1].FieldValue;
    $.ajax({
        url: baseUrl + "/Transaction/InvoicePosting",
        type: "POST",
        datatype: 'JSON',
        data: { "CompanyId": logDetails.CompanyId, "SessionId": logDetails.SessionId, "LoginId": logDetails.LoginId, "vtype": logDetails.iVoucherType, "DocNo": docNo },

        success: function (response) {
            debugger
            console.log(response);
            if (response.Message == "Posted Successfully") {
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else if (response.Message == "Invalid Token") {
                alert(response.Message);
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else {
                alert("Posting Failed.Please Check the Log");
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false)
            }
        },
        error: function (error) {
            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false)
            console.log('Error :: ', error);
        }
    });
}

function ReceiptAfterSave() {
    debugger
    Focus8WAPI.getFieldValue("setReceiptCallback", ["", "DocNo"], Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false, requestId);
}

function setReceiptCallback(response) {
    debugger
    console.log(response);
    logDetails = response.data[0];
    docNo = response.data[1].FieldValue;
    $.ajax({
        url: baseUrl + "/Transaction/ReceiptPosting",
        type: "POST",
        datatype: 'JSON',
        data: { "CompanyId": logDetails.CompanyId, "SessionId": logDetails.SessionId, "LoginId": logDetails.LoginId, "vtype": logDetails.iVoucherType, "DocNo": docNo },

        success: function (response) {
            debugger
            console.log(response);
            if (response.Message == "Posted Successfully") {
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else if (response.Message == "Invalid Token") {
                alert(response.Message);
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else if (response.Message == "New Reference") {
                alert('Invoice shall be re-linked');
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else {
                alert("Posting Failed.Please Check the Log");
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false)
            }
        },
        error: function (error) {
            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false)
            console.log('Error :: ', error);
        }
    });
}

function PDCAfterSave() {
    debugger
    Focus8WAPI.getFieldValue("setPDCCallback", ["", "DocNo"], Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false, requestId);
}

function setPDCCallback(response) {
    debugger
    console.log(response);
    logDetails = response.data[0];
    docNo = response.data[1].FieldValue;
    $.ajax({
        url: baseUrl + "/Transaction/PDCPosting",
        type: "POST",
        datatype: 'JSON',
        data: { "CompanyId": logDetails.CompanyId, "SessionId": logDetails.SessionId, "LoginId": logDetails.LoginId, "vtype": logDetails.iVoucherType, "DocNo": docNo },

        success: function (response) {
            debugger
            console.log(response);
            if (response.Message == "Posted Successfully") {
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else if (response.Message == "Invalid Token") {
                alert(response.Message);
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else if (response.Message == "New Reference") {
                alert('Invoice shall be re-linked');
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true)
            }
            else {
                alert("Posting Failed.Please Check the Log");
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false)
            }
        },
        error: function (error) {
            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false)
            console.log('Error :: ', error);
        }
    });
}

