using System.ComponentModel.Composition;
using HlslTools.Compilation;
using HlslTools.Syntax;

namespace HlslTools.VisualStudio.IntelliSense.QuickInfo.QuickInfoModelProviders
{
    [Export(typeof(IQuickInfoModelProvider))]
    internal sealed class AttributeQuickInfoModelProvider : QuickInfoModelProvider<AttributeSyntax>
    {
        protected override QuickInfoModel CreateModel(SemanticModel semanticModel, SourceLocation position, AttributeSyntax node)
        {
            if (!node.Name.SourceRange.ContainsOrTouches(position))
                return null;

            if (!node.Name.Name.Span.IsInRootFile)
                return null;

            var symbol = semanticModel.GetSymbol(node);
            if (symbol == null)
                return null;

            return QuickInfoModel.ForSymbol(semanticModel, node.Name.Name.Span, symbol);
        }
    }
}