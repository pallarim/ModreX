using System;
using System.Collections.Generic;
using System.Text;
using HttpServer;
using System.Xml.XPath;
using System.Net;
using System.IO;
using System.Xml;

namespace WebDAVSharp
{
    public class PropPatchCommand : ICommand
    {
        #region ICommand Members

        WebDAVListener server;

        public void Start(WebDAVListener server, string path)
        {
            this.server = server;

            server.HttpServer.AddHandler("PROPPATCH", null, path, PropPatchHandler);
        }

        void PropPatchHandler(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            //parse following information
            // uri string/uri from header
            // namespace string from body xml
            string nspace = String.Empty;
            // set dictioany<property, value> from body xml
            Dictionary<string, string> setProperties = new Dictionary<string, string>();
            // remove list<property> from body xml
            List<string> removeProperties = new List<string>();

            Dictionary<string, HttpStatusCode> multiStatus = new Dictionary<string, HttpStatusCode>();

            string username;
            if (server.AuthenticateRequest(request, response, out username))
            {
                if (request.ContentLength != 0)
                {
                    #region parse body
                    XPathNavigator requestNavigator = new XPathDocument(request.Body).CreateNavigator();
                    XPathNodeIterator propNodeIterator = requestNavigator.SelectDescendants("propertyupdate", "DAV:", false);
                    if (propNodeIterator.MoveNext())
                    {
                        XPathNodeIterator nodeChildren = propNodeIterator.Current.SelectChildren(XPathNodeType.All);
                        while (nodeChildren.MoveNext())
                        {
                            XPathNavigator currentNode = nodeChildren.Current;
                            if (currentNode.NodeType == XPathNodeType.Element && currentNode.LocalName.ToLower() == "set")
                            {
                                if (currentNode.MoveToFirstChild())
                                {
                                    if (currentNode.MoveToFirstChild())
                                    {
                                        nspace = currentNode.NamespaceURI;
                                        setProperties.Add(currentNode.LocalName, currentNode.Value);
                                        while (currentNode.MoveToNext())
                                        {
                                            setProperties.Add(currentNode.LocalName, currentNode.Value);
                                        }
                                        currentNode.MoveToParent();
                                    }
                                    currentNode.MoveToParent();
                                }
                            }

                            if (currentNode.NodeType == XPathNodeType.Element && currentNode.LocalName.ToLower() == "remove")
                            {
                                if (currentNode.MoveToFirstChild())
                                {
                                    if (currentNode.MoveToFirstChild())
                                    {
                                        nspace = currentNode.NamespaceURI;
                                        removeProperties.Add(currentNode.LocalName);
                                        while (currentNode.MoveToNext())
                                        {
                                            removeProperties.Add(currentNode.LocalName);
                                        }
                                        currentNode.MoveToParent();
                                    }
                                    currentNode.MoveToParent();
                                }
                            }
                        }
                    }
                    #endregion

                    HttpStatusCode status = server.OnPropPatchConnector(username, request.Uri, request.UriPath, nspace, setProperties, removeProperties, out multiStatus);
                    response.Status = status;
                    if (status == (HttpStatusCode)207 && multiStatus != null)
                    {
                        byte[] bytes = WriteMultiStatusResponseBody(request.Uri.ToString(), nspace, multiStatus);
                        response.ContentLength = bytes.Length;
                        //Console.WriteLine(Encoding.UTF8.GetString(bytes));
                        response.Body.Write(bytes, 0, bytes.Length);
                    }
                }
                else
                    response.Status = HttpStatusCode.BadRequest;
            }
            else
                response.Status = HttpStatusCode.Unauthorized;

        }

        public static byte[] WriteMultiStatusResponseBody(string href, string nspace, Dictionary<string, HttpStatusCode> multiStatus)
        {
            byte[] bytes;
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

                xmlWriter.WriteElementString("href", "DAV:", href);

                foreach (KeyValuePair<string, HttpStatusCode> kvp in multiStatus)
                {
                    xmlWriter.WriteStartElement("propstat", "DAV:");
                    xmlWriter.WriteStartElement("prop", "DAV:");
                    xmlWriter.WriteElementString(kvp.Key, nspace, String.Empty);
                    xmlWriter.WriteEndElement();//prop
                    xmlWriter.WriteElementString("status", "DAV:", Utils.GetHttpStatusString(kvp.Value));
                    xmlWriter.WriteEndElement();//propstat
                }

                xmlWriter.WriteEndElement(); //response
                xmlWriter.WriteEndElement(); // multistatus
                xmlWriter.WriteEndDocument();
                xmlWriter.Flush();

                bytes = responseStream.ToArray();
            }
            return bytes;
        }

        #endregion
    }
}
