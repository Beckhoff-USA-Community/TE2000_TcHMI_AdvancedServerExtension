module TcHmi {
    export module Functions {
        export module TcHmiDatabaseConnect_Dev {
            export function M_WriteEntry(TargetDatabaseHandler:any,zonename:any,temperature:any) {


               var timestamp : string = new Date().toISOString();


                //Be sure to modify the Symbol's connection string
                TcHmi.Symbol.writeEx('%s%'+TargetDatabaseHandler+'::M_WriteEntry%/s%', {
                    "temperature":temperature,"timestamp":timestamp,"zonename":zonename
                },function(data) {
                    if (data.error === TcHmi.Errors.NONE) {
                        console.log("No Errors: ",data);

                    } else {
                        console.log("Command Error: ",data);
                    }
                });

            }
        }
        registerFunctionEx('M_WriteEntry','TcHmi.Functions.TcHmiDatabaseConnect_Dev',TcHmiDatabaseConnect_Dev.M_WriteEntry);
    }
}