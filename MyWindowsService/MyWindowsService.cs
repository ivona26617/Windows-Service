    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceProcess;
    using System.Text;
    using System.Timers;
    using System.Data.SQLite;
    using System.IO;
    using System.Management;

    namespace MyWindowsService
    {
        public partial class MyWindowsService : ServiceBase
        {
            private int eventId = 1;

            PerformanceCounter cpuCounter;
            PerformanceCounter ramCounter;
            PerformanceCounter diskCounter;

            //defined an absolute path because it fails with a relative one
            SQLiteConnection connection = new SQLiteConnection("Data Source = C:\\Users\\Ivona\\source\\repos\\MyWindowsService\\MyWindowsService\\bin\\Debug\\DataBase.db; Version = 3; ");
        
            public MyWindowsService()
            {
                InitializeComponent();
                eventLog1 = new System.Diagnostics.EventLog();
         
                if (!System.Diagnostics.EventLog.SourceExists("MySource"))
                {
                   System.Diagnostics.EventLog.CreateEventSource(
                        "MySource", "MyNewLog");
                }
                eventLog1.Source = "MySource";
                eventLog1.Log = "MyNewLog";

                //creating instance of the PerformanceCounter class for obtaining CPU, DISK and MEMORY utilization 
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                diskCounter= new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            }


            protected override void OnStart(string[] args)
            {
                //System.Diagnostics.Debugger.Launch();
                //setting a timer that will trigger every 5 minutes
                Timer timer = new Timer();
                timer.Interval = 300000;
                timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
                timer.Start();

                eventLog1.WriteEntry("In OnStart.");

                //adding Hardware Types and initial Records to DataBase
                addHardwareType("CPU", cpuInfo());
                addHardwareType("RAM", ramInfo());
                addHardwareType("DISK", diskInfo());
                addRecord(1, getCurrentCpuUsage(), DateTime.Now.ToString());
                addRecord(2, getAvailableRAM(), DateTime.Now.ToString());
                addRecord(3, getDiskUtil(), DateTime.Now.ToString());

            }

            protected override void OnStop()
            {
                eventLog1.WriteEntry("In OnStop.");
            }

            //adding Records to DataBase every 5 minutes
            public void OnTimer(object sender, ElapsedEventArgs args)
            {
                eventLog1.WriteEntry("Monitoring the System- Cpu usage: "+getCurrentCpuUsage().ToString()+", Available RAM: "+getAvailableRAM().ToString()+ ", Disk utilization: " + getDiskUtil().ToString(), EventLogEntryType.Information, eventId++);

                addRecord(1, getCurrentCpuUsage(), DateTime.Now.ToString());
                addRecord(2, getAvailableRAM(), DateTime.Now.ToString());
                addRecord(3, getDiskUtil(), DateTime.Now.ToString());
            }

            //methods for obtaining a counter sample and returning the calculated value for it
            public double getCurrentCpuUsage()
            {
                return cpuCounter.NextValue();
            }

            public double getAvailableRAM()
            {
                return ramCounter.NextValue();
            }

            public double getDiskUtil()
            {
                return diskCounter.NextValue();
            }

            
            /*public void cpuAllInfo()
            {
                ManagementObjectSearcher query = new ManagementObjectSearcher(
                "select * from Win32_Processor");

                ManagementObjectCollection queryCollection = query.Get();

                ManagementObject mo = queryCollection.OfType<ManagementObject>().FirstOrDefault();

                HardwareType cpu = new HardwareType((int)mo["SerialNumber"], mo["Model"].ToString(), mo["Name"].ToString());

                addHardwareType(cpu.Id, cpu.Model, cpu.AdditionalInfo);
            }*/

            //method that returns information about the processor via the WMI class Win32_Processor 
            public string cpuInfo()
            {
                ManagementObjectSearcher query = new ManagementObjectSearcher(
                "select * from Win32_Processor");

                ManagementObjectCollection queryCollection = query.Get();

                ManagementObject mo = queryCollection.OfType<ManagementObject>().FirstOrDefault();

                return mo["Name"].ToString();
            }

            //method that returns information about the disk via the WMI class Win32_DiskDrive 
            public string diskInfo()
            {
                ManagementObjectSearcher query = new ManagementObjectSearcher(
                "select * from Win32_DiskDrive");

                 ManagementObjectCollection queryCollection = query.Get();

                ManagementObject mo = queryCollection.OfType<ManagementObject>().FirstOrDefault();

                return mo["Model"].ToString();
            }

            //method that returns information about the memory via the WMI class Win32_PhysicalMemory
            public string ramInfo()
            {
                ManagementObjectSearcher query = new ManagementObjectSearcher(
                "select * from Win32_PhysicalMemory");

                ManagementObjectCollection queryCollection = query.Get();

                ManagementObject mo = queryCollection.OfType<ManagementObject>().FirstOrDefault();

                return mo["Name"].ToString()+ " "+ mo["TypeDetail"].ToString();
            }

            //inserting data into HardwareType table
            public void addHardwareType(string model, string info)
            {
                connection.Open();

                try
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append("INSERT INTO HardwareType");
                    sb.Append("(Model, AdditionalInfo) ");
                    sb.Append("VALUES (@model, @info) ");
                    sb.Append(";SELECT @ID =@@IDENTITY"); //ID autoincrement

                    SQLiteCommand command = new SQLiteCommand(sb.ToString(), connection);

                    //defining and adding parameters
                    SQLiteParameter idParam = new SQLiteParameter("@id");
                    SQLiteParameter modelParam = new SQLiteParameter("@model", model);
                    SQLiteParameter infoParam = new SQLiteParameter("@info", info);

                    command.Parameters.Add(modelParam);
                    command.Parameters.Add(infoParam);
                    command.Parameters.Add(idParam);

                    command.ExecuteNonQuery();
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.Message);
                }
                finally
                {
                    connection.Close();
                }
            }

            //inserting data into Record table
            public void addRecord(int type, double value, string time)
            {
                connection.Open();

                try
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append("INSERT INTO Record");
                    sb.Append("(HardwareType, Value, CreateDate) ");
                    sb.Append("VALUES (@hardwareType, @value, @createDate) ");
                    sb.Append(";SELECT @ID =@@IDENTITY"); //ID autoincrement

                    SQLiteCommand command = new SQLiteCommand(sb.ToString(), connection);

                    SQLiteParameter idParam = new SQLiteParameter("@id");
                    SQLiteParameter typeParam = new SQLiteParameter("@hardwareType",type);
                    SQLiteParameter valueParam = new SQLiteParameter("@value",value);
                    SQLiteParameter timeParam = new SQLiteParameter("@createDate", time);

                    command.Parameters.Add(typeParam);
                    command.Parameters.Add(valueParam);
                    command.Parameters.Add(timeParam);
                    command.Parameters.Add(idParam);

                    command.ExecuteNonQuery();
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.Message);
                }
                finally
                {
                    connection.Close();
                }
            }

            //creating Report folder in the directory and ReportFile
            public void WriteToFile(string Message)
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\Report";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Report\\ReportFile_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
                if (!File.Exists(filepath))
                {
                    var drives = DriveInfo.GetDrives();
                    // Create a file to write to.   
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
            }
        }
    }
