using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using log4net;
using System.Reflection;
using System.IO;
//using Caps = OpenSim.Framework.Capabilities.Caps;
using OpenSim.Framework.Servers.HttpServer;
using Ionic.Zip;
using System.Drawing;
using OpenSim.Framework;
using ModularRex.RexFramework;


namespace OgreSceneImporter
{
    public delegate byte[] HttpRequestCallback(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse);

    public class StreamHandler : BaseStreamHandler
    {
        private HttpRequestCallback m_callback;

        public override string ContentType { get { return null; } }

        public StreamHandler(string httpMethod, string path, HttpRequestCallback callback) :
            base(httpMethod, path)
        {
            m_callback = callback;
        }

        public override byte[] Handle(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            return m_callback(path, request, httpRequest, httpResponse);
        }
    }


    /// <summary>
    /// Handle adding caps handlers for uploading scenes, when user, with rights to upload scene files, logs in,
    /// + handle uploads
    /// </summary>
    public class UploadHandler
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;
        private OgreSceneImportModule m_osi;

        private const string EXTRACT_FOLDER_NAME = "SceneUploadZipFiles";

        public void AddUploadCap(Scene scene, OgreSceneImportModule osi)
        {
            m_scene = scene;
            m_osi = osi;
            scene.EventManager.OnRegisterCaps += this.RegisterCaps;
        }

        public void RegisterCaps(UUID agentID, OpenSim.Framework.Capabilities.Caps caps)
        {
            if (CheckRights(agentID))
            {
                UUID capID = UUID.Random();
                m_log.InfoFormat("[OGRESCENE]: Creating capability: /CAPS/{0}", capID);
                caps.RegisterHandler("UploadScene", new StreamHandler("POST", "/CAPS/" + capID, ProcessUploadScene));
            }
        }

        private byte[] ProcessUploadScene(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            byte[] data = httpRequest.GetBody();
            
            m_log.Info("[OGRESCENE]: Processing UploadScene packet");

            string outputFile = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "SceneUploadZipFile.zip";

            m_log.Info("[OGRESCENE]: Writing zip file");

            try
            {
                // need to parse these out from beginning
                //--0446c18b14c143c3bd0e787cb6ea4c09
                //Content-Disposition: form-data; name="uploadscene"; filename="C:/CODE/NaaliGit2/naali/bin/test3.scene.zip"
                //Content-Type: application/zip
                //Content-Length: 10144449

                // need to parse this out from end
                //--0446c18b14c143c3bd0e787cb6ea4c09--

                //System.Collections.Specialized.NameValueCollection nvc = httpRequest.Headers;
                //foreach (string key in nvc.AllKeys)
                //{
                //    m_log.Info(key);
                //}

                byte[] content = ReadContent(data);

                BinaryWriter bw = null;
                try
                {
                    bw = new BinaryWriter(File.Open(outputFile, FileMode.Create));
                    bw.Write(content);
                    bw.Flush();
                    bw.Close();

                }
                catch (Exception)
                {
                    if (bw != null) { bw.Close(); }
                    throw;
                }
                RemoveFolder(EXTRACT_FOLDER_NAME);

                Ionic.Zip.ZipFile f = null;
                try
                {
                    f = Ionic.Zip.ZipFile.Read("SceneUploadZipFile.zip");
                    f.ExtractAll(EXTRACT_FOLDER_NAME);
                    f.Dispose();

                }
                catch (Exception)
                {
                    if (f != null)
                        f.Dispose();
                    throw;
                }
                LoadScene(EXTRACT_FOLDER_NAME);

                string respstring = "<Root>";
                respstring += "Scene upload done";
                respstring += "</Root>";
                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                RemoveFolder(EXTRACT_FOLDER_NAME);
                return encoding.GetBytes(respstring);
            }
            catch (Exception exp)
            {
                m_log.ErrorFormat("[OGRESCENE]: Failing to parse upload scene package: {0}\n"
                    +"StackTrace: {1}", exp.Message, exp.StackTrace);
                string respstring = "<Root>";
                respstring += exp.Message;
                respstring += "</Root>";
                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                RemoveFolder(EXTRACT_FOLDER_NAME);
                return encoding.GetBytes(respstring);
            }
        }

