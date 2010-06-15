using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace ModularRex.RexFramework
{
    public interface IRexObjectPropertiesEventManager
    {
        void TriggerOnChangePythonClass(UUID id);
        void TriggerOnChangeCollisionMesh(UUID id);
        void TriggerOnChangeScaleToPrim(UUID id);
        void TriggerOnChangeRexObjectProperties(UUID id);
        void TriggerOnChangeRexObjectMetaData(UUID id);
        
        sbyte GetAssetType(UUID assetid);

        void TriggerOnSaveObject(UUID id);
    }
}
