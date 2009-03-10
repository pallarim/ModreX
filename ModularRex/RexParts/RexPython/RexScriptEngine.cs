using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using OpenSim.Framework;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Scripting;
using OpenMetaverse;
using Nini.Config;

namespace ModularRex.RexParts.RexPython
{
    public class RexScriptEngine : IRegionModule
    {
        public RexScriptInterface mCSharp;

        internal Scene World; 
        internal RexEventManager m_EventManager;

        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private IronPython.Hosting.PythonEngine mPython = null;
        private bool m_PythonEnabled;
        private bool m_EngineStarted;
        private ModrexObjects m_rexObjects;

        public RexScriptEngine()
        {
        }

        public log4net.ILog Log
        {
            get { return m_log; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public bool IsPythonEnabled
        {
            get { return m_PythonEnabled; }
        }

        public bool IsEngineStarted
        {
            get { return m_EngineStarted; }
        }


        public void Initialise(Scene scene, IConfigSource config)
        {
            try
            {
                m_PythonEnabled = config.Configs["Startup"].GetBoolean("rex_python", false);
            }
            catch (Exception)
            {
                m_PythonEnabled = true;
            }
            World = scene;
        }

        public void PostInitialise()
        {
            OpenSim.Region.Framework.Interfaces.IRegionModule module = World.Modules["RexObjectsModule"];
            if (module != null && module is ModrexObjects)
            {
                m_rexObjects = (ModrexObjects)module;
            }

            InitializeEngine(World);
        }

        public void CloseDown()
        {
        }

        public string GetName()
        {
            return "RexPythonScriptModule";
        }

        public void InitializeEngine(Scene Sceneworld)
        {
            if (m_PythonEnabled)
            {
                Log.InfoFormat("[RexScriptEngine]: Rex PythonScriptEngine initializing");

                RexScriptAccess.MyScriptAccess = new RexPythonScriptAccessImpl(this);
                m_EventManager = new RexEventManager(this);
                mCSharp = new RexScriptInterface(null, null, 0, UUID.Zero, this);
                StartPythonEngine();
            }
            else
                Log.InfoFormat("[RexScriptEngine]: Rex PythonScriptEngine disabled");
       }

        public void StartPythonEngine()
        {
            try
            {
                Log.InfoFormat("[RexScriptEngine]: IronPython init");
                m_EngineStarted = false;
                bool bNewEngine = true;

                if (mPython == null)
                {
                    IronPython.Hosting.EngineOptions engineOptions = new IronPython.Hosting.EngineOptions();
                    engineOptions.ClrDebuggingEnabled = false;
                    IronPython.Compiler.Options.Verbose = false;
                    IronPython.Compiler.Options.GenerateModulesAsSnippets = true;
                    mPython = new IronPython.Hosting.PythonEngine(engineOptions);
                }
                else
                    bNewEngine = false;

                // Add script folder paths to python path
                mPython.AddToPath(AppDomain.CurrentDomain.BaseDirectory);

                string rexdlldir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScriptEngines");
                mPython.AddToPath(rexdlldir);

                string PytProjectPath = Path.Combine(rexdlldir, "PythonScript");
                mPython.AddToPath(PytProjectPath);

                DirectoryInfo TempDirInfo = new DirectoryInfo(@PytProjectPath);
                DirectoryInfo[] dirs = TempDirInfo.GetDirectories("*.*");
                string TempPath = "";
                foreach (DirectoryInfo dir in dirs)
                {
                    TempPath = Path.Combine(PytProjectPath, dir.Name);
                    mPython.AddToPath(TempPath);
                }
                String PytLibPath = Path.Combine(rexdlldir, "Lib");
                mPython.AddToPath(PytLibPath);

                // Import Core and init
                mPython.Execute("from RXCore import *"); 
                if (!bNewEngine)
                {
                    ExecutePythonStartCommand("reload(rxlslobject)");
                    ExecutePythonStartCommand("reload(rxactor)");
                    ExecutePythonStartCommand("reload(rxavatar)");
                    ExecutePythonStartCommand("reload(rxbot)");
                    ExecutePythonStartCommand("reload(rxevent)");
                    ExecutePythonStartCommand("reload(rxeventmanager)");
                    ExecutePythonStartCommand("reload(rxworld)");
                    ExecutePythonStartCommand("reload(rxworldinfo)");                    
                    ExecutePythonStartCommand("reload(rxtimer)");                       
                    ExecutePythonStartCommand("reload(rxX10)");              
                }
                mPython.Globals.Add("objCSharp", mCSharp);
                mPython.ExecuteFile(PytProjectPath + "/RXCore/rxmain.py"); // tucofixme, possible error with path???

                // Import other packages
                foreach (DirectoryInfo dir in dirs)
                {
                    if (dir.Name.IndexOf(".") != -1)
                        continue;
                    else if (dir.Name.Length >= 6 && dir.Name.Substring(0, 6).ToLower() == "rxcore")
                        continue;
                    else
                    {
                        mPython.Execute("from " + dir.Name + " import *");
                        if (!bNewEngine)
                        {
                            FileInfo[] files = dir.GetFiles("*.py");
                            foreach (FileInfo file in files)
                            {
                                if (file.Name.ToLower() == "__init__.py")
                                    continue;
                                else
                                    ExecutePythonStartCommand("reload(" + file.Name.Substring(0, file.Name.Length - 3) + ")");
                            }
                        }
                    }
                }

                // Create objects
                string PythonClassName = "";
                string PythonTag = "";
                string PyText = "";
                int tagindex = 0;

                foreach (EntityBase ent in World.GetEntities())
                {
                    if (ent is SceneObjectGroup)
                    {
                        RexFramework.RexObjectProperties rexobj = m_rexObjects.GetObject(((SceneObjectGroup)ent).RootPart.UUID);
                        PythonClassName = "rxactor.Actor";
                        PythonTag = "";

                        // First check m_RexClassName, then description of object
                        if (rexobj.RexClassName.Length > 0)
                        {
                            tagindex = rexobj.RexClassName.IndexOf("?", 0);
                            if (tagindex > -1)
                            {
                                PythonClassName = rexobj.RexClassName.Substring(0, tagindex);
                                PythonTag = rexobj.RexClassName.Substring(tagindex + 1);
                            }
                            else
                                PythonClassName = rexobj.RexClassName;

                            ((SceneObjectGroup)ent).RootPart.SetScriptEvents(rexobj.ParentObjectID, (int)scriptEvents.touch_start);
                        }
                        //TODO: Get Py-classname and tag from prim description?
                        CreateActorToPython(ent.LocalId.ToString(), PythonClassName, PythonTag);
                    }
                }

                #region old code for checking class name from RexObjectPart. not in use, only as reference
                //List<EntityBase> EntityList = World.GetEntities();
                //foreach (EntityBase ent in EntityList)
                //{
                //    if (ent is RexObjects.RexObjectGroup)
                //    {
                //        PythonClassName = "rxactor.Actor";
                //        PythonTag = "";

                //        SceneObjectPart part = ((SceneObjectGroup)ent).GetChildPart(((SceneObjectGroup)ent).UUID);
                //        if (part is RexObjects.RexObjectPart)
                //        {
                //            RexObjects.RexObjectPart rexpart = (RexObjects.RexObjectPart)part;
                //            if (rexpart != null)
                //            {
                //                // First check m_RexClassName, then description of object
                //                if (rexpart.RexClassName.Length > 0)
                //                {
                //                    tagindex = rexpart.RexClassName.IndexOf("?", 0);
                //                    if (tagindex > -1)
                //                    {
                //                        PythonClassName = rexpart.RexClassName.Substring(0, tagindex);
                //                        PythonTag = rexpart.RexClassName.Substring(tagindex + 1);
                //                    }
                //                    else
                //                        PythonClassName = rexpart.RexClassName;
                //                }
                //                else if (rexpart.Description.Length > 9 && rexpart.Description.Substring(0, 4).ToLower() == "<py>")
                //                {
                //                    tagindex = rexpart.Description.IndexOf("</py>", 4);
                //                    if (tagindex > -1)
                //                        PyText = rexpart.Description.Substring(4, tagindex - 4);
                //                    else
                //                        continue;

                //                    tagindex = PyText.IndexOf("?", 0);
                //                    if (tagindex > -1)
                //                    {
                //                        PythonClassName = PyText.Substring(0, tagindex);
                //                        PythonTag = PyText.Substring(tagindex + 1);
                //                    }
                //                    else
                //                        PythonClassName = PyText;
                //                }
                //            }
                //        }
                //        CreateActorToPython(ent.LocalId.ToString(), PythonClassName, PythonTag);
                //    }
                //}
                #endregion

                // Create avatars
                string PParams = "";              
                List<ScenePresence> ScenePresencesList = World.GetScenePresences();
                foreach (ScenePresence avatar in ScenePresencesList)
                {
                    if (avatar.ControllingClient is IRexBot)
                        PParams = "\"add_bot\"," + avatar.LocalId.ToString() + "," + "\"" + avatar.UUID.ToString() + "\"";
                    else
                        PParams = "\"add_presence\"," + avatar.LocalId.ToString() + "," + "\"" + avatar.UUID.ToString() + "\"";

                    ExecutePythonStartCommand("CreateEventWithName(" + PParams + ")");
                }
             
                // start script thread
                m_EngineStarted = true;
                mPython.Execute("StartMainThread()");
            }
            catch (Exception e)
            {
                Log.WarnFormat("[RexScriptEngine]: Python init exception: " + e.ToString());
            }
        }


        public void RestartPythonEngine()
        {
            if (!m_PythonEnabled)
            {
                Log.InfoFormat("[RexScriptEngine]: Rex PythonScriptEngine disabled");
                return;
            }

            try
            {
                Log.InfoFormat("[RexScriptEngine]: Restart");
                // ShutDownPythonEngine();
                mPython.Execute("StopMainThread()");
                GC.Collect();
                GC.WaitForPendingFinalizers(); // tucofixme, blocking???
                StartPythonEngine();
            }
            catch (Exception e)
            {
                Log.WarnFormat("[RexScriptEngine]: restart exception: " + e.ToString());
            }

        }


        public void ExecutePythonCommand(string vCommand)
        {
            if (!m_EngineStarted)
                return;
            try
            {
                mPython.Execute(vCommand);
            }
            catch (Exception e)
            {
                Log.WarnFormat("[RexScriptEngine]: ExecutePythonCommand exception " + e.ToString());
            }
        }

        public void ExecutePythonStartCommand(string vCommand)
        {
            try
            {
                mPython.Execute(vCommand);
            }
            catch (Exception e)
            {
                Log.WarnFormat("[RexScriptEngine]: ExecutePythonStartCommand exception " + e.ToString());
            }
        }


        public object EvalutePythonCommand(string vCommand)
        {
            try
            {
                return mPython.Evaluate(vCommand);
            }
            catch (Exception e)
            {
                Log.WarnFormat("[RexScriptEngine]: ExecutePythonStartCommand exception " + e.ToString());
            }
            return null;
        }





        public void CreateActorToPython(string vLocalId, string vPythonClassName, string vPythonTag)
        {
            try
            {
                mPython.Execute("CreateActorOfClass(" + vLocalId + "," + vPythonClassName + ",\"" + vPythonTag + "\")");
            }
            catch (Exception)
            {
                try
                {
                    if (vPythonClassName.Length > 0)
                        Log.WarnFormat("[RexScriptEngine]: Could not load class:" + vPythonClassName);

                    mPython.Execute("CreateActorOfClass(" + vLocalId + ",rxactor.Actor,\"\")");
                }
                catch (Exception)
                {
                }
            }
        }

        public void Shutdown()
        {
            ShutDownPythonEngine();
            // We are shutting down
        }

        public void Close()
        {
            ShutDownPythonEngine();
            // We are shutting down
        }

        private void ShutDownPythonEngine()
        {
            if (mPython != null)
            {
                mPython.Execute("StopMainThread()");
                mPython.Shutdown();
                mPython.Dispose();
                mPython = null;
            }
        }

        public string Name
        {
            get { return "RexPythonScriptModule"; }
        }
    }
}
