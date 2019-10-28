using System;
using System.IO;
using System.Threading;
using custom_message_based_implementation.consts;
using custom_message_based_implementation.encoding;
using custom_message_based_implementation.model;
using message_based_communication.model;
using message_based_communication.module;
using NLog;
using File = custom_message_based_implementation.model.File;

namespace file_servermodule
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string SELF_IP = "sip";
        private const string SELF_COMM_PORT = "scp";
        private const string ROUTER_IP = "rip";
        private const string ROUTER_COMM_PORT = "rcp";
        private const string ROUTER_REG_PORT = "rrp";
        private const string IS_LOCALHOST = "isLocal";

        public static bool IsLocalhost = true;
        public const bool IsTesting = false;

        public static void Main(string[] args)
        {
            SetupNLog();
            WriteLineAndLog("File servermodule is starting...");
            try
            {
                Port portToRegisterOn = new Port() { ThePort = 5523 };

                var router_conn_info = new ConnectionInformation()
                {
                    IP = new IP() { TheIP = null },
                    Port = new Port() { ThePort = 5522 } // is set after the reading of the system args because isLocalhost might change
                };

                var self_conn_info = new ConnectionInformation()
                {
                    IP = new IP() { TheIP = null }, // is set after the reading of the system args because isLocalhost might change
                    Port = new Port() { ThePort = 5569 } // todo port stuff
                };


                //setting network information with sys args
                foreach (var arg in args)
                {
                    var split = arg.Split(":");
                    if (2 != split.Length)
                    {
                        throw new ArgumentException("Got badly formatted system arguments");
                    }

                    if (split[0].Equals(SELF_IP)) // set self ip
                    {
                        self_conn_info.IP.TheIP = split[1];
                        WriteLineAndLog("Overriding self ip with: " + split[1]);
                    }
                    else if (split[0].Equals(SELF_COMM_PORT)) // set self communication port
                    {
                        self_conn_info.Port.ThePort = Convert.ToInt32(split[1]);
                        WriteLineAndLog("Overriding self communication port with: " + split[1]);
                    }
                    else if (split[0].Equals(ROUTER_IP)) // set router ip
                    {
                        router_conn_info.IP.TheIP = split[1];
                        WriteLineAndLog("Overriding router ip with: " + split[1]);
                    }
                    else if (split[0].Equals(ROUTER_COMM_PORT)) // set router communication port
                    {
                        router_conn_info.Port.ThePort = Convert.ToInt32(split[1]);
                        WriteLineAndLog("Overriding router communication port with: " + split[1]);
                    }
                    else if (split[0].Equals(ROUTER_REG_PORT)) // set router registration port
                    {
                        portToRegisterOn.ThePort = Convert.ToInt32(split[1]);
                        WriteLineAndLog("Overriding router registration port with: " + split[1]);
                    }
                    else if (split[0].Equals(IS_LOCALHOST))
                    {
                        if (split[1].Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        {
                            IsLocalhost = true;
                        }
                        else if (split[1].Equals("false", StringComparison.InvariantCultureIgnoreCase))
                        {
                            IsLocalhost = false;
                        }
                        else
                        {
                            throw new Exception("ERROR IN SYSTEM ARGUMENTS");
                        }
                    }
                }

                if (null == self_conn_info.IP.TheIP)
                {
                    self_conn_info.IP.TheIP = IsLocalhost ? "127.0.0.1" : BaseCommunicationModule.GetIP();
                }
                if (null == router_conn_info.IP.TheIP)
                {
                    router_conn_info.IP.TheIP = IsLocalhost ? "127.0.0.1" : "Fill in IP";
                }

                Console.WriteLine(
                    "\n\n Using the following network parameters: \n self network info: \n{ IP: " + self_conn_info.IP.TheIP + ", comm_port: " + self_conn_info.Port.ThePort + " }"
                    + "\n and router network information: \n{ IP: " + router_conn_info.IP.TheIP + ", comm_port: " + router_conn_info.Port.ThePort + ", reg_port: " + portToRegisterOn.ThePort + "}"
                    + "\n\n");

                var file = new FileServermodule(new ModuleType() { TypeID = ModuleTypeConst.MODULE_TYPE_FILE });
                file.Setup(router_conn_info, portToRegisterOn, self_conn_info, new CustomEncoder());

                WriteLineAndLog("File servermodule has started successfully with self IP: " + self_conn_info.IP.TheIP + " and server IP: " + router_conn_info.IP.TheIP);
                if (IsTesting)
                {
                    WriteLineAndLog("Testing enabled");
                    var path = AppDomain.CurrentDomain.BaseDirectory + "testGenerated.txt";
                    CreateTextFile(path);

                    var data = System.IO.File.ReadAllBytes(path);

                    var pk = new PrimaryKey {TheKey = 123};
                    var filename = new FileName {FileNameProp = "testUpload.txt"};
                    var filename2 = new FileName {FileNameProp = "testUpload2.txt"};

                    file.UploadFile(new File{FileName = filename, FileData = data}, pk, true);
                    file.UploadFile(new File { FileName = filename2, FileData = data }, pk, false);

                    var downloadedFile = file.DownloadFile(filename, pk);
                    path = AppDomain.CurrentDomain.BaseDirectory + downloadedFile.FileName.FileNameProp;
                    CreateTextFile(path, downloadedFile.FileData);

                    var files = file.GetListOfFiles(pk); // contains the two files created above
                    foreach (var f in files)
                    {
                        WriteLineAndLog(f.FileNameProp);
                    }
                    file.GetListOfFiles(new PrimaryKey { TheKey = 124 }); // directory does not exist
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "125");
                    file.GetListOfFiles(new PrimaryKey { TheKey = 125 }); // empty directory

                    file.RenameFile(filename, new FileName{FileNameProp = "renamed.txt"}, pk);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("File servermodule encountered an exception while in the setup process: " + ex);
                Logger.Debug(ex);
            }

            Console.WriteLine("Putting main thread to sleep in a loop");
            while (true)
            {
                Thread.Sleep(500);
            }
        }

        private static void CreateTextFile(string path, byte[] data = null)
        {
            if (!System.IO.File.Exists(path))
            {
                if(data == null) {
                    WriteLineAndLog("Creating new file: " + path);
                    using (var sw = System.IO.File.CreateText(path))
                    {
                        sw.WriteLine("The very first line!");
                    }
                } else {
                    WriteLineAndLog("Creating new file with data: " + path);
                    using (var fs = System.IO.File.Create(path))
                    {
                        fs.Write(data);
                    }
                }
            }
            else if (System.IO.File.Exists(path))
            {
                if (data == null)
                {
                    WriteLineAndLog("Appending existing file: " + path);
                    using (var sw = new StreamWriter(path, true))
                    {
                        sw.WriteLine("The next line!");
                    }
                }
                else
                {
                    WriteLineAndLog("Rewriting existing file with data: " + path);
                    using (var fs = System.IO.File.Open(path, FileMode.Create))
                    {
                        fs.Write(data);
                    }
                }
            }
        }

        private static void SetupNLog()
        {
            var config = new NLog.Config.LoggingConfiguration();
            var logFile = "file-servermodule-log.txt";

            /*
            var rootFolder = System.AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(rootFolder, logFile)))
            {  
                File.Delete(Path.Combine(rootFolder, logFile));
            }
            */

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = logFile };

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            // Apply config           
            LogManager.Configuration = config;
        }

        public static void WriteLineAndLog(string message)
        {
            Console.WriteLine(message);
            Logger.Info(message);
        }

    }
}
