BEGIN;

ALTER TABLE RexMaterialsDictionaryItems ADD COLUMN AssetURI varchar(512) null;

ALTER TABLE RexObjectProperties ADD COLUMN RexMeshURI varchar(512) null;
ALTER TABLE RexObjectProperties ADD COLUMN RexCollisionMeshURI varchar(512) null;
ALTER TABLE RexObjectProperties ADD COLUMN RexParticleScriptURI varchar(512) null;
ALTER TABLE RexObjectProperties ADD COLUMN RexAnimationPackageURI varchar(512) null;
ALTER TABLE RexObjectProperties ADD COLUMN RexSoundURI varchar(512) null;

COMMIT;
