var TcHmi;
(function (TcHmi) {
    let Functions;
    (function (Functions) {
        let TcHmiDatabaseConnect_Dev;
        (function (TcHmiDatabaseConnect_Dev) {
            function M_WriteEntry(TargetDatabaseHandler, zonename, temperature) {
                var timestamp = new Date().toISOString();
                //Be sure to modify the Symbol's connection string
                TcHmi.Symbol.writeEx('%s%' + TargetDatabaseHandler + '::M_WriteEntry%/s%', {
                    "temperature": temperature, "timestamp": timestamp, "zonename": zonename
                }, function (data) {
                    if (data.error === TcHmi.Errors.NONE) {
                        console.log("No Errors: ", data);
                    }
                    else {
                        console.log("Command Error: ", data);
                    }
                });
            }
            TcHmiDatabaseConnect_Dev.M_WriteEntry = M_WriteEntry;
        })(TcHmiDatabaseConnect_Dev = Functions.TcHmiDatabaseConnect_Dev || (Functions.TcHmiDatabaseConnect_Dev = {}));
        Functions.registerFunctionEx('M_WriteEntry', 'TcHmi.Functions.TcHmiDatabaseConnect_Dev', TcHmiDatabaseConnect_Dev.M_WriteEntry);
    })(Functions = TcHmi.Functions || (TcHmi.Functions = {}));
})(TcHmi || (TcHmi = {}));
//# sourceMappingURL=M_WriteEntry.js.map