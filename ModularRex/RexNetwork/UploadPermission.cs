using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using log4net;
using System.Reflection;

namespace ModularRex.RexNetwork
{
    public class UploadPermission : IRegionModule, IUploadPermissions
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;
        private bool m_bypassPermissions = true;
        private bool m_disableFromAll = false;

        #region IRegionModule Members

        public void Close()
        {
        }

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scene = scene;
            if (source.Configs["realXtend"] != null)
            {
                m_bypassPermissions = !(source.Configs["realXtend"].GetBoolean("UploadPermissionsEnabled", false));
                m_disableFromAll = source.Configs["realXtend"].GetBoolean("DisableUploads", false);
            }

            m_scene.AddCommand(this, "uploadpermissions", "uploadpermissions true|false", "this enables or disables upload permissions", SetUploadPermissionsCommand);
            m_scene.AddCommand(this, "disableupload", "disableupload true|false", "this enables or disables upload", DisableUploadCommand);
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public string Name
        {
            get { return "UploadPermission"; }
        }

        public void PostInitialise()
        {
        }

        #endregion

        #region Console command handlers

        private void SetUploadPermissionsCommand(string module, string[] cmd)
        {
            if (cmd.Length >= 1 && cmd[0] == "uploadpermissions")
            {
                if (cmd.Length >= 2)
                {
                    try
                    {
                        m_bypassPermissions = !(Convert.ToBoolean(cmd[1]));
                    }
                    catch (Exception) {
                        m_log.Error("[UPLOADPERMISSIONS]: Error parseing command");
                    }
                }
                else
                {
                    m_log.InfoFormat("[UPLOADPERMISSIONS]: Upload permissions are {0}", m_bypassPermissions ? "disabled" : "enabled");
                }
            }
        }

        private void DisableUploadCommand(string module, string[] cmd)
        {
            if (cmd.Length >= 1 && cmd[0] == "disableupload")
            {
                if (cmd.Length >= 2)
                {
                    try
                    {
                        m_disableFromAll = (Convert.ToBoolean(cmd[1]));
                    }
                    catch (Exception)
                    {
                        m_log.Error("[UPLOADPERMISSIONS]: Error parseing command");
                    }
                }
                else
                {
                    m_log.InfoFormat("[UPLOADPERMISSIONS]: Uploads are {0}", m_disableFromAll ? "disabled" : "enabled");
                }
            }
        }

        #endregion

        #region IUploadPermissions Members

        public bool CanUpload(UUID agentId)
        {
            if (m_bypassPermissions)
            {
                return true;
            }
            else if (m_disableFromAll)
            {
                return false;
            }
            else
            {
                //TODO: Do the actual permission checking for the user
                
                return false;
            }
        }

        #endregion
    }


    public interface IUploadPermissions
    {
        bool CanUpload(UUID agentId);
    }
}
