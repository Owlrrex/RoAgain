using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared
{
    public enum LocalizationLanguage
    {
        Unknown,
        English,
        German
    }

    public interface ILocalizedStringTable
    {
        public static ILocalizedStringTable Instance;

        public string GetStringById(LocalizedStringId id);

        public void SetClientLanguage(LocalizationLanguage newLanguage, bool forceReload = false);

        public void ReloadStrings();
    }

    public interface ILocalizedString
    {
        public string Resolve();

        public bool IsValid();

        public static bool IsValid(ILocalizedString locStr)
        {
            if (locStr == null)
                return false;

            return locStr.IsValid();
        }
    }

    /// <summary>
    /// Identifies a Localized String
    /// Only an Int at the moment, but may contain information about multiple string tables or similar in the future
    /// </summary>
    [Serializable]
    public struct LocalizedStringId : ILocalizedString
    {
        public static readonly LocalizedStringId INVALID = new(-1);

        public int Id;
        // Can add stuff like "string bank" here, if that's being added

        public LocalizedStringId(int id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return obj is LocalizedStringId other && Equals(other);
        }

        public bool Equals(LocalizedStringId other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(LocalizedStringId left, LocalizedStringId right) => left.Equals(right);
        public static bool operator !=(LocalizedStringId left, LocalizedStringId right) => !(left == right);

        public override string ToString()
        {
            return Id.ToString();
        }

        public static bool TryParse(string input, out LocalizedStringId outId)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                outId = INVALID;
                return false;
            }

            if (!int.TryParse(input, out outId.Id))
            {
                outId = INVALID;
                return false;
            }

            return true;
        }

        public static bool TryParse(ReadOnlySpan<char> input, out LocalizedStringId outId)
        {
            if (input.Length == 0)
            {
                outId = INVALID;
                return false;
            }

            if (!int.TryParse(input, out outId.Id))
            {
                outId = INVALID;
                return false;
            }

            return true;
        }

        public static bool TryParse(char[] input, out LocalizedStringId outId)
        {
            if (input.Length == 0)
            {
                outId = INVALID;
                return false;
            }

            if (!int.TryParse(input, out outId.Id))
            {
                outId = INVALID;
                return false;
            }

            return true;
        }

        public string Resolve()
        {
            return ILocalizedStringTable.Instance.GetStringById(this);
        }

        public bool IsValid()
        {
            return this != INVALID;
        }
    }

    public class CompositeLocalizedString : ILocalizedString
    {
        public ILocalizedString FormatString = LocalizedStringId.INVALID;
        public List<object> Arguments = new();

        public string Resolve()
        {
            List<string> argStrings = new();
            foreach (object argObj in Arguments)
            {
                if(argObj is ILocalizedString argLocStr)
                    argStrings.Add(argLocStr.Resolve());
                else
                    argStrings.Add(argObj.ToString());
            }
            return string.Format(FormatString.Resolve(), argStrings.ToArray());
        }

        public override bool Equals(object obj)
        {
            return obj is LocalizedStringId other && Equals(other);
        }

        public bool Equals(CompositeLocalizedString other)
        {
            return FormatString == other.FormatString
                && Arguments.SequenceEqual(other.Arguments);
        }

        public override int GetHashCode()
        {
            int hash = FormatString.GetHashCode();
            foreach (object arg in Arguments)
            {
                hash = HashCode.Combine(hash, arg.GetHashCode());
            }
            return hash;
        }

        public static bool operator ==(CompositeLocalizedString left, CompositeLocalizedString right) => left.Equals(right);
        public static bool operator !=(CompositeLocalizedString left, CompositeLocalizedString right) => !(left == right);

        public bool IsValid()
        {
            return FormatString.IsValid();
        }
    }
}
