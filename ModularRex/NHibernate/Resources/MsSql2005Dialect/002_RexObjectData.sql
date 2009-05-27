ALTER TABLE RexMaterialsDictionaryItems ADD AssetURI NVARCHAR(512) null;

ALTER TABLE RexObjectProperties ADD RexMeshURI NVARCHAR(512) null;
ALTER TABLE RexObjectProperties ADD RexCollisionMeshURI NVARCHAR(512) null;
ALTER TABLE RexObjectProperties ADD RexParticleScriptURI NVARCHAR(512) null;
ALTER TABLE RexObjectProperties ADD RexAnimationPackageURI NVARCHAR(512) null;
ALTER TABLE RexObjectProperties ADD RexSoundURI NVARCHAR(512) null;