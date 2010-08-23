using System;
using System.Collections.Generic;
using System.Text;
using HttpServer;
using System.Net;

namespace WebDAVSharp
{
    public class UnlockCommand : ICommand
    {
        WebDAVListener server;

        #region ICommand Members

        public void Start(WebDAVListener server, string path)
        {
            this.server = server;

            server.HttpServer.AddHandler("UNLOCK", null, path, UnlockHandler);
        }

        #endregion

        void UnlockHandler(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            string username;
            if (server.AuthenticateRequest(request, response, out username))
            {
                string locktoken = (request.Headers.GetValues("Lock-Token"))[0];
                if (locktoken == null || locktoken == String.Empty)
                {
                    response.Status = HttpStatusCode.BadRequest;
                }
                else
                {
                    HttpStatusCode code = server.OnUnlockConnector(request.UriPath, locktoken, username);
                    response.Status = code;
                }
            }
            else
            {
                response.Status = HttpStatusCode.Unauthorized;
            }
        }
    }
}
