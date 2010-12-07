create table ECData (
   EntityID  VARCHAR(37),
   ComponentType VARCHAR(64) not null,
   ComponentName VARCHAR(64) not null,
   Data BLOB,
   DataIsString INTEGER,
   primary key (EntityID, ComponentType, ComponentName)
)