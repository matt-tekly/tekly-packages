namespace Tekly.Backtick
{
    public static class StringExtensions
    {
        public static string Color(this string str, string color)
        {
            return $"<color={color}>{str}</color>";
        }

        public static string Gray(this string str)
        {
            return str.Color("#BBBBBBBB");
        }
        
        public static string Error(this string str)
        {
            return str.Color("red");
        }
        
        public static string Warning(this string str)
        {
            return str.Color("yellow");
        }

        public static string Emphasize(this string str)
        {
            return $"<i>{str}</i>";
        }
    }
}