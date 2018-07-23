namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing
{
    using System;
    using System.Collections.Generic;
    using System.Composition;

    using Microsoft.DocAsCode.Dfm;

    [Export(typeof(IDfmCustomizedRendererPartProvider))]
    public class DfmInteractiveCodeRendererPartProvider : IDfmCustomizedRendererPartProvider
    {
        public const string InteractivePostfix = "-interactive";

        public DfmInteractiveCodeRendererPartProvider()
        {
            Console.WriteLine("hhhhh");
        }

        public IEnumerable<IDfmCustomizedRendererPart> CreateParts(IReadOnlyDictionary<string, object> parameters)
        {
            Console.WriteLine("here");
            object value;
            if (parameters.TryGetValue("no-interactive", out value) && true.Equals(value))
            {
                yield break;
            }
            yield return new DfmInteractiveCodeRendererPart();
        }
    }
}
