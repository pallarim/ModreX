CREATE TABLE [RexMaterialsDictionaryItems](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[AssetID] [varchar](50) NULL,
	[Num] [int] NULL,
	[RexObjectUUID] [varchar](50) NULL,
 CONSTRAINT [PK_RexMaterialsDictionaryItems] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

CREATE TABLE [RexObjectProperties](
	[ID] [varchar](50) NOT NULL,
	[RexDrawType] [tinyint] NULL,
	[RexIsVisible] [int] NULL,
	[RexCastShadows] [int] NULL,
	[RexLightCreatesShadows] [int] NULL,
	[RexDescriptionTexture] [int] NULL,
	[RexScaleToPrim] [int] NULL,
	[RexDrawDistance] [float] NULL,
	[RexLOD] [float] NULL,
	[RexMeshUUID] [varchar](50) NULL,
	[RexCollisionMeshUUID] [varchar](50) NULL,
	[RexParticleScriptUUID] [varchar](50) NULL,
	[RexAnimationPackageUUID] [varchar](50) NULL,
	[RexAnimationName] [varchar](64) NULL,
	[RexAnimationRate] [float] NULL,
	[RexClassName] [varchar](64) NULL,
	[RexSoundUUID] [varchar](50) NULL,
	[RexSoundVolume] [float] NULL,
	[RexSoundRadius] [float] NULL,
	[RexData] [varchar](3000) NULL,
	[RexSelectPriority] [int] NULL,
 CONSTRAINT [PK_RexObjectProperties] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
