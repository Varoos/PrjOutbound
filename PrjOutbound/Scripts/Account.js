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
    Focus8WAPI.getFieldValue("AccountCallback", ["PlaceOfSupply"], 1, false, 111);
}

function AccountCallback(response1) {
    debugger;
    PlaceOfSupply = response1.data[0].FieldValue;
    if (PlaceOfSupply == 0) {
        alert("Place of Supply is Mandatory");
        return (false);
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
