using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Not sure if using this over a raw Vector is worth it.
public class Coordinate
{
    public Vector2Int V;

    // geometric distance
    public float DistanceTo(Coordinate other)
    {
        return Vector2Int.Distance(V, other.V);
    }

    // distance along grid
    public float GridDistanceTo(Coordinate other)
    {
        return other.V.x - V.x + other.V.y - V.y;
    }

    
}
