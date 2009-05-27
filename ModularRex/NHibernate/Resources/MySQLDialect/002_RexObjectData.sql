ALTER TABLE RexMaterialsDictionaryItems add AssetURI varchar(512) null;

ALTER TABLE RexObjectProperties add RexMeshURI varchar(512) null;
ALTER TABLE RexObjectProperties add RexCollisionMeshURI varchar(512) null;
ALTER TABLE RexObjectProperties add RexParticleScriptURI varchar(512) null;
ALTER TABLE RexObjectProperties add RexAnimationPackageURI varchar(512) null;
ALTER TABLE RexObjectProperties add RexSoundURI varchar(512) null;