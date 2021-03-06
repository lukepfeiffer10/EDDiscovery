﻿using EDDiscovery.DB;
using EDDiscovery2;
using EDDiscovery2.DB;
using EDDiscovery2.EDDB;
using EDDiscovery2.EDSM;
using EDDiscovery2.PlanetSystems;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using EDDiscovery2.Themes;

namespace EDDiscovery
{
    public delegate void DistancesLoaded();

    public partial class EDDiscoveryForm : Form
    {
        //readonly string _fileTgcSystems;
        readonly string _fileEDSMDistances;
        private EDSMSync _edsmSync;
        private SQLiteDBClass _db = new SQLiteDBClass();

        public AutoCompleteStringCollection SystemNames { get; private set; }
        public string CommanderName { get; private set; }
        static public EDDConfig EDDConfig { get; private set; }
        public EDDiscovery2._3DMap.MapManager Map { get; private set; }

        public event DistancesLoaded OnDistancesLoaded;

        public EDDiscoveryForm()
        {
            InitializeComponent();

            EDDConfig = new EDDConfig();

            //_fileTgcSystems = Path.Combine(Tools.GetAppDataDirectory(), "tgcsystems.json");
            _fileEDSMDistances = Path.Combine(Tools.GetAppDataDirectory(), "EDSMDistances.json");        

            string logpath="";
            try
            {
                logpath = Path.Combine(Tools.GetAppDataDirectory(), "Log");
                if (!Directory.Exists(logpath))
                {
                    Directory.CreateDirectory(logpath);
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to create the folder '{logpath}'");
                Trace.WriteLine($"Exception: {ex.Message}");
            }
            _edsmSync = new EDSMSync(this);

            trilaterationControl.InitControl(this);
            travelHistoryControl1.InitControl(this);
            imageHandler1.InitControl(this);
            menuStrip1.Renderer = new MenuRenderer(new MenuColorTable());
            listBox1.DataSource = EDDConfig.Themes;
            listBox1.DisplayMember = "Name";
            listBox1.ValueMember = "Name";
            listBox1.SelectedValueChanged += delegate(object sender, EventArgs args)
            {
                var listBox = sender as ListBox;
                var theme = listBox.SelectedItem as ITheme;
                EDDConfig.SelectedTheme = theme;
                this.BackColor = EDDConfig.SelectedTheme.MainBackgroundColor;
                this.ForeColor = EDDConfig.SelectedTheme.MainFontColor;
                ApplyThemeToControls(this.Controls);
            };

            SystemNames = new AutoCompleteStringCollection();
            Map = new EDDiscovery2._3DMap.MapManager();
        }

        public TravelHistoryControl TravelControl
        {
            get { return travelHistoryControl1; }
        }


        internal void ShowTrilaterationTab()
        {
            tabControl1.SelectedIndex = 1;
        }

        internal void ShowHistoryTab()
        {
            tabControl1.SelectedIndex = 0;
        }

        private void EDDiscoveryForm_Load(object sender, EventArgs e)
        {
            try
            {
                EliteDangerous.CheckED();
                EDDConfig.Update();
                RepositionForm();
                InitFormControls();
                InitSettingsTab();
                CheckIfEliteDangerousIsRunning();
                CheckIfVerboseLoggingIsTurnedOn();

                if (File.Exists("test.txt"))
                {
                    button1.Visible = true;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("EDDiscoveryForm_Load exception: " + ex.Message);
                System.Windows.Forms.MessageBox.Show("Trace: " + ex.StackTrace);
            }
        }


        private void EDDiscoveryForm_Shown(object sender, EventArgs e)
        {
            try
            {
                travelHistoryControl1.Enabled = false;

                var edsmThread = new Thread(GetEDSMSystems) {Name = "Downloading EDSM Systems"};
                var downloadmapsThread = new Thread(DownloadMaps) { Name = "Downloading map Files" };
                edsmThread.Start();
                downloadmapsThread.Start();

                while (edsmThread.IsAlive || downloadmapsThread.IsAlive)
                {
                    Thread.Sleep(50);
                    Application.DoEvents();
                }

                edsmThread.Join();
                downloadmapsThread.Join();

                OnDistancesLoaded += new DistancesLoaded(this.DistancesLoaded);

                 GetEDSMDistancesAsync();

                //Application.DoEvents();
                GetEDDBAsync(false);

                routeControl1.textBox_From.AutoCompleteCustomSource = SystemNames;
                routeControl1.textBox_To.AutoCompleteCustomSource = SystemNames;

                Text += "         Systems:  " + SystemData.SystemList.Count;

                routeControl1.travelhistorycontrol1 = travelHistoryControl1;
                travelHistoryControl1.netlog.OnNewPosition += new NetLogEventHandler(routeControl1.NewPosition);
                travelHistoryControl1.netlog.OnNewPosition += new NetLogEventHandler(travelHistoryControl1.NewPosition);
                travelHistoryControl1.sync.OnNewEDSMTravelLog += new EDSMNewSystemEventHandler(travelHistoryControl1.RefreshEDSMEvent);

                TravelHistoryControl.LogText("Reading travelhistory ");
                travelHistoryControl1.RefreshHistory();
                travelHistoryControl1.netlog.StartMonitor();

                travelHistoryControl1.Enabled = true;
                if (EliteDangerous.CheckStationLogging())
                {
                    panelInfo.Visible = false;
                }


                // Check for a new installer    
                CheckForNewInstaller();

                LogLine($"{Environment.NewLine}Loading completed!");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("EDDiscovery_Load exception: " + ex.Message);
                System.Windows.Forms.MessageBox.Show("Trace: " + ex.StackTrace);
                travelHistoryControl1.Enabled = true;

            }
        }


        public void DownloadMaps()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(Tools.GetAppDataDirectory(), "Maps")))
                    Directory.CreateDirectory(Path.Combine(Tools.GetAppDataDirectory(), "Maps"));


