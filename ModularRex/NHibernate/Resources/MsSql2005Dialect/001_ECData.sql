CREATE TABLE [dbo].[ECData] (
   [EntityID]  [nvarchar](37),
   [ComponentType] [nvarchar](64) not null,
   [ComponentName] [nvarchar](64) not null,
   [Data] [image],
   [DataIsString] [int],
   primary key ([EntityID], [ComponentType], [ComponentName])
)