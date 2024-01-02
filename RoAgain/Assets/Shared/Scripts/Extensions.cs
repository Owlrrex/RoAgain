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

    public static void DiffArrays<T, U>(T oldList, T newList, out HashSet<U> newElements, out HashSet<U> stayedElements, out HashSet<U> removedElements) where T : IEnumerable<U>
    {
        newElements = new(newList);
        newElements.ExceptWith(oldList);

        stayedElements = new(newList);
        stayedElements.IntersectWith(oldList);

        removedElements = new(oldList);
        removedElements.ExceptWith(newList);
    }

    public static string ToHotkeyString(this KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.None => string.Empty,
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
}
