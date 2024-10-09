using Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class Extensions
{
    public static Rect ToRect(this Vector4 vector)
    {
        return new Rect(vector.x, vector.y, vector.z, vector.w);
    }

    public static string SerializeReflection(this object o)
    {
        StringBuilder fullString = new();
        Type type = o.GetType();
        string classname = type.Name;
        fullString.Append($"{{{classname}(");

        MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
        string memberDelimiter = ", ";
        bool isFirst = true;
        foreach (MemberInfo mi in memberInfos)
        {
            if (mi.MemberType != MemberTypes.Field)
                continue;

            if (isFirst)
                isFirst = false;
            else
                fullString.Append(memberDelimiter);

            string memberName = mi.Name;
            object memberValue = type.InvokeMember(mi.Name, BindingFlags.GetField, null, o, new object[] { });
            string memberString = $"{memberName}={memberValue}";
            fullString.Append(memberString);
        }
        fullString.Append(")}");

        return fullString.ToString();
    }

    public static int GridDistanceSquare(this Vector2Int self, Vector2Int other)
    {
        return Mathf.Max(Mathf.Abs(other.x - self.x), Mathf.Abs(other.y - self.y));
    }

    public static Vector2Int ToVector(this GridData.Direction direction)
    {
        return direction switch
        {
            GridData.Direction.North => new Vector2Int(0, 1),
            GridData.Direction.NorthEast => new Vector2Int(1, 1),
            GridData.Direction.East => new Vector2Int(1, 0),
            GridData.Direction.SouthEast => new Vector2Int(1, -1),
            GridData.Direction.South => new Vector2Int(0, -1),
            GridData.Direction.SouthWest => new Vector2Int(-1, -1),
            GridData.Direction.West => new Vector2Int(-1, 0),
            GridData.Direction.NorthWest => new Vector2Int(-1, 1),
            _ => Vector2Int.zero,
        };
    }

    public static Coordinate ToCoordinateOffset(this GridData.Direction direction)
    {
        return direction switch
        {
            GridData.Direction.North => new(0, 1),
            GridData.Direction.NorthEast => new(1, 1),
            GridData.Direction.East => new(1, 0),
            GridData.Direction.SouthEast => new(1, -1),
            GridData.Direction.South => new(0, -1),
            GridData.Direction.SouthWest => new(-1, -1),
            GridData.Direction.West => new(-1, 0),
            GridData.Direction.NorthWest => new(-1, 1),
            _ => Coordinate.INVALID,
        };
    }

    public static GridData.Direction GetDirectionTo(this Coordinate from, Coordinate to)
    {
        (int X, int Y) diffs = (to.X - from.X, to.Y - from.Y);

        // Fast-Track coordinate offsets that are commonly used while updating directions during movement
        return diffs switch
        {
            (0, 0) => GridData.Direction.Unknown,
            (0, 1) => GridData.Direction.East,
            (0, -1) => GridData.Direction.West,
            (1, 0) => GridData.Direction.North,
            (-1, 0) => GridData.Direction.South,
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

    public static void DiffArrays<T, U>(T oldList, T newList, ref HashSet<U> newElements, ref HashSet<U> stayedElements, ref HashSet<U> removedElements) where T : IEnumerable<U>
    {
        if(newElements != null)
        {
            newElements.Clear();
            newElements.UnionWith(newList);
            newElements.ExceptWith(oldList);
        }
        
        if(stayedElements != null)
        {
            stayedElements.Clear();
            stayedElements.UnionWith(newList);
            stayedElements.IntersectWith(oldList);
        }
        
        if(removedElements != null)
        {
            removedElements.Clear();
            removedElements.UnionWith(oldList);
            removedElements.ExceptWith(newList);
        }
    }

    public static string ToHotkeyString(this KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.None => "...",
            KeyCode.Alpha1 => "1",
            KeyCode.Alpha2 => "2",
            KeyCode.Alpha3 => "3",
            KeyCode.Alpha4 => "4",
            KeyCode.Alpha5 => "5",
            KeyCode.Alpha6 => "6",
            KeyCode.Alpha7 => "7",
            KeyCode.Alpha8 => "8",
            KeyCode.Alpha9 => "9",
            KeyCode.Alpha0 => "0",
            KeyCode.Keypad0 => "0",
            KeyCode.Keypad1 => "1",
            KeyCode.Keypad2 => "2",
            KeyCode.Keypad3 => "3",
            KeyCode.Keypad4 => "4",
            KeyCode.Keypad5 => "5",
            KeyCode.Keypad6 => "6",
            KeyCode.Keypad7 => "7",
            KeyCode.Keypad8 => "8",
            KeyCode.Keypad9 => "9",
            _ => keyCode.ToString()
        };
    }

    public static bool IsSameHotkey(this KeyCode self, KeyCode other)
    {
        // TODO: which keys should be treated the same? numpad & alpha numbers?
        return self == other;
    }

    public static bool HasLayer(this LayerMask mask, int layerIndex)
    {
        return (mask.value & (1 << layerIndex)) > 0;
    }

    public static bool HasLayer(this LayerMask mask, string layerName)
    {
        return (mask.value & (1 << LayerMask.NameToLayer(layerName))) > 0;
    }
}
