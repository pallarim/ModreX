using System;
using System.Collections.Generic;
using System.Text;
using log4net.Config;
using Nini.Config;
using OpenSim;
using OpenSim.Framework;

namespace ModularRex.Tools.MigrationTool
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ArgvConfigSource configSource = new ArgvConfigSource(args);

            XmlConfigurator.Configure();

            configSource.AddSwitch("Startup", "inifile");
            configSource.AddSwitch("Startup", "inimaster");
            configSource.AddSwitch("Startup", "inidirectory");

            ConfigurationLoader configurationLoader = new ConfigurationLoader();
            ConfigSettings appSettings;
            NetworkServersInfo networkSettings;
            configurationLoader.LoadConfigSettings(configSource, out appSettings, out networkSettings);

            MigrationWorker app = new MigrationWorker(configSource);
            app.Start();

            Console.WriteLine("Hit enter to quit");
            Console.ReadLine();
        }
    }
}
