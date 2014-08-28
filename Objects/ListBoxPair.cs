using Cyclestreets.Annotations;

namespace Cyclestreets.Objects
{
    public class ListBoxPair
    {
// ReSharper disable once MemberCanBePrivate.Global
        public string DisplayName { [UsedImplicitly] get; set; }
        public string Value { get; private set; }

        public ListBoxPair(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }
}