using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownMigration.Convert
{
    [Flags]
    public enum MigrationRule
    {
        Xref = 0x1,
        InclusionInline = 0x2,
        Image = 0x4,
        Link = 0x8,
        Strong = 0x10,
        Em = 0x20,
        Table = 0x40,

        Note = 0x80,
        InclusionBlock = 0x100,
        Code = 0x200,
        Html = 0x400,
        Heading = 0x800,
        List = 0x1000,
        BlockQuote = 0x2000,
        YamlHeader = 0x4000,
        HtmlBlock = 0x8000,

        Normalize = 0x10000,

        None = 0x0,
        Paragraph = 0x4F,
        All = 0x1FFFF
    }

}
