using System;
using System.Collections.Generic;
using System.Text;
using HttpServer;
using System.Net;
using System.Xml;
using System.IO;

namespace WebDAVSharp
{
    public class DeleteCommand : ICommand
    {
        WebDAVListener server;

        #region ICommand Members

        public void Start(WebDAVListener server, string path)
        {
            this.server = server;
            server.HttpServer.AddHandler("DELETE", null, path, DeleteHandler);
        }

        #endregion

        void DeleteHandler(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            string username;
            if (server.AuthenticateRequest(request, response, out username))
            {
                Dictionary<string, HttpStatusCode> multiStatus = null;
                HttpStatusCode status = server.DeleteResource(request.Uri, username, out multiStatus);
                response.Status = status;
                if (status == (HttpStatusCode)207 && multiStatus != null) //multiple status
                {
                    byte[] bytes = XmlResponse.WriteMultiStatusResponseBody(multiStatus);
                    response.ContentLength = bytes.Length;
                    //Console.WriteLine(Encoding.UTF8.GetString(bytes));
                    response.Body.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
