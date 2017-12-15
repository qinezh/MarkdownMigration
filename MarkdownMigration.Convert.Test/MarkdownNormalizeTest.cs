using Xunit;

namespace MarkdownMigration.Convert.Test
{
    public class MarkdownNormalizeTest
    {
        [Fact]
        [Trait("Related", "MarkdownNormalize")]
        public void TestResloved_ShortcutXref()
        {
            var source = "abcd\tef\tgh\n  \nnew line";
            var expected = "abcd    ef  gh\n\nnew line";

            var result = NormalizeUtility.Normalize(source);
            Assert.Equal(expected, result);
        }
    }
}
