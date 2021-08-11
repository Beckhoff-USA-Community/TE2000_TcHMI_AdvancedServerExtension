var TcHmi;
(function (TcHmi) {
    let Functions;
    (function (Functions) {
        let TcHmiDatabaseConnect_Dev;
        (function (TcHmiDatabaseConnect_Dev) {
            function ConfigListToComboBoxList(ConfigEntry_List) {
                var ComboBoxList = [];
                for (let i = 0; i < ConfigEntry_List.length; i++) {
                    ComboBoxList.push({
                        "index": i,
                        "text": ConfigEntry_List[i],
                        "value": ConfigEntry_List[i]
                    });
                    TcHmi.Symbol.exists('%s%' + ConfigEntry_List[i] + '%/s%', function (data) {
                        if (data.error === TcHmi.Errors.NONE) {
                            // no errors
                            var symExists = data.result;
                            if (!symExists) {
                                var symExists = data.result;
                                console.log('Symbol does not exist: ' + symExists);
                                console.log('Creating symbol...');
                                TcHmi.Server.requestEx({
                                    requestType: 'ReadWrite',
                                    commands: [{
                                            symbol: 'AddSymbol',
                                            commandOptions: ['SendErrorMessage'],
                                            writeValue: {
                                                NAME: ConfigEntry_List[i],
                                                MAPPING: ConfigEntry_List[i],
                                                ACCESS: 3,
                                                DOMAIN: "SQLiteConnector",
                                                AUTOMAP: true,
                                                "SCHEMA": {
                                                    "$ref": "tchmi:server#/definitions/SQLiteConnector.SqliteConnection"
                                                },
                                                USEMAPPING: true
                                            }
                                        }]
                                }, { timeout: 5000 }, function (data) {
                                    if (data.error === TcHmi.Errors.NONE) {
                                        // Handle result value... 
                                    }
                                    else {
                                    }
                                });
                            }
                        }
                        else {
                            // Checkign exists failed
                        }
                    });
                }
                return ComboBoxList;
            }
            TcHmiDatabaseConnect_Dev.ConfigListToComboBoxList = ConfigListToComboBoxList;
        })(TcHmiDatabaseConnect_Dev = Functions.TcHmiDatabaseConnect_Dev || (Functions.TcHmiDatabaseConnect_Dev = {}));
        Functions.registerFunctionEx('ConfigListToComboBoxList', 'TcHmi.Functions.TcHmiDatabaseConnect_Dev', TcHmiDatabaseConnect_Dev.ConfigListToComboBoxList);
    })(Functions = TcHmi.Functions || (TcHmi.Functions = {}));
})(TcHmi || (TcHmi = {}));
//# sourceMappingURL=ConfigListToComboBoxList.js.map