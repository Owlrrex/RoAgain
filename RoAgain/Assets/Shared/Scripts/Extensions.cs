using Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine; // TODO: split into client-extensions (that can use UnityEngine) and others that aren't allowed to use it

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

    // Check for "Is Power of 2"
    // https://stackoverflow.com/questions/600293/how-to-check-if-a-number-is-a-power-of-2
    public static bool IsPowerOfTwo(this int n)
    {
        return n != 0 && ((n & (n - 1)) == 0);
    }
}
