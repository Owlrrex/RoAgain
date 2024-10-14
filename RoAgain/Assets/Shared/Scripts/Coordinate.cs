using System;

namespace Shared
{
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

        public static bool operator ==(Coordinate self, Coordinate other)
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

        public GridData.Direction GetDirectionTo(Coordinate to)
        {
            (int X, int Y) diffs = (to.X - X, to.Y - Y);

            // Fast-Track coordinate offsets that are commonly used while updating directions during movement
            return diffs switch
            {
                (0, 0) => GridData.Direction.Unknown,
                (0, 1) => GridData.Direction.North,
                (0, -1) => GridData.Direction.South,
                (1, 0) => GridData.Direction.East,
                (-1, 0) => GridData.Direction.West,
                (1, 1) => GridData.Direction.NorthEast,
                (1, -1) => GridData.Direction.SouthEast,
                (-1, 1) => GridData.Direction.NorthWest,
                (-1, -1) => GridData.Direction.SouthWest,
                _ => CalculateDirection(diffs)
            };
        }

        // Doesn't divide the circle absolutely equally, since the sine-values for those angles are a mess
        // Instead, divides along the lines of y = 2x and y = x/2 etc.
        private static GridData.Direction CalculateDirection((int X, int Y) diffs)
        {
            if (diffs.Y == 0)
            {
                if (diffs.X > 0)
                    return GridData.Direction.East;
                else
                    return GridData.Direction.West;
            }

            float ratio = Math.Abs(diffs.X) / Math.Abs(diffs.Y);

            // Closest to horizontal = 1, Closest to diagonal = 2, Clostest to vertical = 3
            int sector = 2;
            if (ratio < 0.5f)
                sector = 1;
            else if (ratio > 2)
                sector = 3;

            // Topright = 1, Topleft = 2, Bottomleft = 3, Bottomright = 4
            int quadrant = diffs.X > 0 ? (diffs.Y > 0 ? 1 : 4) : (diffs.Y > 0 ? 2 : 3);

            return (quadrant, sector) switch
            {
                (1, 1) => GridData.Direction.East,
                (1, 2) => GridData.Direction.NorthEast,
                (1, 3) => GridData.Direction.North,
                (2, 1) => GridData.Direction.West,
                (2, 2) => GridData.Direction.NorthWest,
                (2, 3) => GridData.Direction.North,
                (3, 1) => GridData.Direction.West,
                (3, 2) => GridData.Direction.SouthWest,
                (3, 3) => GridData.Direction.South,
                (4, 1) => GridData.Direction.East,
                (4, 2) => GridData.Direction.SouthEast,
                (4, 3) => GridData.Direction.South,
                _ => GridData.Direction.Unknown
            };
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
}
