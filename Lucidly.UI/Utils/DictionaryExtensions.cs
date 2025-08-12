namespace Lucidly.UI.Utils
{
    public static class DictionaryExtensions
    {
        public static Dictionary<string, string> Set(this Dictionary<string, string> dict, string key, string value)
        {
            dict[key] = value;
            return dict;
        }
    }
}
