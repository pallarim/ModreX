create table AssetFolder (
  Id  integer,
   Discriminator TEXT not null,
   ParentPath TEXT,
   Name TEXT,
   AssetID TEXT,
   primary key (Id)
)