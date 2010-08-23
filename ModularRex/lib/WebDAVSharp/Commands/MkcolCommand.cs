using System;
using System.Collections.Generic;
using System.Text;
using HttpServer;

namespace WebDAVSharp
{
    public class MkcolCommand : ICommand
    {
        WebDAVListener server;

        #region ICommand Members

        public void Start(WebDAVListener server, string path)
        {
            this.server = server;
            server.HttpServer.AddHandler("MKCOL", null, path, MkcolHandler);
        }

        void MkcolHandler(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            string username;
            if (server.AuthenticateRequest(request, response, out username))
            {
                System.Net.HttpStatusCode status = server.CreateCollection(request.UriPath, username);
                response.Status = status;
            }
        }

        #endregion
    }
}
