declare module TcHmi {
    module Functions {
        module TcHmiDatabaseConnect_Dev {
            function ConfigListToComboBoxList(ConfigEntry_List: Array<any>): {
                index: number;
                text: any;
                value: any;
            }[];
        }
    }
}
