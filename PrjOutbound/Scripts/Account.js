var baseUrl = 'http://localhost/PrjOutbound';
var companyId = 0;
var Code = "";
var CanPost = "";
var PlaceOfSupply = "";


function BeforeSaveFn() {
    debugger;
    Focus8WAPI.getGlobalValue("BeforeSaveCallBack", '*', 1);
}
function BeforeSaveCallBack(response) {
    debugger;
    companyId = response.data.CompanyId;
    Focus8WAPI.getFieldValue("AccountCallback", ["PlaceOfSupply", "PostToIdistributor"], 1, false, 111);
}

function AccountCallback(response1) {
    debugger;
    PlaceOfSupply = response1.data[0].FieldValue;
    CanPost = response1.data[1].FieldValue;
    if (CanPost.toLowerCase() == "yes") {
        if (PlaceOfSupply == 0) {
            alert("Place of Supply is Mandatory");
            return (false);
        }
        else {
            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
        }
    }
    else {
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
    }
}
function AfterSaveFn() {
    debugger;
    Focus8WAPI.getGlobalValue("AfterSaveCallBack", '*', 1);
}
function AfterSaveCallBack(response) {
    debugger;
    companyId = response.data.CompanyId;
    Focus8WAPI.getFieldValue("AcSaveCallback", ["sCode", "PostToIdistributor"], 1, false, 111);
}

function AcSaveCallback(response1) {
    debugger;
    Code = response1.data[0].FieldValue;
    CanPost = response1.data[1].FieldValue;
    if (CanPost.toLowerCase() == "yes") {
        debugger;
        $.ajax({
            url: baseUrl + "/Account/MasterPosting",
            type: "POST",
            data: { companyId: companyId, Code: Code },
            success: function (data) {
                debugger;
                if (data.Message != "Posted Successfully") {
                    alert(data.Message);
                }
                console.log(data.Message);
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
            },
            error: function (e) {
                debugger;
                console.log(e);
            }
        });
    }
    else {
        Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
    }
}
function WAAfterSaveFn() {
    debugger;
    Focus8WAPI.getGlobalValue("WAAfterSaveCallBack", '*', 1);
}
function WAAfterSaveCallBack(response) {
    debugger;
    companyId = response.data.CompanyId;
    Focus8WAPI.getFieldValue("AcSaveCallback", ["sCode"], 1, false, 111);
}

function WAAcSaveCallback(response1) {
    debugger;
    Code = response1.data[0].FieldValue;
    debugger;
    $.ajax({
        url: baseUrl + "/Account/WorkingAreaPosting",
        type: "POST",
        data: { companyId: companyId, Code: Code },
        success: function (data) {
            debugger;
            if (data.Message != "Posted Successfully") {
                alert(data.Message);
            }
            console.log(data.Message);
            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
        },
        error: function (e) {
            debugger;
            console.log(e);
        }
    });
}
function CCAfterSaveFn() {
    debugger;
    Focus8WAPI.getGlobalValue("CCAfterSaveCallBack", '*', 1);
}
function CCAfterSaveCallBack(response) {
    debugger;
    companyId = response.data.CompanyId;
    Focus8WAPI.getFieldValue("CCSaveCallback", ["sCode"], 1, false, 111);
}

function CCSaveCallback(response1) {
    debugger;
    Code = response1.data[0].FieldValue;
    debugger;
    $.ajax({
        url: baseUrl + "/Account/CustomerCatPosting",
        type: "POST",
        data: { companyId: companyId, Code: Code },
        success: function (data) {
            debugger;
            if (data.Message != "Posted Successfully") {
                alert(data.Message);
            }
            console.log(data.Message);
            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.MASTER, true);
        },
        error: function (e) {
            debugger;
            console.log(e);
        }
    });
}

