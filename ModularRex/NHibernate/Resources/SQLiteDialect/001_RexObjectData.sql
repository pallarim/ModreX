CREATE TABLE `RexMaterialsDictionaryItems` (
    `ID` INTEGER PRIMARY KEY ASC NOT NULL,
    `AssetID` VARCHAR(50) NULL,
        `Num` INTEGER NULL,
        `RexObjectUUID` VARCHAR(50) NULL );


CREATE TABLE `RexObjectProperties` (
    `ID` VARCHAR(50) PRIMARY KEY ASC NOT NULL,
    `RexDrawType` VARCHAR(50) NOT NULL,
    `RexIsVisible` INTEGER NULL,
    `RexCastShadows` INTEGER NULL,
    `RexLightCreatesShadows` INTEGER NULL,
    `RexDescriptionTexture` INTEGER NULL,
    `RexScaleToPrim` INTEGER NULL,
    `RexDrawDistance` FLOAT NULL,
    `RexLOD` FLOAT NULL,
    `RexMeshUUID` VARCHAR(50) NULL,
    `RexCollisionMeshUUID` VARCHAR(50) NULL,
    `RexParticleScriptUUID` VARCHAR(50) NULL,
    `RexAnimationPackageUUID` VARCHAR(50) NULL,
    `RexAnimationName` VARCHAR(64) NULL,
    `RexAnimationRate` FLOAT NULL,
    `RexClassName` VARCHAR(64) NULL,
    `RexSoundUUID` VARCHAR(50) NULL,
    `RexSoundVolume` FLOAT NULL,
    `RexSoundRadius` FLOAT NULL,
    `RexData` VARCHAR(3000) NULL,
    `RexSelectPriority` INTEGER NULL
);
