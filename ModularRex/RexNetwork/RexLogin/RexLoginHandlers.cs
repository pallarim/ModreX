using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Net;
using System.Text;

using OpenSim.Server.Base;
using OpenSim.Server.Handlers.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nwc.XmlRpc;
using Nini.Config;
using log4net;

namespace ModularRex.RexNetwork.RexLogin
{
    public class RexLoginHandlers
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ILoginService m_LocalService;
        private IRexLoginService m_RexService;

        public RexLoginHandlers(ILoginService service, IRexLoginService rexService)
        {
            m_LocalService = service;
            m_RexService = rexService;
        }

        public XmlRpcResponse HandleXMLRPCLogin(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            Hashtable requestData = (Hashtable)request.Params[0];
            if (requestData != null)
            {

                bool IsRexLogin = (requestData.Contains("account") && requestData.Contains("sessionhash"));
                string clientVersion = "Unknown";

                if (requestData.Contains("version"))
                {
                    clientVersion = (string)requestData["version"];
                }

                if (!IsRexLogin)
                {
                    if (requestData.ContainsKey("first") && requestData["first"] != null &&
                        requestData.ContainsKey("last") && requestData["last"] != null &&
                        requestData.ContainsKey("passwd") && requestData["passwd"] != null)
                    {
                        string first = requestData["first"].ToString();
                        string last = requestData["last"].ToString();
                        string passwd = requestData["passwd"].ToString();
                        string startLocation = string.Empty;
                        if (requestData.ContainsKey("start"))
                            startLocation = requestData["start"].ToString();

                        m_log.InfoFormat("[LOGIN]: XMLRPC Login Requested for {0} {1}, starting in {2}, using {3}", first, last, startLocation, clientVersion);

                        LoginResponse reply = null;
                        reply = m_LocalService.Login(first, last, passwd, startLocation, UUID.Zero, clientVersion, remoteClient);

                        XmlRpcResponse response = new XmlRpcResponse();
                        response.Value = reply.ToHashtable();
                        Hashtable val = (Hashtable)response.Value;
                        val["rex"] = "running rex mode";
                        return response;
                    }
                }
                else
                {
                    string account = (string)requestData["account"];
                    string sessionHash = (string)requestData["sessionhash"];
                    string startLocation = string.Empty;
                    if (requestData.ContainsKey("start"))
                        startLocation = requestData["start"].ToString();
                    UUID scopeID = UUID.Zero;
                    if (requestData["scope_id"] != null)
                        scopeID = new UUID(requestData["scope_id"].ToString());

                    m_log.InfoFormat("[REX LOGIN BEGIN]: XMLRPC Received login request message from user '{0}' '{1}'", account, sessionHash);

                    LoginResponse reply = null;
                    reply = m_RexService.Login(account, sessionHash, startLocation, scopeID, clientVersion, remoteClient);
                    XmlRpcResponse response = new XmlRpcResponse();
                    response.Value = reply.ToHashtable();
                    return response;
                }
            }

            return FailedXMLRPCResponse();

        }

        

        private XmlRpcResponse FailedXMLRPCResponse()
        {
            Hashtable hash = new Hashtable();
            hash["reason"] = "key";
            hash["message"] = "Incomplete login credentials. Check your username and password.";
            hash["login"] = "false";

            XmlRpcResponse response = new XmlRpcResponse();
            response.Value = hash;

            return response;
        }
    }
}
