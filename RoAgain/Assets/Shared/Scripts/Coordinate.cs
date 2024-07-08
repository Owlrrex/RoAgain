using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Not sure if using this over a raw Vector is worth it.
public struct Coordinate
{
    public static readonly Coordinate INVALID = new() { X = -1, Y = -1 };

    public int X;
    public int Y;

    // geometric distance
    public float DistanceTo(Coordinate other)
    {
        float hori = X - other.X;
        float vert = Y - other.Y;
        return (float)Math.Sqrt(hori * hori + vert * vert);
    }

    // distance along grid
    public float GridDistanceTo(Coordinate other)
    {
        return Math.Abs(other.X - X) + Math.Abs(other.Y - Y);
    }

    public readonly Vector2Int ToVector()
    {
        return new Vector2Int(X, Y);
    }

    public override readonly string ToString()
    {
        return $"{X}/{Y}";
    }
}

public struct MapCoordinate
{
    public static readonly MapCoordinate INVALID = new() { MapId = string.Empty, Coord = Coordinate.INVALID };

    public Coordinate Coord;
    public string MapId;

    public override readonly string ToString()
    {
        return $"{MapId}/{Coord}";
    }
}

public static class CoordinateExtensions
{
    public static Coordinate ToCoordinate(this string s)
    {
        string[] parts = s.Split("/");
        if (parts.Length != 2)
            return Coordinate.INVALID;

        Coordinate coord = new();
        if (!int.TryParse(parts[0], out coord.X)
            || !int.TryParse(parts[1], out coord.Y))
        {
            return Coordinate.INVALID;
        }

        return coord;
    }

    public static Coordinate ToCoordinate(this Vector2Int v)
    {
        return new() { X = v.x, Y = v.y };
    }

    public static MapCoordinate ToMapCoordinate(this string s)
    {
        string[] parts = s.Split("/");
        if (parts.Length != 3)
            return MapCoordinate.INVALID;

        MapCoordinate coord = new();
        if (!int.TryParse(parts[1], out coord.Coord.X)
            || !int.TryParse(parts[2], out coord.Coord.Y))
        {
            return MapCoordinate.INVALID;
        }

        return coord;
    }
}