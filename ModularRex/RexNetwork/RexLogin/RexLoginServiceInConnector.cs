using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;

namespace ModularRex.RexNetwork.RexLogin
{
    //public class RexLoginServiceInConnector : ServiceConnector
    //{
        //private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //private ILoginService m_LLLoginService;
        //private IRexLoginService m_RexLoginService;

        //public RexLoginServiceInConnector(IConfigSource config, IHttpServer server, IScene scene) :
        //        base(config, server, String.Empty)
        //{
        //    m_log.Debug("[REXLOGIN IN CONNECTOR]: Starting...");
        //    string loginService = ReadLocalServiceFromConfig(config);

        //    ISimulationService simService = scene.RequestModuleInterface<ISimulationService>();
        //    ILibraryService libService = scene.RequestModuleInterface<ILibraryService>();

        //    Object[] args = new Object[] { config, simService, libService };
        //    m_LLLoginService = ServerUtils.LoadPlugin<ILoginService>(loginService, args);
        //    m_RexLoginService = ServerUtils.LoadPlugin<IRexLoginService>("ModularRex.dll", "RexLoginService", args);

        //    InitializeHandlers(server);
        //}

        //public RexLoginServiceInConnector(IConfigSource config, IHttpServer server) :
        //    base(config, server, String.Empty)
        //{
        //    string loginService = ReadLocalServiceFromConfig(config);

        //    Object[] args = new Object[] { config };

        //    m_LLLoginService = ServerUtils.LoadPlugin<ILoginService>(loginService, args);
        //    m_RexLoginService = ServerUtils.LoadPlugin<IRexLoginService>("ModularRex.dll", "RexLoginService", args);

        //    InitializeHandlers(server);
        //}

        //private string ReadLocalServiceFromConfig(IConfigSource config)
        //{
        //    IConfig serverConfig = config.Configs["LoginService"];
        //    if (serverConfig == null)
        //        throw new Exception(String.Format("No section LoginService in config file"));

        //    string loginService = serverConfig.GetString("LocalServiceModule", String.Empty);
        //    if (loginService == string.Empty)
        //        throw new Exception(String.Format("No LocalServiceModule for LoginService in config file"));

        //    return loginService;
        //}

        //private void InitializeHandlers(IHttpServer server)
        //{
        //    RexLoginHandlers loginHandlers = new RexLoginHandlers(m_LLLoginService, m_RexLoginService);
        //    server.AddXmlRPCHandler("login_to_simulator", loginHandlers.HandleXMLRPCLogin, false);
        //}
    //}
}
