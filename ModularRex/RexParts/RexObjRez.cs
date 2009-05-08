using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;
using OpenMetaverse;
using log4net;
using System.Reflection;
using OpenSim.Framework.Communications.Cache;
using ModularRex.RexFramework;

namespace ModularRex.RexParts
{
    public class RexObjectRezModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;
        private ModrexObjects m_rexObjects;

        #region IRegionModule Members

        public void Close()
        {
        }

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scene = scene;
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public string Name
        {
            get { return "RexObjectRezModule"; }
        }

        public void PostInitialise()
        {
            m_scene.EventManager.OnNewClient += HandleNewClient;

            OpenSim.Region.Framework.Interfaces.IRegionModule module = m_scene.Modules["RexObjectsModule"];
            if (module != null && module is ModrexObjects)
            {
                m_rexObjects = (ModrexObjects)module;
            }
        }

        private void HandleNewClient(IClientAPI client)
        {
            //Do these to all clients
            //Other clients might accidentially take object with rex properties
            //They also might rez in back again

            ScenePresence avatar = m_scene.GetScenePresence(client.AgentId);
            if (avatar != null)
            {
                avatar.ControllingClient.OnRezObject -= avatar.Scene.RezObject;
                avatar.ControllingClient.OnDeRezObject -= avatar.Scene.DeRezObject;
                avatar.ControllingClient.OnRezObject += ClientRezObject;
                avatar.ControllingClient.OnDeRezObject += ClientDeRezObject;
            }
        }

        private void ClientDeRezObject(IClientAPI remoteClient, uint localID, UUID groupID, DeRezAction action, UUID destinationID)
        {
            SceneObjectPart part = m_scene.GetSceneObjectPart(localID);
            if (part == null)
                return;

            if (part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            // Can't delete child prims
            if (part != part.ParentGroup.RootPart)
                return;

            SceneObjectGroup grp = part.ParentGroup;

            //force a database backup/update on this SceneObjectGroup
            //So that we know the database is upto date, for when deleting the object from it
            m_scene.ForceSceneObjectBackup(grp);

            bool permissionToTake = false;
            bool permissionToDelete = false;

            if (action == DeRezAction.SaveToExistingUserInventoryItem)
            {
                if (grp.OwnerID == remoteClient.AgentId && grp.RootPart.FromUserInventoryItemID != UUID.Zero)
                {
                    permissionToTake = true;
                    permissionToDelete = false;
                }
            }
            else if (action == DeRezAction.TakeCopy)
            {
                permissionToTake =
                        m_scene.Permissions.CanTakeCopyObject(
                        grp.UUID,
                        remoteClient.AgentId);
            }
            else if (action == DeRezAction.GodTakeCopy)
            {
                permissionToTake =
                        m_scene.Permissions.IsGod(
                        remoteClient.AgentId);
            }
            else if (action == DeRezAction.Take)
            {
                permissionToTake =
                        m_scene.Permissions.CanTakeObject(
                        grp.UUID,
                        remoteClient.AgentId);

                //If they can take, they can delete!
                permissionToDelete = permissionToTake;
            }
            else if (action == DeRezAction.Delete)
            {
                permissionToTake =
                        m_scene.Permissions.CanDeleteObject(
                        grp.UUID,
                        remoteClient.AgentId);
                permissionToDelete = permissionToTake;
            }
            else if (action == DeRezAction.Return)
            {
                if (remoteClient != null)
                {
                    permissionToTake =
                            m_scene.Permissions.CanReturnObject(
                            grp.UUID,
                            remoteClient.AgentId);
                    permissionToDelete = permissionToTake;

                    if (permissionToDelete)
                    {
                        m_scene.AddReturn(grp.OwnerID, grp.Name, grp.AbsolutePosition, "parcel owner return");
                    }
                }
                else // Auto return passes through here with null agent
                {
                    permissionToTake = true;
                    permissionToDelete = true;
                }
            }
            else
            {
                m_log.DebugFormat(
                    "[AGENT INVENTORY]: Ignoring unexpected derez action {0} for {1}", action, remoteClient.Name);
                return;
            }

            if (permissionToTake)
            {
                //TODO: Change this to some kind of async queue
                //The queue was orginally introduced in OpenSim revision 5346:
                // * Moves sending items to inventory via a delete into a seperate thread (this thread
                //   can be expanded to support all sends to inventory from inworld easily enough). 
                //   Thread is temporary and only exists while items are being returned.
                // * This should remove the "lag" caused by deleting many objects.
                // * Patch brought to you by Joshua Nightshade's bitching at me to fix it.
                UUID assetId = m_scene.DeleteToInventory(action, destinationID, grp, remoteClient);
 
                //Get the item id of the asset so the RexObjectProperties can be changed to that id
                CachedUserInfo userInfo = m_scene.CommsManager.UserProfileCacheService.GetUserDetails(remoteClient.AgentId);
                InventoryItemBase item = userInfo.RootFolder.FindAsset(assetId);

                //Clone the old properties 
                if (m_rexObjects != null)
                {
                    RexObjectProperties origprops = m_rexObjects.GetObject(part.UUID);
                    RexObjectProperties cloneprops = m_rexObjects.GetObject(item.ID);

                    cloneprops.SetRexPrimDataFromObject(origprops);
                }

                if (permissionToDelete)
                {
                    grp.DeleteGroup(false);
                    m_rexObjects.DeleteObject(part.UUID);
                }

            }
            else if (permissionToDelete)
            {
                m_scene.DeleteSceneObject(grp, false);
            }
        }

        private void ClientRezObject(IClientAPI remoteClient, UUID itemID, Vector3 RayEnd, Vector3 RayStart, UUID RayTargetID,
            byte BypassRayCast, bool RayEndIsIntersection, bool RezSelected, bool RemoveItem, UUID fromTaskID)
        {
            SceneObjectGroup group = m_scene.RezObject(remoteClient, itemID, RayEnd, RayStart, RayTargetID,
                BypassRayCast, RayEndIsIntersection, RezSelected, RemoveItem, fromTaskID, false);

            RexObjectProperties robj = m_rexObjects.GetObject(group.RootPart.FromUserInventoryItemID);
            RexObjectProperties newprops = m_rexObjects.GetObject(group.RootPart.UUID);
            newprops.SetRexPrimDataFromObject(robj);
        }

        #endregion
    }
}
