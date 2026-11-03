namespace CodeCrafters.Bittorrent.Extensions;

public static class Extensions
{
    public static IEnumerable<string> Splits(this string str, int n)
    {
        if (String.IsNullOrEmpty(str) || n < 1) {
            throw new ArgumentException();
        }
 
        return Enumerable.Range(0, str.Length / n)
            .Select(i => str.Substring(i * n, n));
    }
}