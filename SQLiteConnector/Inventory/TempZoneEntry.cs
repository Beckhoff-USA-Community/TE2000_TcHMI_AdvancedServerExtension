using System;
using System.Collections.Generic;
using System.Text;
using TcHmiSrv.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;


namespace SQLiteConnector
{
    public class TempZoneEntry
    {

        private long _id;

        [JsonProperty(Required = Required.Default)]
        public long id
        {
            get
            {
                return this._id;
            }
            set
            {
                this._id = value;
            }
        }


        private string _zonename;
        public string zonename
        {
            get
            {
                return this._zonename;
            }
            set
            {
                if (value.Length > 20)
                {
                    _zonename = value.Substring(0, 20);
                }
                else
                {
                    _zonename = value;
                }
            }


            }

        
        private float _temperature;
        public float temperature
        {
            get
            {
                return this._temperature;
            }
            set
            {
                    _temperature = value;
            }
        }

        private string _timestamp;
        public string timestamp
        {
            get
            {
                return this._timestamp;
            }
            set
            {
                    _timestamp = value;
            }
        }
    }
}
