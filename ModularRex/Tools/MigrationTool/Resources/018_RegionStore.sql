BEGIN;

update terrain 
  set RegionUUID = substr(RegionUUID, 1, 8) || "-" || substr(RegionUUID, 9, 4) || "-" || substr(RegionUUID, 13, 4) || "-" || substr(RegionUUID, 17, 4) || "-" || substr(RegionUUID, 21, 12)
  where RegionUUID not like '%-%';
  

update landaccesslist
  set LandUUID = substr(LandUUID, 1, 8) || "-" || substr(LandUUID, 9, 4) || "-" || substr(LandUUID, 13, 4) || "-" || substr(LandUUID, 17, 4) || "-" || substr(LandUUID, 21, 12) 
  where LandUUID not like '%-%';

update landaccesslist
  set AccessUUID = substr(AccessUUID, 1, 8) || "-" || substr(AccessUUID, 9, 4) || "-" || substr(AccessUUID, 13, 4) || "-" || substr(AccessUUID, 17, 4) || "-" || substr(AccessUUID, 21, 12) 
  where AccessUUID not like '%-%';


update primshapes
  set UUID = substr(UUID, 1, 8) || "-" || substr(UUID, 9, 4) || "-" || substr(UUID, 13, 4) || "-" || substr(UUID, 17, 4) || "-" || substr(UUID, 21, 12) 
  where UUID not like '%-%';


update land
  set UUID = substr(UUID, 1, 8) || "-" || substr(UUID, 9, 4) || "-" || substr(UUID, 13, 4) || "-" || substr(UUID, 17, 4) || "-" || substr(UUID, 21, 12) 
  where UUID not like '%-%';
  
update land
  set RegionUUID = substr(RegionUUID, 1, 8) || "-" || substr(RegionUUID, 9, 4) || "-" || substr(RegionUUID, 13, 4) || "-" || substr(RegionUUID, 17, 4) || "-" || substr(RegionUUID, 21, 12)
  where RegionUUID not like '%-%';

update land
  set OwnerUUID = substr(OwnerUUID, 1, 8) || "-" || substr(OwnerUUID, 9, 4) || "-" || substr(OwnerUUID, 13, 4) || "-" || substr(OwnerUUID, 17, 4) || "-" || substr(OwnerUUID, 21, 12)
  where OwnerUUID not like '%-%';

update land
  set GroupUUID = substr(GroupUUID, 1, 8) || "-" || substr(GroupUUID, 9, 4) || "-" || substr(GroupUUID, 13, 4) || "-" || substr(GroupUUID, 17, 4) || "-" || substr(GroupUUID, 21, 12)
  where GroupUUID not like '%-%';

update land
  set MediaTextureUUID = substr(MediaTextureUUID, 1, 8) || "-" || substr(MediaTextureUUID, 9, 4) || "-" || substr(MediaTextureUUID, 13, 4) || "-" || substr(MediaTextureUUID, 17, 4) || "-" || substr(MediaTextureUUID, 21, 12)
  where MediaTextureUUID not like '%-%';

update land
  set SnapshotUUID = substr(SnapshotUUID, 1, 8) || "-" || substr(SnapshotUUID, 9, 4) || "-" || substr(SnapshotUUID, 13, 4) || "-" || substr(SnapshotUUID, 17, 4) || "-" || substr(SnapshotUUID, 21, 12)
  where SnapshotUUID not like '%-%';

update land
  set AuthbuyerID = substr(AuthbuyerID, 1, 8) || "-" || substr(AuthbuyerID, 9, 4) || "-" || substr(AuthbuyerID, 13, 4) || "-" || substr(AuthbuyerID, 17, 4) || "-" || substr(AuthbuyerID, 21, 12)
  where AuthbuyerID not like '%-%';
  
COMMIT;