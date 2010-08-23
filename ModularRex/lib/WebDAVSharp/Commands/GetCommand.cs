using System;
using System.Collections.Generic;
using System.Text;
using HttpServer;

namespace WebDAVSharp
{
    public class GetCommand : ICommand
    {
        WebDAVListener server;

        #region ICommand Members

        public void Start(WebDAVListener server, string path)
        {
            this.server = server;
            server.HttpServer.AddHandler("GET", null, path, GetHandler);
        }

        #endregion

        void GetHandler(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            string username;
            if (server.AuthenticateRequest(request, response, out username))
            {
                System.Net.HttpStatusCode status = server.GET(response, request.UriPath, username);
                response.Status = status;
            }
        }


    }
}
