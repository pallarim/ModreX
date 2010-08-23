using System;
using System.Collections.Generic;
using System.Text;
using HttpServer;
using System.Net;
using System.IO;
using System.Xml;

namespace WebDAVSharp
{
    public class MoveCommand : ICommand
    {
        WebDAVListener server;

        #region ICommand Members

        public void Start(WebDAVListener server, string path)
        {
            this.server = server;
            server.HttpServer.AddHandler("MOVE", null, path, MoveHandler);
        }

        void MoveHandler(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            string username;
            if (server.AuthenticateRequest(request, response, out username))
            {
                //parse Destination from header
                //this is required for the move command
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
                HttpStatusCode status;
                if (!CheckSrcAndDst(request.Uri, destination))
                    status = HttpStatusCode.BadGateway;
                else
                    status = server.OnMoveConnector(username, request.Uri, destination, depth, overwrite, ifHeaders, out multiStatusValues);

                response.Status = status;
                if (status == (HttpStatusCode)207 && multiStatusValues != null) //multiple status
                {
                    //Add status information XML and add that XML to body
                    using (MemoryStream responseStream = new MemoryStream())
                    {
                        XmlTextWriter xmlWriter = new XmlTextWriter(responseStream, Encoding.ASCII); //, Encoding.UTF8);

                        xmlWriter.Formatting = Formatting.Indented;
                        xmlWriter.IndentChar = '\t';
                        xmlWriter.Indentation = 1;
                        xmlWriter.WriteStartDocument();
                        xmlWriter.WriteStartElement("D", "multistatus", "DAV:");
                        xmlWriter.WriteStartElement("response", "DAV:");

                        //Due the error mimimazion rule, only one error is returned in multi-response
                        // see: http://www.webdav.org/specs/rfc2518.html#copy.for.collections
                        foreach (KeyValuePair<string, HttpStatusCode> kvp in multiStatusValues)
                        {
                            xmlWriter.WriteElementString("href", "DAV:", kvp.Key);
                            xmlWriter.WriteElementString("status", "DAV:", Utils.GetHttpStatusString(kvp.Value));
                            break;
                        }

                        xmlWriter.WriteEndElement(); //response
                        xmlWriter.WriteEndElement(); // multistatus
                        xmlWriter.WriteEndDocument();
                        xmlWriter.Flush();

                        byte[] bytes = responseStream.ToArray();
                        response.ContentLength = bytes.Length;
                        //Console.WriteLine(Encoding.UTF8.GetString(bytes));
                        response.Body.Write(bytes, 0, bytes.Length);
                    }
                }
            }
        }

        private bool CheckSrcAndDst(Uri source, string destination)
        {
            string[] srcParts = source.ToString().Split('/');
            string[] dstParts = destination.Split('/');
            for (int i = 0; i < 3; i++)
            {
                if (srcParts[i] != dstParts[i])
                {
                    //error, some of the following has happened
                    //a) source or destination did not contain the whole absolute uri
                    //b) source and destination use different protocol http vs. https
                    //c) source and destination are in different domain
                    Console.WriteLine("Error occurred while comparing source and destination uris. Source part {0}, Destination part {1}",
                        srcParts[i], dstParts[i]);
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
