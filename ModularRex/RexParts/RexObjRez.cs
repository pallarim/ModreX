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
using System.Timers;

namespace ModularRex.RexParts
{
    public class RexObjectRezModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;
        private ModrexObjects m_rexObjects;

        /// <value>
        /// Is the deleter currently enabled?
        /// </value>
        public bool DeleterEnabled;

        private Timer m_inventoryTicker = new Timer(2000);
        private readonly Queue<DeleteToInventoryHolder> m_inventoryDeletes = new Queue<DeleteToInventoryHolder>();

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
            m_inventoryTicker.AutoReset = false;
            m_inventoryTicker.Elapsed += InventoryRunDeleteTimer;

            m_scene.EventManager.OnNewClient += HandleNewClient;

            OpenSim.Region.Framework.Interfaces.IRegionModule module = m_scene.Modules["RexObjectsModule"];
            if (module != null && module is ModrexObjects)
            {
                m_rexObjects = (ModrexObjects)module;
            }
            DeleterEnabled = true;
        }

        #endregion

        #region Event Handlers

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
                avatar.ControllingClient.OnDeRezObject += DeRezObj;
            }
        }

        private void DeRezObj(IClientAPI remoteClient, List<uint> localIDs, UUID groupID, DeRezAction action, UUID destinationID)
        {
            foreach (uint id in localIDs)
            {
                ClientDeRezObject(remoteClient, id, groupID, action, destinationID);
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

                DeleteToInventory(action, destinationID, grp, remoteClient, permissionToDelete);

                ////TODO: Change this to some kind of async queue
                ////The queue was orginally introduced in OpenSim revision 5346:
                //// * Moves sending items to inventory via a delete into a seperate thread (this thread
                ////   can be expanded to support all sends to inventory from inworld easily enough). 
                ////   Thread is temporary and only exists while items are being returned.
                //// * This should remove the "lag" caused by deleting many objects.
                //// * Patch brought to you by Joshua Nightshade's bitching at me to fix it.
                //UUID assetId = m_scene.DeleteToInventory(action, destinationID, grp, remoteClient);
 
                ////Get the item id of the asset so the RexObjectProperties can be changed to that id
                //CachedUserInfo userInfo = m_scene.CommsManager.UserProfileCacheService.GetUserDetails(remoteClient.AgentId);
                //InventoryItemBase item = userInfo.RootFolder.FindAsset(assetId);

                ////Clone the old properties 
                //if (m_rexObjects != null)
                //{
                //    RexObjectProperties origprops = m_rexObjects.GetObject(part.UUID);
                //    RexObjectProperties cloneprops = m_rexObjects.GetObject(item.ID);

                //    cloneprops.SetRexPrimDataFromObject(origprops);
                //}

                //if (permissionToDelete)
                //{
                //    grp.DeleteGroup(false);
                //}

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

        #region AsyncSceneObjectGroupDeleter

        /// <summary>
        /// Delete the given object from the scene
        /// </summary>
        public void DeleteToInventory(DeRezAction action, UUID folderID,
                SceneObjectGroup objectGroup, IClientAPI remoteClient,
                bool permissionToDelete)
        {
            if (DeleterEnabled)
                m_inventoryTicker.Stop();

            lock (m_inventoryDeletes)
            {
                DeleteToInventoryHolder dtis = new DeleteToInventoryHolder();
                dtis.action = action;
                dtis.folderID = folderID;
                dtis.objectGroup = objectGroup;
                dtis.remoteClient = remoteClient;
                dtis.permissionToDelete = permissionToDelete;

                m_inventoryDeletes.Enqueue(dtis);
            }

            if (DeleterEnabled)
                m_inventoryTicker.Start();

            // Visually remove it, even if it isnt really gone yet.  This means that if we crash before the object
            // has gone to inventory, it will reappear in the region again on restart instead of being lost.
            // This is not ideal since the object will still be available for manipulation when it should be, but it's
            // better than losing the object for now.
            if (permissionToDelete)
                objectGroup.DeleteGroup(false);
        }

        private void InventoryRunDeleteTimer(object sender, ElapsedEventArgs e)
        {
            m_log.Debug("[SCENE]: Starting send to inventory loop");

            while (InventoryDeQueueAndDelete())
            {
                m_log.Debug("[SCENE]: Sent item successfully to inventory, continuing...");
            }
        }

        /// <summary>
        /// Move the next object in the queue to inventory.  Then delete it properly from the scene.
        /// </summary>
        /// <returns></returns>
        public bool InventoryDeQueueAndDelete()
        {
            DeleteToInventoryHolder x = null;

            try
            {
                lock (m_inventoryDeletes)
                {
                    int left = m_inventoryDeletes.Count;
                    if (left > 0)
                    {
                        m_log.DebugFormat(
                            "[SCENE]: Sending object to user's inventory, {0} item(s) remaining.", left);

                        x = m_inventoryDeletes.Dequeue();

                        try
                        {
                            //m_scene.DeleteToInventory(x.action, x.folderID, x.objectGroup, x.remoteClient);
                            //if (x.permissionToDelete)
                            //    m_scene.DeleteSceneObject(x.objectGroup, false);

                            UUID assetId = m_scene.DeleteToInventory(x.action, x.folderID, x.objectGroup, x.remoteClient);

                            //Get the item id of the asset so the RexObjectProperties can be changed to that id
                            CachedUserInfo userInfo = m_scene.CommsManager.UserProfileCacheService.GetUserDetails(x.remoteClient.AgentId);
                            if (userInfo.RootFolder != null)
                            {
                                InventoryItemBase item = userInfo.RootFolder.FindAsset(assetId);

                                //Clone the old properties 
                                if (m_rexObjects != null)
                                {
                                    RexObjectProperties origprops = m_rexObjects.GetObject(x.objectGroup.RootPart.UUID);
                                    RexObjectProperties cloneprops = m_rexObjects.GetObject(item.ID);

                                    cloneprops.SetRexPrimDataFromObject(origprops);
                                }
                            }
                            else
                                m_log.Warn("[REXOBJECTS]: Could not find users root folder from cache. Did not clone rex object");
                            if (x.permissionToDelete)
                            {
                                m_scene.DeleteSceneObject(x.objectGroup, false);
                                //m_rexObjects.DeleteObject(x.objectGroup.RootPart.UUID);
                            }
                        }
                        catch (Exception e)
                        {
                            m_log.DebugFormat("Exception background sending object: " + e);
                        }

                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                // We can't put the object group details in here since the root part may have disappeared (which is where these sit).
                // FIXME: This needs to be fixed.
                m_log.ErrorFormat(
                    "[SCENE]: Queued sending of scene object to agent {0} {1} failed: {2}",
                    (x != null ? x.remoteClient.Name : "unavailable"), (x != null ? x.remoteClient.AgentId.ToString() : "unavailable"), e.ToString());
            }

            m_log.Debug("[SCENE]: No objects left in inventory send queue.");
            return false;
        }

        #endregion
    }

    class DeleteToInventoryHolder
    {
        public DeRezAction action;
        public IClientAPI remoteClient;
        public SceneObjectGroup objectGroup;
        public UUID folderID;
        public bool permissionToDelete;
    }
}
