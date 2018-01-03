using System;

namespace MarkdownMigration.Common
{
    public static class Helper
    {
        public static int CountEndNewLine(string content)
        {
            if (content == null)
            {
                throw new ArgumentException($"{nameof(content)} can't be null");
            }

            var last = content.Length - 1;
            var count = 0;
            while (last >= 0)
            {
                if (content[last] == '\n')
                {
                    ++count;
                }
                else
                {
                    return count;
                }

                --last;
            }

            return count;
        }
    }
}
