using System.Text;

namespace BMCL.Launch;

public static class ArgumentTokenizer
{
    public static IReadOnlyList<string> Split(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var escaped = false;

        foreach (var character in input)
        {
            if (escaped)
            {
                current.Append(character);
                escaped = false;
                continue;
            }

            switch (character)
            {
                case '\\':
                    escaped = true;
                    break;
                case '"':
                    inQuotes = !inQuotes;
                    break;
                default:
                    if (char.IsWhiteSpace(character) && !inQuotes)
                    {
                        if (current.Length > 0)
                        {
                            result.Add(current.ToString());
                            current.Clear();
                        }
                    }
                    else
                    {
                        current.Append(character);
                    }

                    break;
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }
}
