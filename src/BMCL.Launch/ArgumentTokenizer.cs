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
        var hasToken = false;
        var index = 0;

        while (index < input.Length)
        {
            var character = input[index];

            if (character == '"')
            {
                inQuotes = !inQuotes;
                hasToken = true;
                index++;
                continue;
            }

            if (!inQuotes && char.IsWhiteSpace(character))
            {
                if (hasToken)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }

                index++;
                continue;
            }

            if (character == '\\' &&
                index + 1 < input.Length &&
                input[index + 1] == '"')
            {
                current.Append('"');
                hasToken = true;
                index += 2;
                continue;
            }

            current.Append(character);
            hasToken = true;
            index++;
        }

        if (hasToken)
        {
            result.Add(current.ToString());
        }

        return result;
    }
}
