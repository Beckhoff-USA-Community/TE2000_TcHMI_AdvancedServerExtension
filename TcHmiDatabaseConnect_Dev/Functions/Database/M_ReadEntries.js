var TcHmi;
(function (TcHmi) {
    let Functions;
    (function (Functions) {
        let TcHmiDatabaseConnect_Dev;
        (function (TcHmiDatabaseConnect_Dev) {
            function M_ReadEntries(TargetDatabaseHandler, TargetDataGrid) {
                console.log("Sending query...");
                TcHmi.Symbol.writeEx('%s%' + TargetDatabaseHandler + '::M_ReadEntries%/s%', [], function (data) {
                    console.log("Query response...");
                    console.log(data);
                    //TcHmi.Symbol.writeEx('%s%SQLConnectionAPI.FilterInventory::FilterItem%/s%', FilterText, function (data) {
                    if (data.error === TcHmi.Errors.NONE) {
                        //ListItems is the desired return set, modify as needed
                        //TcHmi.Controls.get('TempOutput').setText(data.response.commands[0].readValue.ListItems);
                        //TcHmi.Controls.get('TcHmiDatagrid_1').setSrcData(data.response.commands[0].readValue.ListItems);
                        var _targetDatagrid = TcHmi.Controls.get(TargetDataGrid);
                        if (_targetDatagrid) {
                            if (data.value) {
                                console.log(data.value);
                                _targetDatagrid.setSrcData(data.value);
                            }
                        }
                    }
                    else {
                    }
                    return "This is a return string";
                });
            }
            TcHmiDatabaseConnect_Dev.M_ReadEntries = M_ReadEntries;
        })(TcHmiDatabaseConnect_Dev = Functions.TcHmiDatabaseConnect_Dev || (Functions.TcHmiDatabaseConnect_Dev = {}));
        Functions.registerFunctionEx('M_ReadEntries', 'TcHmi.Functions.TcHmiDatabaseConnect_Dev', TcHmiDatabaseConnect_Dev.M_ReadEntries);
    })(Functions = TcHmi.Functions || (TcHmi.Functions = {}));
})(TcHmi || (TcHmi = {}));
//# sourceMappingURL=M_ReadEntries.js.map