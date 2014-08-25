namespace Cyclestreets.Objects
{
    public class ListBoxPair
    {
        private string DisplayName { get; set; }
        public string Value { get; set; }

        public ListBoxPair(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }
}