        private void RemoveFolder(string folder)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), folder);
            if (System.IO.Directory.Exists(path))
                System.IO.Directory.Delete(path, true);        
        }

        private bool CheckRights(UUID agentID)
        {
            // currently only owner is able to upload scenes
            if (agentID == m_scene.RegionInfo.EstateSettings.EstateOwner)
                return true;
            UUID[] managers = m_scene.RegionInfo.EstateSettings.EstateManagers;
            foreach (UUID id in managers) 
            {
                if (id == agentID)
                    return true;
            }

            return false;
        }

        private byte[] ReadContent(byte[] data)
        {
            MemoryStream mstream = new MemoryStream(data);
            System.IO.BinaryReader reader = new BinaryReader(mstream);
            //reader.BaseStream.Position
            byte[] startBytes;
            if (mstream.Length > 500)
            {
                startBytes = reader.ReadBytes(500);
            }
            else
            {
                startBytes = reader.ReadBytes((int)mstream.Length);
            }
            string startString = System.Text.Encoding.ASCII.GetString(startBytes).ToString();

            // find 2 linefeeds in a row, marks the end of headers
            int startIndex = 0;
            bool crlf = false;
            int LF_index = startString.IndexOf("\n", 0);
            int CRLF_index = startString.IndexOf("\r\n", 0);
            if (CRLF_index != -1)
            {
                crlf = true;
            }

            if (crlf)
            {
                startIndex = startString.IndexOf("\r\n\r\n", 38);
            }
            else
            {
                startIndex = startString.IndexOf("\n\n", 36);
            }
            
            int contentStartIndex;
            if(crlf)
                contentStartIndex= startIndex + 4;
            else
                contentStartIndex = startIndex + 2;
            int contentEndIndex = (int)mstream.Length - 38; // 38 = length of "--uuid--"
            int byteCount = contentEndIndex - contentStartIndex;
            byte[] fileBuffer = new byte[byteCount];

            Buffer.BlockCopy(data, contentStartIndex, fileBuffer, 0, byteCount);

            return fileBuffer;
        }

        private void LoadScene(string extractFolderName)
        {
            m_log.Info("[OGRESCENE]: LoadScene");
            string packagePath = Path.Combine(extractFolderName, "UploadPackage");
            DirectoryInfo di = new DirectoryInfo(packagePath);
            // find .scene file, should be only one file
            FileInfo fi = di.GetFiles("*.scene")[0];
            string path = fi.FullName;
            
            string loadName = path.Substring(0, path.Length - 6);
            m_log.InfoFormat("[OGRESCENE]: Loading scene file: {0}", fi.FullName);
            CreateSceneMaterialFileIfNeeded(fi.Name, di);
            try
            {
                m_osi.ImportUploadedOgreScene(loadName);
            }
            catch (Exception exp)
            {
                m_log.ErrorFormat("[OGRESCENE]: Error importing uploaded ogre scene: {0}, {1}", exp.Message, exp.StackTrace);
            }
        }

        private void CreateSceneMaterialFileIfNeeded(string scene, DirectoryInfo di)
        {
            string materialname = scene.Substring(0, scene.Length - 6) + ".material";
            bool exist = false;
            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name == materialname) { exist = true; break; }
            }
            if (!exist)
            {
                FileStream fs = null;
                // create scene material file from other material files
                try
                {
                    string dirPath = di.FullName;
                    m_log.InfoFormat("[OGRESCENE]: Creating material file: {0}", Path.Combine(dirPath, materialname));
                    fs = File.Create(Path.Combine(dirPath, materialname));
                    StreamWriter mwriter = new StreamWriter(fs);
                    foreach (FileInfo mfile in di.GetFiles("*.material"))
                    {
                        if (mfile.Name != materialname)
                        {
                            m_log.InfoFormat("[OGRESCENE]: Adding material file: {0}", mfile.Name);
                            FileStream mfs = mfile.Open(FileMode.Open);
                            StreamReader sr = new StreamReader(mfs);
                            string line;
                            try
                            {
                                while ((line = sr.ReadLine()) != null)
                                {
                                    if (line.StartsWith("material"))
                                    {
                                        string[] spl = line.Split(':');
                                        if (spl.Length > 1)
                                        {
                                            // Try reading the inherited material from OgreMaterials folder and add it to stream
                                            // Try reading the inherited material from upload folder and add it to stream
                                            string baseMaterialContent = ReadBaseMaterial(spl[1].Trim(), dirPath);
                                            line = spl[0].Trim();
                                            mwriter.WriteLine(line);
                                            if ((line = sr.ReadLine()).StartsWith("{"))
                                            {
                                                mwriter.WriteLine(line);
                                            }
                                            else
                                            {
                                                throw new Exception("malformed .material file");
                                            }
                                            mwriter.WriteLine(baseMaterialContent);
                                        }
                                        else
                                        {
                                            mwriter.WriteLine(line);
                                        }
                                    }
                                    else
                                    {
                                        mwriter.WriteLine(line);
                                    }
                                }
                                mwriter.Flush();
                                mfs.Close();
                            }
                            catch (Exception exp)
                            {
                                m_log.WarnFormat("[OGRESCENE]: Warning, Error while creating material file {0}" 
                                     + "StackTrace {1}", exp.Message, exp.StackTrace);
                                if (mfs != null)
                                {
                                    mfs.Close();
                                }
                            }                                                        
                        }
                    }
                    mwriter.Flush();
                    mwriter.Close();

                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("[OGRESCENE]: Failing to create material file: {0}" +
                                      "StackTrace: {1}", e.Message, e.StackTrace);
                    if(fs!=null)
                        fs.Close();
                }
            }
        }

        private string ReadBaseMaterial(string name, string dirPath)
        {
            string content = "";
            StreamReader sr = null;
            try
            {
                string path = Path.Combine(dirPath, name + ".material");
                if (File.Exists(path))
                {
                    sr = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read));
                    content = sr.ReadToEnd();
                    content = ParseInnerContent(content, name);
                    sr.Close();

                }
            }
            catch (Exception exp)
            {
                m_log.WarnFormat("Failing to read basematerial {0} \n" +
                    "StackTrace: {1} \n" +
                    "returning empty string", exp.Message, exp.StackTrace);
                if (sr != null)
                {
                    sr.Close();
                }
            }
            return content;
        }

        private string ParseInnerContent(string content, string name)
        {
            int materialIndex = content.IndexOf("material " + name);
            int start = content.IndexOf("{", materialIndex);
            int stop = content.LastIndexOf("}");
            int len = stop - start;
            return content.Substring(start + 1, len - 2);
        }

    }
}