                LogText("Checking for new EDDiscovery maps" + Environment.NewLine);

                if (DownloadMapFile("SC-01.jpg"))  // If server down only try one.
                {
                    DownloadMapFile("SC-02.jpg");
                    DownloadMapFile("SC-03.jpg");
                    DownloadMapFile("SC-04.jpg");

                    DownloadMapFile("SC-L4.jpg");
                    DownloadMapFile("SC-U4.jpg");

                    DownloadMapFile("Galaxy_L.jpg");
                    DownloadMapFile("Galaxy_L.json");
                    DownloadMapFile("Galaxy_L_Grid.jpg");
                    DownloadMapFile("Galaxy_L_Grid.json");

                    DownloadMapFile("DW1.jpg");
                    DownloadMapFile("DW1.json");
                    DownloadMapFile("DW2.jpg");
                    DownloadMapFile("DW2.json");
                    DownloadMapFile("DW3.jpg");
                    DownloadMapFile("DW3.json");


                    //for (int ii = -10; ii <= 60; ii += 10)
                    //{
                    //    DownloadMapFile("Map A+00" + ii.ToString("+00;-00") + ".png");
                    //    DownloadMapFile("Map A+00" + ii.ToString("+00;-00") + ".json");
                    //}
                }
            }
            catch (Exception ex)
            {
                LogText("Exception in DownloadImages:" + ex.Message + Environment.NewLine);
            }

        }

        public void updateMapData()
        {
            Map.Instance.SystemNames = SystemNames;
            Map.Instance.VisitedSystems = VisitedSystems;
        }

        private bool DownloadMapFile(string file)
        {
            EDDBClass eddb = new EDDBClass();
            bool newfile = false;
            if (eddb.DownloadFile("http://eddiscovery.astronet.se/Maps/" + file, Path.Combine(Tools.GetAppDataDirectory(), "Maps\\" + file), out newfile))
            {
                if (newfile)
                    LogText("Downloaded map: " + file + Environment.NewLine);
                return true;

            }
            else
                return false;
        }

        private bool CanSkipSlowUpdates()
        {
#if DEBUG
            return EDDConfig.CanSkipSlowUpdates;
#else
            return false;
#endif
        }
        /*
        private void GetRedWizzardFiles()
        {
            WebClient web = new WebClient();

            try
            {
                LogText("Checking for new EDDiscovery data" + Environment.NewLine);

                //GetNewRedWizzardFile(_fileTgcSystems, "http://robert.astronet.se/Elite/ed-systems/tgcsystems.json");
                //GetNewRedWizzardFile(fileTgcDistances, "http://robert.astronet.se/Elite/ed-systems/tgcdistances.json");
            }
            catch (Exception ex)
            {
                LogText("GetRedWizzardFiles exception:" + ex.Message + Environment.NewLine);
                return;
            }
        }
        
        private void GetNewRedWizzardFile(string filename, string url)
        {
            string etagFilename = filename + ".etag";

            var request = (HttpWebRequest) HttpWebRequest.Create(url);
            request.UserAgent = "EDDiscovery v" + Assembly.GetExecutingAssembly().FullName.Split(',')[1].Split('=')[1];
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if (File.Exists(etagFilename))
            {
                var etag = File.ReadAllText(etagFilename);
                if (etag != "")
                {
                   request.Headers[HttpRequestHeader.IfNoneMatch] = etag;
                }
            }

            try {
                var response = (HttpWebResponse) request.GetResponse();
                
                LogText("Downloading " + filename + "..." + Environment.NewLine);

                File.WriteAllText(filename + ".etag.tmp", response.Headers[HttpResponseHeader.ETag]);
                var destFileStream = File.Open(filename + ".tmp", FileMode.Create, FileAccess.Write);
                response.GetResponseStream().CopyTo(destFileStream);
                
                destFileStream.Close();
                response.Close();

                if (File.Exists(filename))
                    File.Delete(filename);
                if (File.Exists(etagFilename))
                    File.Delete(etagFilename);

                File.Move(filename + ".tmp", filename);
                File.Move(etagFilename + ".tmp", etagFilename);
            } catch (WebException e)
            {
                var code = ((HttpWebResponse) e.Response).StatusCode;
                if (code == HttpStatusCode.NotModified)
                {
                    LogText(filename + " is up to date." + Environment.NewLine);
                } else
                {
                    throw e;
                }
            }
        }
        */

        private void GetEDSMSystems()
        {
            try
            {
                EDSMClass edsm = new EDSMClass();


                string json;

                string rwsystime = _db.GetSettingString("EDSMLastSystems", "2000-01-01 00:00:00"); // Latest time from RW file.

                CommanderName = _db.GetSettingString("CommanderName", "");
                Invoke((MethodInvoker) delegate {
                    travelHistoryControl1.textBoxCmdrName.Text = CommanderName;
                });


                //                List<SystemClass> systems = SystemClass.ParseEDSC(json, ref rwsysfiletime);
                DateTime edsmdate = DateTime.Parse(rwsystime, new CultureInfo("sv-SE"));

                if (DateTime.Now.Subtract(edsmdate).TotalDays > 7)  // Over 7 days do a sync from EDSM
                {
                    SyncAllEDSMSystems();
                }
                else
                {
                    if (CanSkipSlowUpdates())
                    {
                        LogLine("Skipping loading updates (DEBUG option).");
                        LogLine("  Need to turn this back on again? Look in the Settings tab.");
                    }
                    else
                    {
                        string retstr = edsm.GetNewSystems(_db);
                        Invoke((MethodInvoker)delegate
                        {
                            TravelHistoryControl.LogText(retstr);
                        });
                    }

                }

                _db.GetAllSystemNotes();
                _db.GetAllSystems();



                SystemNames.Clear();
                foreach (SystemClass system in SystemData.SystemList)
                {
                    SystemNames.Add(system.name);
                }

            }
            catch (Exception ex)
            {
                Invoke((MethodInvoker) delegate {
                    TravelHistoryControl.LogText("GetEDSMSystems exception:" + ex.Message + Environment.NewLine);
                    TravelHistoryControl.LogText(ex.StackTrace + Environment.NewLine);
                });
            }

            GC.Collect();

        }

        private Thread ThreadEDSMDistances;
        private void GetEDSMDistancesAsync()
        {
            ThreadEDSMDistances = new System.Threading.Thread(new System.Threading.ThreadStart(GetEDSMDistances));
            ThreadEDSMDistances.Name = "Get Distances";
            ThreadEDSMDistances.Start();
        }

        private Thread ThreadEDDB;

        public List<SystemPosition> VisitedSystems
        {
            get { return travelHistoryControl1.visitedSystems; }
        }


        private bool eddbforceupdate;
        private void GetEDDBAsync(bool force)
        {
            ThreadEDDB = new System.Threading.Thread(new System.Threading.ThreadStart(GetEDDBUpdate));
            ThreadEDDB.Name = "Get EDDB Update";
            eddbforceupdate = force;
            ThreadEDDB.Start();
        }

   
        private void GetEDSMDistances()
        {
            try
            {
                if (EDDConfig.UseDistances)
                {
                    EDSMClass edsm = new EDSMClass();
                    EDDBClass eddb = new EDDBClass();
                    string lstdist = _db.GetSettingString("EDSCLastDist", "2010-01-01 00:00:00");
                    string json;

                    // Get distances
                    lstdist = _db.GetSettingString("EDSCLastDist", "2010-01-01 00:00:00");
                    List<DistanceClass> dists = new List<DistanceClass>();

                    if (lstdist.Equals("2010-01-01 00:00:00"))
                    {
                        LogText("Downloading mirrored EDSM distance data. (Might take some time)" + Environment.NewLine);
                        eddb.GetEDSMDistances();
                        json = LoadJsonFile(_fileEDSMDistances);
                        if (json != null)
                        {
                            LogText("Adding mirrored EDSM distance data." + Environment.NewLine);

                            dists = new List<DistanceClass>();
                            dists = DistanceClass.ParseEDSM(json, ref lstdist);
                            LogText("Found " + dists.Count.ToString() + " distances." + Environment.NewLine);

                            Application.DoEvents();
                            DistanceClass.Store(dists);
                            _db.PutSettingString("EDSCLastDist", lstdist);
                        }
                    }


                    LogText("Checking for new distances from EDSM. ");


                    Application.DoEvents();
                    json = edsm.RequestDistances(lstdist);

                    dists = new List<DistanceClass>();
                    dists = DistanceClass.ParseEDSM(json, ref lstdist);

                    if (json == null)
                        LogText("No response from server." + Environment.NewLine);

                    else
                        LogText("Found " + dists.Count.ToString() + " new distances." + Environment.NewLine);

                    Application.DoEvents();
                    DistanceClass.Store(dists);
                    _db.PutSettingString("EDSCLastDist", lstdist);
                }
                _db.GetAllDistances(EDDConfig.UseDistances);  // Load user added distances
                updateMapData();
                OnDistancesLoaded();
                GC.Collect();
            }
            catch (Exception ex)
            {
                LogText("GetEDSMDistances exception:" + ex.Message + Environment.NewLine);
                LogText(ex.StackTrace + Environment.NewLine);
            }

        }

        private void CheckForNewInstaller()
        {
            {
                EDDiscoveryServer eds = new EDDiscoveryServer();

                string inst = eds.GetLastestInstaller();
                if (inst != null)
                {
                    JObject jo = (JObject)JObject.Parse(inst);

                    string newVersion = jo["Version"].Value<string>();
                    string newInstaller = jo["Filename"].Value<string>();

                    var currentVersion = Application.ProductVersion;

                    Version v1, v2;
                    v1 = new Version(newVersion);
                    v2 = new Version(currentVersion);

                    if (v1.CompareTo(v2) > 0) // Test if newver installer exists:
                    {
                        LogText("New EDDiscovery installer availble  " + "http://eddiscovery.astronet.se/release/" + newInstaller + Environment.NewLine, Color.Salmon);
                    }

                }
            }
        }


        internal void DistancesLoaded()
        {
            Invoke((MethodInvoker)delegate
            {
                travelHistoryControl1.RefreshHistory();
            });
        }


        private void  GetEDDBUpdate()
        {
            try
            {
                EDDBClass eddb = new EDDBClass();
                string timestr;
                DateTime time;

                Thread.Sleep(1000);

                timestr = _db.GetSettingString("EDDBSystemsTime", "0");
                time = new DateTime(Convert.ToInt64(timestr), DateTimeKind.Utc);
                bool updatedb = false;




                if (DateTime.UtcNow.Subtract(time).TotalDays > 0.5)
                {
                    LogText("Get systems from EDDB. ");

                    if (eddb.GetSystems())
                    {
                        LogText("OK." + Environment.NewLine);

                        _db.PutSettingString("EDDBSystemsTime", DateTime.UtcNow.Ticks.ToString());
                        updatedb = true;
                    }
                    else
                        LogText("Failed." + Environment.NewLine, Color.Red);


                    eddb.GetCommodities();
                    eddb.ReadCommodities();
                }


                timestr = _db.GetSettingString("EDDBStationsLiteTime", "0");
                time = new DateTime(Convert.ToInt64(timestr), DateTimeKind.Utc);

                if (DateTime.UtcNow.Subtract(time).TotalDays > 0.5)
                {

                    LogText("Get stations from EDDB. ");
                    if (eddb.GetStationsLite())
                    {
                        LogText("OK." + Environment.NewLine);
                        _db.PutSettingString("EDDBStationsLiteTime", DateTime.UtcNow.Ticks.ToString());
                        updatedb = true;
                    }
                    else
                        LogText("Failed." + Environment.NewLine, Color.Red);

                }

                

                if (updatedb || eddbforceupdate)
                {
                    DBUpdateEDDB(eddb);
                }

                return;

            }
            catch (Exception ex)
            {
                Invoke((MethodInvoker)delegate
                {
                    TravelHistoryControl.LogText("GetEDSCSystems exception:" + ex.Message + Environment.NewLine);
                });
            }
           
        }

        private void DBUpdateEDDB(EDDBClass eddb)
        {
            List<SystemClass> eddbsystems = eddb.ReadSystems();
            List<StationClass> eddbstations = eddb.ReadStations();

            LogText("Add new EDDB data to database." + Environment.NewLine);
            eddb.Add2DB(eddbsystems, eddbstations);

            
            eddbsystems.Clear();
            eddbstations.Clear();
            eddbsystems = null;
            GC.Collect();
            LogText("EDDB update done." + Environment.NewLine);
        }


        private void LogText(string text)
        {
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    TravelHistoryControl.LogText(text);
                });
            }
            catch
            {
            }
        }

        public void LogText(string text, Color col)
        {
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    TravelHistoryControl.LogText(text, col);

                });
            }
            catch
            {
            }
        }

        public void LogLine(string text)
        {
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    TravelHistoryControl.LogText(text + Environment.NewLine);
                });
            }
            catch
            {
            }
        }

        public void LogLine(string text, Color col)
        {
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    TravelHistoryControl.LogText(text + Environment.NewLine, col);

                });
            }
            catch
            {
            }
        }

        static public string LoadJsonFile(string filename)
        {
            string json = null;
            try
            {
                if (!File.Exists(filename))
                    return null;

                StreamReader reader = new StreamReader(filename);
                json = reader.ReadToEnd();
                reader.Close();
            }
            catch
            {
            }

            return json;
        }


      





        private string LoadJSON(string jfile)
        {
            string json = null;
            try
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\EDDiscovery";

                if (!Directory.Exists(appdata))
                    Directory.CreateDirectory(appdata);

                string filename = appdata + "\\"+jfile;
                
                if (!File.Exists(filename))
                    return null;


                StreamReader reader = new StreamReader(filename);

                json = reader.ReadToEnd();

                reader.Close();
            }
            catch
            {
            }

            return json;
        }




        private void button_Browse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dirdlg = new FolderBrowserDialog();


            DialogResult dlgResult = dirdlg.ShowDialog();

            if (dlgResult == DialogResult.OK)
            {
                textBoxNetLogDir.Text = dirdlg.SelectedPath;
            }
        }

        private void button_Save_Click(object sender, EventArgs e)
        {
            SaveSettings();

            tabControl1.SelectedTab = tabPageTravelHistory;
            travelHistoryControl1.RefreshHistory();
        }

        private void SaveSettings()
        {
            _db.PutSettingBool("NetlogDirAutoMode", radioButton_Auto.Checked);
            _db.PutSettingString("Netlogdir", textBoxNetLogDir.Text);
            _db.PutSettingString("EDSMApiKey", textBoxEDSMApiKey.Text);
            _db.PutSettingInt("FormWidth", this.Width);
            _db.PutSettingInt("FormHeight", this.Height);
            _db.PutSettingInt("FormTop", this.Top);
            _db.PutSettingInt("FormLeft", this.Left);
            _db.PutSettingString("DefaultMapCenter", textBoxHomeSystem.Text);
            _db.PutSettingDouble("DefaultMapZoom", Double.Parse(textBoxDefaultZoom.Text));
            _db.PutSettingBool("CentreMapOnSelection", radioButtonHistorySelection.Checked);
            EDDConfig.UseDistances = checkBox_Distances.Checked;
            EDDConfig.EDSMLog = checkBoxEDSMLog.Checked;
            EDDConfig.CanSkipSlowUpdates = checkboxSkipSlowUpdates.Checked;
        }

        private void routeControl1_Load(object sender, EventArgs e)
        {

        }

        private void EDDiscoveryForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            travelHistoryControl1.netlog.StopMonitor();
            _edsmSync.StopSync();
            SaveSettings();
        }

        private void travelHistoryControl1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //FormSagCarinaMission frm = new FormSagCarinaMission(this);
            //            frm.Show();

            //SystemViewForm frm = new SystemViewForm();
            //frm.Show();

            EdMaterializer mat = new EdMaterializer();

            mat.GetAll(null);


            EDObject obj = new EDObject();

            obj.commander = "Test";
            obj.system = "Fine Ring Sector JH-V C2-4";
            obj.objectName = "A 3";
            obj.ObjectType = ObjectTypesEnum.HighMetalContent;
            obj.arrivalPoint = 0;
            obj.gravity = 0.13f;

            obj.materials[MaterialEnum.Carbon] = true;
            obj.materials[MaterialEnum.Iron] = true;
            obj.materials[MaterialEnum.Nickel] = true;
            obj.materials[MaterialEnum.Phosphorus] = true;
            obj.materials[MaterialEnum.Sulphur] = true;
            obj.materials[MaterialEnum.Germanium] = true;
            obj.materials[MaterialEnum.Selenium] = true;
            obj.materials[MaterialEnum.Vanadium] = true;
            obj.materials[MaterialEnum.Cadmium] = true;
            obj.materials[MaterialEnum.Molybdenum] = true;
            obj.materials[MaterialEnum.Tin] = true;
            obj.materials[MaterialEnum.Polonium] = true;

            mat.DeleteID(5);
            mat.DeleteID(6);
            mat.DeleteID(7);

            mat.Store(obj);
        }

        private void addNewStarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://robert.astronet.se/Elite/ed-systems/entry.html");
        }

        private void frontierForumThreadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://forums.frontier.co.uk/showthread.php?t=138155&p=2113535#post2113535");
        }

        private void eDDiscoveryFGESupportThreadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://firstgreatexpedition.org/mybb/showthread.php?tid=1406");
        }

        private void eDDiscoveryHomepageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://eddiscovery.astronet.se/");
        }

        private void openEliteDangerousDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (EliteDangerous.EDDirectory != null && !EliteDangerous.EDDirectory.Equals(""))
                    Process.Start(EliteDangerous.EDDirectory);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Open EliteDangerous directory exception: " + ex.Message);
            }

        }

        private void showLogfilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(travelHistoryControl1.netlog.GetNetLogPath());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Show log files exception: " + ex.Message);
            }
        }

        private void statisticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StatsForm frm = new StatsForm();

            frm.travelhistoryctrl = travelHistoryControl1;
            frm.Show();

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }



        private void TestTrileteration()
        {
            foreach (SystemClass System in SQLiteDBClass.globalSystems)
            {
                if (DateTime.Now.Subtract(System.CreateDate).TotalDays < 60)
                {
                    //var Distances = from SQLiteDBClass.globalDistances

                    var distances1 = from p in SQLiteDBClass.dictDistances where p.Value.NameA.ToLower() == System.SearchName select p.Value;
                    var distances2 = from p in SQLiteDBClass.dictDistances where p.Value.NameB.ToLower() == System.SearchName select p.Value;

                    int nr = distances1.Count();
                    //nr = distances2.Count();


                    if (nr > 4)
                    {
                        var trilateration = new Trilateration();
                        //                    trilateration.Logger = (s) => System.Console.WriteLine(s);

                        foreach (var item in distances1)
                        {
                            SystemClass distsys = SystemData.GetSystem(item.NameB);
                            if (distsys != null)
                            {
                                if (distsys.HasCoordinate)
                                {
                                    Trilateration.Entry entry = new Trilateration.Entry(distsys.x, distsys.y, distsys.z, item.Dist);
                                    trilateration.AddEntry(entry);
                                }
                            }
                        }

                        foreach (var item in distances2)
                        {
                            SystemClass distsys = SystemData.GetSystem(item.NameA);
                            if (distsys != null)
                            {
                                if (distsys.HasCoordinate)
                                {
                                    Trilateration.Entry entry = new Trilateration.Entry(distsys.x, distsys.y, distsys.z, item.Dist);
                                    trilateration.AddEntry(entry);
                                }
                            }
                        }


                        var csharpResult = trilateration.Run(Trilateration.Algorithm.RedWizzard_Native);
                        var javascriptResult = trilateration.Run(Trilateration.Algorithm.RedWizzard_Emulated);
                        if (javascriptResult.State == Trilateration.ResultState.Exact)
                            nr++;
                    }
                }
            }
        }

        private void show2DMapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSagCarinaMission frm = new FormSagCarinaMission(this);

            frm.Show();
        }

        private void setDefaultMapColourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            travelHistoryControl1.setDefaultMapColour();
        }

        private void forceEDDBUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetEDDBAsync(true);
        }

        //Pleiades Sector WU-O B16-0
        //Pleiades Sector WU-O b6-0

        private void InitFormControls()
        {
            UpdateTitle();

            labelPanelText.Text = "Loading. Please wait!";
            panelInfo.Visible = true;
            panelInfo.BackColor = Color.Gold;

            routeControl1.travelhistorycontrol1 = travelHistoryControl1;
        }

        private void UpdateTitle()
        {
            var assemblyFullName = Assembly.GetExecutingAssembly().FullName;
            var version = assemblyFullName.Split(',')[1].Split('=')[1];
            Text = string.Format("EDDiscovery v{0}", version);
        }

        private void RepositionForm()
        {
            var top = _db.GetSettingInt("FormTop", -1);
            if (top > 0)
            {
                var left = _db.GetSettingInt("FormLeft", -1);
                var height = _db.GetSettingInt("FormHeight", -1);
                var width = _db.GetSettingInt("FormWidth", -1);
                this.Top = top;
                this.Left = left;
                this.Height = height;
                this.Width = width;
            }
        }

        private void InitSettingsTab()
        {
            // Default directory
            bool auto = _db.GetSettingBool("NetlogDirAutoMode", true);
            if (auto)
            {
                radioButton_Auto.Checked = auto;
            }
            else
            {
                radioButton_Manual.Checked = true;
            }
            string datapath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Frontier_Developments\\Products"); // \\FORC-FDEV-D-1001\\Logs\\";
            textBoxNetLogDir.Text = _db.GetSettingString("Netlogdir", datapath);

            textBoxEDSMApiKey.Text = _db.GetSettingString("EDSMApiKey", "");
            checkBox_Distances.Checked = EDDConfig.UseDistances;
            checkBoxEDSMLog.Checked = EDDConfig.EDSMLog;
            
            checkboxSkipSlowUpdates.Checked = EDDConfig.CanSkipSlowUpdates;
#if DEBUG
            checkboxSkipSlowUpdates.Visible = true;
#endif
            textBoxHomeSystem.AutoCompleteCustomSource = SystemNames;
            textBoxHomeSystem.Text = _db.GetSettingString("DefaultMapCenter", "Sol");

            textBoxDefaultZoom.Text = _db.GetSettingDouble("DefaultMapZoom", 1.0).ToString();

            bool selectionCentre = _db.GetSettingBool("CentreMapOnSelection", true);
            if (selectionCentre)
            {
                radioButtonHistorySelection.Checked = true;
            }
            else
            {
                radioButtonCentreHome.Checked = true;
            }
        }

        private void CheckIfEliteDangerousIsRunning()
        {
            if (EliteDangerous.EDRunning)
            {
                TravelHistoryControl.LogText("EliteDangerous is running." + Environment.NewLine);
            }
            else
            {
                TravelHistoryControl.LogText("EliteDangerous is not running ." + Environment.NewLine);
            }
        }

        private void CheckIfVerboseLoggingIsTurnedOn()
        {
            if (!EliteDangerous.CheckStationLogging())
            {
                TravelHistoryControl.LogText("Elite Dangerous is not logging system names!!! ", Color.Red);
                TravelHistoryControl.LogText("Add ");
                TravelHistoryControl.LogText("VerboseLogging=\"1\" ", Color.Blue);
                TravelHistoryControl.LogText("to <Network  section in File: " + Path.Combine(EliteDangerous.EDDirectory, "AppConfig.xml") + " or AppConfigLocal.xml  Remember to restart Elite!" + Environment.NewLine);

                labelPanelText.Text = "Elite Dangerous is not logging system names!";
                panelInfo.BackColor = Color.Salmon;
            }
        }

        private void prospectingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlanetsForm frm = new PlanetsForm();

            frm.InitForm(this);
            frm.Show();
        }

        private void textBoxDefaultZoom_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var value = textBoxDefaultZoom.Text.Trim();
            double parseout;
            if (Double.TryParse(value, out parseout))
            {
                e.Cancel = (parseout < 0.01 || parseout > 50.0);
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void syncEDSMSystemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AsyncSyncEDSMSystems();
        }

        private void AsyncSyncEDSMSystems()
        {
            var EDSMThread = new Thread(SyncAllEDSMSystems) { Name = "Downloading EDSM system" };
            EDSMThread.Start();
        }

        private void SyncAllEDSMSystems()
        {
            try
            {
                EDDBClass eddb = new EDDBClass();
                EDSMClass edsm = new EDSMClass();

                string edsmsystems = Path.Combine(Tools.GetAppDataDirectory(), "edsmsystems.json");
                bool newfile = false;
                string  rwsysfiletime = "2014-01-01 00:00:00";
                LogText("Get systems from EDSM." + Environment.NewLine);

                eddb.DownloadFile("http://www.edsm.net/dump/systemsWithCoordinates.json", edsmsystems, out newfile);

                if (newfile)
                {
                    LogText("Adding EDSM systems." + Environment.NewLine);
                    _db.GetAllSystems();
                    string json = LoadJsonFile(edsmsystems);
                    List<SystemClass> systems = SystemClass.ParseEDSM(json, ref rwsysfiletime);

                   
                    List<SystemClass> systems2Store = new List<SystemClass>();

                    foreach (SystemClass system in systems)
                    {
                        // Check if sys exists first
                        SystemClass sys = SystemData.GetSystem(system.name);
                        if (sys == null)
                            systems2Store.Add(system);
                        else if (!sys.name.Equals(system.name) || sys.x != system.x || sys.y!=system.y  || sys.z != system.z)  // Case or position changed
                            systems2Store.Add(system);
                    }
                    SystemClass.Store(systems2Store);
                    systems.Clear();
                    systems = null;
                    systems2Store.Clear();
                    systems2Store = null;
                    json = null;

                    _db.PutSettingString("EDSMLastSystems", rwsysfiletime);
                    _db.GetAllSystems();
                }
                else
                    LogText("No new file." + Environment.NewLine);

                string retstr = edsm.GetNewSystems(_db);
                Invoke((MethodInvoker)delegate
                {
                    TravelHistoryControl.LogText(retstr);
                });

                GC.Collect();
            }
            catch (Exception ex)
            {
                Invoke((MethodInvoker)delegate
                {
                    TravelHistoryControl.LogText("GetAllEDSMSystems exception:" + ex.Message + Environment.NewLine);
                });
            }

        }
        
        public void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (var control in controls)
            {
                if (control is GroupBox)
                {
                    var gb = control as GroupBox;
                    gb.ForeColor = EDDConfig.SelectedTheme.MainFontColor;
                }
                else if (control is TextBox)
                {
                    var tb = control as TextBox;
                    tb.ForeColor = EDDConfig.SelectedTheme.MainFontColor;
                    tb.BackColor = EDDConfig.SelectedTheme.SecondaryBackgroundColor;
                }
                else if (control is Button)
                {
                    var button = control as Button;
                    button.BackColor = EDDConfig.SelectedTheme.AccentColor;
                    button.UseVisualStyleBackColor = EDDConfig.SelectedTheme.UseVisualStyleBackColor;
                }
                else if (control is TabPage)
                {
                    var tab = control as TabPage;
                    tab.BackColor = EDDConfig.SelectedTheme.MainBackgroundColor;
                    tab.ForeColor = EDDConfig.SelectedTheme.MainFontColor;
                }
                else if (control is MenuStrip)
                {
                    var menu = control as MenuStrip;
                    menu.Renderer = EDDConfig.SelectedTheme.ToolStripRenderer;
                    menu.BackColor = EDDConfig.SelectedTheme.MainBackgroundColor;
                    menu.ForeColor = EDDConfig.SelectedTheme.MainFontColor;
                }
                else if (control is DataGridView)
                {
                    var dg = control as DataGridView;
                    dg.BackgroundColor = EDDConfig.SelectedTheme.GridBackgroundColor;
                    dg.ForeColor = EDDConfig.SelectedTheme.GridForeColor;
                    dg.GridColor = EDDConfig.SelectedTheme.GridColor;
                    dg.BackColor = EDDConfig.SelectedTheme.GridBackColor;
                    dg.DefaultCellStyle.BackColor = EDDConfig.SelectedTheme.GridCellBackColor;
                    dg.DefaultCellStyle.ForeColor = EDDConfig.SelectedTheme.GridCellForeColor;
                    dg.DefaultCellStyle.SelectionBackColor = EDDConfig.SelectedTheme.GridCellSelectionBackColor;
                    dg.DefaultCellStyle.SelectionForeColor = EDDConfig.SelectedTheme.GridCellSelectionForeColor;
                }
                else
                {
                    var c = control as Control;
                    c.BackColor = EDDConfig.SelectedTheme.MainBackgroundColor;
                    c.ForeColor = EDDConfig.SelectedTheme.MainFontColor;
                }

                if (control is Control)
                {
                    var container = control as Control;
                    ApplyThemeToControls(container.Controls);
                }
            }
        }
    }
}
