namespace MarkdownMigration.Common
{
    public static class Helper
    {
        public static int CountEndNewLine(string source)
        {
            var last = source.Length - 1;
            var count = 0;
            while (last >= 0)
            {
                if (source[last] == '\n')
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
