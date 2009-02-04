CREATE TABLE [dbo].[RexAssetData](
	[ID] [nvarchar](255) NOT NULL,
	[MediaURL] [nvarchar](64) NULL,
	[RefreshRate] [tinyint] NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]