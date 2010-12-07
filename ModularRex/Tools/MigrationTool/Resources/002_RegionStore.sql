BEGIN TRANSACTION;

CREATE TABLE regionban(
		regionUUID varchar (255),
		bannedUUID varchar (255),
		bannedIp varchar (255),
		bannedIpHostMask varchar (255)
		);
		
ALTER TABLE "prims" RENAME TO "__temp__prims";
CREATE TABLE "prims" ("UUID" varchar(255) PRIMARY KEY ,"RegionUUID" varchar(255),"CreationDate" integer,"Name" varchar(255),"SceneGroupID" varchar(255),"Text" varchar(255),"Description" varchar(255),"SitName" varchar(255),"TouchName" varchar(255),"ObjectFlags" integer,"CreatorID" varchar(255),"OwnerID" varchar(255),"GroupID" varchar(255),"LastOwnerID" varchar(255),"OwnerMask" integer,"NextOwnerMask" integer,"GroupMask" integer,"EveryoneMask" integer,"BaseMask" integer,"PositionX" float,"PositionY" float,"PositionZ" float,"GroupPositionX" float,"GroupPositionY" float,"GroupPositionZ" float,"VelocityX" float,"VelocityY" float,"VelocityZ" float,"AngularVelocityX" float,"AngularVelocityY" float,"AngularVelocityZ" float,"AccelerationX" float,"AccelerationY" float,"AccelerationZ" float,"RotationX" float,"RotationY" float,"RotationZ" float,"RotationW" float,"ClickAction" string,"SitTargetOffsetX" float,"SitTargetOffsetY" float,"SitTargetOffsetZ" float,"SitTargetOrientW" float,"SitTargetOrientX" float,"SitTargetOrientY" float,"SitTargetOrientZ" float);
INSERT INTO "prims" SELECT "UUID","RegionUUID","CreationDate","Name","SceneGroupID","Text","Description","SitName","TouchName","ObjectFlags","CreatorID","OwnerID","GroupID","LastOwnerID","OwnerMask","NextOwnerMask","GroupMask","EveryoneMask","BaseMask","PositionX","PositionY","PositionZ","GroupPositionX","GroupPositionY","GroupPositionZ","VelocityX","VelocityY","VelocityZ","AngularVelocityX","AngularVelocityY","AngularVelocityZ","AccelerationX","AccelerationY","AccelerationZ","RotationX","RotationY","RotationZ","RotationW","ClickAction","SitTargetOffsetX","SitTargetOffsetY","SitTargetOffsetZ","SitTargetOrientW","SitTargetOrientX","SitTargetOrientY","SitTargetOrientZ" FROM "__temp__prims";
DROP TABLE "__temp__prims";

update prims
  set UUID = substr(UUID, 1, 8) || "-" || substr(UUID, 9, 4) || "-" || substr(UUID, 13, 4) || "-" || substr(UUID, 17, 4) || "-" || substr(UUID, 21, 12) 
  where UUID not like '%-%';

update prims
  set RegionUUID = substr(RegionUUID, 1, 8) || "-" || substr(RegionUUID, 9, 4) || "-" || substr(RegionUUID, 13, 4) || "-" || substr(RegionUUID, 17, 4) || "-" || substr(RegionUUID, 21, 12) 
  where RegionUUID not like '%-%';

update prims
  set SceneGroupID = substr(SceneGroupID, 1, 8) || "-" || substr(SceneGroupID, 9, 4) || "-" || substr(SceneGroupID, 13, 4) || "-" || substr(SceneGroupID, 17, 4) || "-" || substr(SceneGroupID, 21, 12) 
  where SceneGroupID not like '%-%';

update prims
  set CreatorID = substr(CreatorID, 1, 8) || "-" || substr(CreatorID, 9, 4) || "-" || substr(CreatorID, 13, 4) || "-" || substr(CreatorID, 17, 4) || "-" || substr(CreatorID, 21, 12) 
  where CreatorID not like '%-%';

update prims
  set OwnerID = substr(OwnerID, 1, 8) || "-" || substr(OwnerID, 9, 4) || "-" || substr(OwnerID, 13, 4) || "-" || substr(OwnerID, 17, 4) || "-" || substr(OwnerID, 21, 12) 
  where OwnerID not like '%-%';

update prims
  set GroupID = substr(GroupID, 1, 8) || "-" || substr(GroupID, 9, 4) || "-" || substr(GroupID, 13, 4) || "-" || substr(GroupID, 17, 4) || "-" || substr(GroupID, 21, 12) 
  where GroupID not like '%-%';

update prims
  set LastOwnerID = substr(LastOwnerID, 1, 8) || "-" || substr(LastOwnerID, 9, 4) || "-" || substr(LastOwnerID, 13, 4) || "-" || substr(LastOwnerID, 17, 4) || "-" || substr(LastOwnerID, 21, 12) 
  where LastOwnerID not like '%-%';
       
COMMIT;