﻿using EDDiscovery;
using EDDiscovery.DB;
using EDDiscovery2.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace EDDiscovery2.EDSM
{
    class EDSMClass : HttpCom
    {
        public string commanderName;
        public string apiKey;

        private readonly string fromSoftwareVersion;
        private readonly string fromSoftware;



        public EDSMClass()
        {
            fromSoftware = "EDDiscovery";
            ServerAdress = "http://www.edsm.net/";

            var assemblyFullName = Assembly.GetExecutingAssembly().FullName;
            fromSoftwareVersion = assemblyFullName.Split(',')[1].Split('=')[1];
        }




        public string SubmitDistances(string cmdr, string from, string to, double dist)
        {
            return SubmitDistances(cmdr, from, new Dictionary<string, double> { { to, dist } });
        }

        public string SubmitDistances(string cmdr, string from, Dictionary<string, double> distances)
        {
            CultureInfo culture = new CultureInfo("en-US");
            string query = "{\"ver\":2," + " \"commander\":\"" + cmdr + "\", \"fromSoftware\":\"" + fromSoftware + "\",  \"fromSoftwareVersion\":\"" + fromSoftwareVersion + "\", \"p0\": { \"name\": \"" + from + "\" },   \"refs\": [";

            var counter = 0;
            foreach (var item in distances)
            {
                if (counter++ > 0)
                {
                    query += ",";
                }

                var to = item.Key;
                var distance = item.Value.ToString("0.00", culture);

                query += " { \"name\": \"" + to + "\",  \"dist\": " + distance + " } ";
            }


            query += "] } ";

            return RequestPost("{ \"data\": " + query + " }", "api-v1/submit-distances");
        }


        public bool ShowDistanceResponse(string json, out string respstr, out Boolean trilOK)
        {
            bool retval = true;
            JObject edsm = null;
            trilOK = false;

            respstr = "";

            try
            {
                if (json == null)
                    return false;

                edsm = (JObject)JObject.Parse(json);

                if (edsm == null)
                    return false;

                JObject basesystem = (JObject)edsm["basesystem"];
                JArray distances = (JArray)edsm["distances"];


                if (distances != null)
                    foreach (var st in distances)
                    {
                        int statusnum = st["msgnum"].Value<int>();

                        if (statusnum == 201)
                            retval = false;

                        respstr += "Status " + statusnum.ToString() + " : " + st["msg"].Value<string>() + Environment.NewLine;

                    }

                if (basesystem != null)
                {

                    int statusnum = basesystem["msgnum"].Value<int>();

                    if (statusnum == 101)
                        retval = false;

                    if (statusnum == 102 || statusnum == 104)
                        trilOK = true;

                    respstr += "System " + statusnum.ToString() + " : " + basesystem["msg"].Value<string>() + Environment.NewLine;
                }

                return retval;
            }
            catch (Exception ex)
            {
                respstr += "Excpetion in ShowDistanceResponse: " + ex.Message;
                return false;
            }
        }

        

     public string RequestSystems(string date)
        {
            string query;
            string  json2;
            //string datestr = date.ToString("yyyy-MM-dd hh:mm:ss");

            query = "?startdatetime=" + HttpUtility.UrlEncode(date);
            //json1= RequestGet("systems" + query + "&coords=1&submitted=1");
            json2=  RequestGet("api-v1/systems" + query + "&coords=1&submitted=1&known=1");

            return json2;
        }

        public string RequestDistances(string date)
        {
            string query;
            query = "?startdatetime=" + HttpUtility.UrlEncode(date);

            return RequestGet("api-v1/distances" + query + "coords=1 & submitted=1");
        }



        internal string GetNewSystems(SQLiteDBClass db)
        {
            string json;
            string date = "2010-01-01 00:00:00";
            string lstsyst;

            string retstr = "";


            Application.DoEvents();

            db.GetAllSystems();

            //if (lstsys)


            DateTime NewSystemTime;

            if (SQLiteDBClass.globalSystems == null || SQLiteDBClass.globalSystems.Count ==0)
            {
                lstsyst = "2010-01-01 00:00:00";
            }
            else
            {
                NewSystemTime = SQLiteDBClass.globalSystems.Max(x => x.UpdateDate);
                lstsyst = NewSystemTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                lstsyst = db.GetSettingString("EDSMLastSystems", lstsyst);

                if (lstsyst.Equals("2010-01-01 00:00:00"))
                    lstsyst = NewSystemTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);


            }
            json = RequestSystems(lstsyst);


            List<SystemClass> listNewSystems = SystemClass.ParseEDSM(json, ref date);

            retstr = listNewSystems.Count.ToString() + " new systems from EDSM." + Environment.NewLine;
            Application.DoEvents();
            SystemClass.Store(listNewSystems);
            db.PutSettingString("EDSMLastSystems", date);

            return retstr;
        }


        public List<DistanceClass> GetDistances(string systemname)
        {
            List<DistanceClass> listDistances = new List<DistanceClass>();
            try
            {
                string json;


                string query;
                query = "?sysname=" + HttpUtility.UrlEncode(systemname) + "&coords=1&distances=1&submitted=1";

                json = RequestGet("api-v1/system" + query);

                //http://www.edsm.net/api-v1/system?sysname=Col+359+Sector+CP-Y+c1-18&coords=1&include_hidden=1&distances=1&submitted=1

                if (json.Length > 1)
                {
                    JObject ditancesresp = (JObject)JObject.Parse(json);

                    JArray distances = (JArray)ditancesresp["distances"];

                    if (distances != null)
                    {
                        foreach (JObject jo in distances)
                        {
                            DistanceClass dc = new DistanceClass();

                            dc.NameA = systemname;
                            dc.NameB = jo["name"].Value<string>();
                            dc.Dist = jo["dist"].Value<float>();
//                            dc.CommanderCreate = jo[]

                            listDistances.Add(dc);
                        }
                    }
                }
            }
            catch
            {
            }
            return listDistances;
        }

        public string GetComments(DateTime starttime)
        {
            SQLiteDBClass db = new SQLiteDBClass();

            string query = "get-comments?startdatetime=" + HttpUtility.UrlEncode(starttime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)) + "&apiKey=" + apiKey + "&commanderName=" + HttpUtility.UrlEncode(commanderName);
            //string query = "get-comments?apiKey=" + apiKey + "&commanderName=" + HttpUtility.UrlEncode(commanderName);
            string json = RequestGet("api-logs-v1/" + query);

            return json;
        }


        public string GetComment(string systemName)
        {
            string query;
            query = "get-comment?systemName=" + HttpUtility.UrlEncode(systemName);

            string json =  RequestGet("api-logs-v1/" + query);
            return json;
        }

        public string SetComment(SystemNoteClass sn)
        {
            string query;
            query = "set-comment?systemName=" + HttpUtility.UrlEncode(sn.Name) + "&commanderName=" + HttpUtility.UrlEncode(commanderName) + "&apiKey=" + apiKey + "&comment=" + HttpUtility.UrlEncode(sn.Note);
            string json = RequestGet("api-logs-v1/"+ query);

            return json;
        }

        public string SetLog(string systemName, DateTime dateVisited)
        {
            string query;
            query = "set-log?systemName=" + HttpUtility.UrlEncode(systemName) + "&commanderName=" + HttpUtility.UrlEncode(commanderName) + "&apiKey=" + apiKey + "&dateVisited=" + HttpUtility.UrlEncode(dateVisited.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            string json = RequestGet("api-logs-v1/" + query);

            return json;
        }

        public int GetLogs(DateTime starttime, out List<SystemPosition> log)
        {
            log = new List<SystemPosition>();

            string query = "get-logs?startdatetime=" + HttpUtility.UrlEncode(starttime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)) + "&apiKey=" + apiKey + "&commanderName=" + HttpUtility.UrlEncode(commanderName);
            //string query = "get-logs?apiKey=" + apiKey + "&commanderName=" + HttpUtility.UrlEncode(commanderName);
            string json = RequestGet("api-logs-v1/"+ query);

            if (json == null)
                return 0;

            JObject msg = (JObject)JObject.Parse(json);
            int msgnr = msg["msgnum"].Value<int>();

            JArray logs = (JArray)msg["logs"];

            if (logs != null)
            {
                foreach (JObject jo in logs)
                {
                    SystemPosition pos = new SystemPosition();


                    pos.Name = jo["system"].Value<string>();
                    string str = jo["date"].Value<string>();

                    pos.time = DateTime.ParseExact(str, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

                    log.Add(pos);
              
                }
            }

            return msgnr;
        }


        


    }
}
