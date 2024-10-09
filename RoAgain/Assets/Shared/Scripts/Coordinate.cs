using System;

[Serializable]
public struct Coordinate
{
    public static readonly Coordinate INVALID = new(int.MinValue, int.MinValue);

    public int X;
    public int Y;

    public Coordinate(int x, int y) { X = x; Y = y; }

    // geometric distance
    public float DistanceTo(Coordinate other)
    {
        float hori = X - other.X;
        float vert = Y - other.Y;
        return (float)Math.Sqrt(hori * hori + vert * vert);
    }

    // distance along vertical & horizontal steps
    public int GridDistanceTo(Coordinate other)
    {
        return Math.Abs(other.X - X) + Math.Abs(other.Y - Y);
    }

    // Max of X-diff & Y-diff - distance where each square-ring is distance 1 further away
    public int GridDistanceSquare(Coordinate other)
    {
        return Math.Max(Math.Abs(other.X - X), Math.Abs(other.Y - Y));
    }

    public static bool operator==(Coordinate self, Coordinate other)
    {
        return self.Equals(other);
    }

    public static bool operator !=(Coordinate self, Coordinate other)
    {
        return !(self == other);
    }

    public override readonly string ToString()
    {
        return $"{X}/{Y}";
    }

    public override bool Equals(object obj)
    {
        return obj is Coordinate coordinate &&
               X == coordinate.X &&
               Y == coordinate.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}

[Serializable]
public struct MapCoordinate
{
    public static readonly MapCoordinate INVALID = new() { MapId = string.Empty, Coord = Coordinate.INVALID };

    public Coordinate Coord;
    public string MapId;

    public static bool operator ==(MapCoordinate self, MapCoordinate other)
    {
        return self.Equals(other);
    }

    public static bool operator !=(MapCoordinate self, MapCoordinate other)
    {
        return !(self == other);
    }

    public override readonly string ToString()
    {
        return $"{MapId}/{Coord}";
    }

    public override bool Equals(object obj)
    {
        return obj is MapCoordinate coordinate &&
               Coord.Equals(coordinate.Coord) &&
               MapId == coordinate.MapId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Coord, MapId);
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
        coord.MapId = parts[0];

        return coord;
    }
}