using System;
using System.Collections.Generic;
using System.Text;
using HttpServer;
using System.Net;

namespace WebDAVSharp
{
    public class CopyCommand : ICommand
    {
        WebDAVListener server;

        #region ICommand Members

        public void Start(WebDAVListener server, string path)
        {
            this.server = server;
            server.HttpServer.AddHandler("COPY", null, path, CopyHandler);
        }

        void CopyHandler(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            string username;
            if (server.AuthenticateRequest(request, response, out username))
            {
                //parse Destination from header
                //this is required for the copy command
                string[] destinations = request.Headers.GetValues("destination");
                if (destinations == null)
                {
                    response.Status = HttpStatusCode.BadRequest;
                    return;
                }
                string destination = destinations[0];

                //If the client includes a Depth header with an illegal value, the server should return 400 Bad Request.
                //this means that if the resource is a collection, then depth should be infinity
                //and if resource is not a collection, then the depth should be 0
                //if the depth doesn't exist, then proceed normally
                DepthHeader depth = DepthHeader.Infinity; //this is the standard default
                if (request.Headers["depth"] != null)
                {
                    depth = Depth.ParseDepth(request);
                }

                //parse Overwrite header
                //possible values: 'T' or 'F' (true or false)
                //otherwise return 400 Bad Request
                //if value is F and destination already exists, fail with response 412 Precondition Failed
                //default for this value is T
                bool overwrite = true;
                string[] overwrites = request.Headers.GetValues("overwrite");
                if (overwrites != null)
                {
                    if (overwrites[0].ToLower() == "t")
                        overwrite = true;
                    else if (overwrites[0].ToLower() == "f")
                        overwrite = false;
                    else
                    {
                        response.Status = HttpStatusCode.BadRequest;
                        return;
                    }
                }

                //If header might contain lock tokens, we need to pass them forward too
                string[] ifHeaders = request.Headers.GetValues("if");

                Dictionary<String, HttpStatusCode> multiStatusValues = null;
                HttpStatusCode status = server.OnCopyConnector(username, request.Uri, destination, depth, overwrite, ifHeaders, out multiStatusValues);
                response.Status = status;
                if (status == (HttpStatusCode)207 && multiStatusValues != null) //multiple status
                {
                    byte[] bytes = XmlResponse.WriteMultiStatusResponseBody(multiStatusValues);
                    response.ContentLength = bytes.Length;
                    //Console.WriteLine(Encoding.UTF8.GetString(bytes));
                    response.Body.Write(bytes, 0, bytes.Length);
                }
            }
        }

        #endregion
    }
}
