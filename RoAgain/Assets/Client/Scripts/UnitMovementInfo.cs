using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMovementInfo
{
    public int UnitId;
    public GridData.Path Path;

    public UnitMovementInfo(EntityPathUpdatePacket packet)
    {
        UnitId = packet.UnitId;
        Path = packet.Path;
    }
}
