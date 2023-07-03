using System.Linq;

namespace JFramework.Net
{
    public static class Extensions
    {
        public static int GetStableHashCode(this string text)
        {
            unchecked
            {
                return text.Aggregate(23, (current, @char) => current * 31 + @char);
            }
        }
    }
}