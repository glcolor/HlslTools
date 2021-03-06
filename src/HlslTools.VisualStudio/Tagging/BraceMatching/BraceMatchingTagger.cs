using System;
using System.Collections.Generic;
using System.Threading;
using HlslTools.VisualStudio.Parsing;
using HlslTools.VisualStudio.Util.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace HlslTools.VisualStudio.Tagging.BraceMatching
{
    internal sealed class BraceMatchingTagger : AsyncTagger<ITextMarkerTag>
    {
        private readonly ITextView _textView;
        private readonly BraceMatcher _braceMatcher;

        private readonly List<ITagSpan<ITextMarkerTag>> _emptyList = new List<ITagSpan<ITextMarkerTag>>();
        private readonly ITextMarkerTag _tag = new TextMarkerTag("bracehighlight");

        public BraceMatchingTagger(BackgroundParser backgroundParser, ITextView textView, BraceMatcher braceMatcher)
        {
            backgroundParser.SubscribeToThrottledSyntaxTreeAvailable(BackgroundParserSubscriptionDelay.NearImmediate,
                async x => await InvalidateTags(x.Snapshot, x.CancellationToken));

            textView.Caret.PositionChanged += OnCaretPositionChanged;

            _textView = textView;
            _braceMatcher = braceMatcher;
        }

        private async void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            await InvalidateTags(_textView.TextSnapshot, CancellationToken.None);
        }

        protected override Tuple<ITextSnapshot, List<ITagSpan<ITextMarkerTag>>> GetTags(ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            if (snapshot != _textView.TextSnapshot)
                return Tuple.Create(snapshot, _emptyList);

            var syntaxTree = snapshot.GetSyntaxTree(cancellationToken);

            var unmappedPosition = _textView.GetPosition(snapshot);
            var position =  syntaxTree.MapRootFilePosition(unmappedPosition);

            var result = _braceMatcher.MatchBraces(syntaxTree, position);
            if (!result.IsValid)
                return Tuple.Create(snapshot, _emptyList);

            var leftTag = new TagSpan<ITextMarkerTag>(new SnapshotSpan(snapshot, result.Left.Start, result.Left.Length), _tag);
            var rightTag = new TagSpan<ITextMarkerTag>(new SnapshotSpan(snapshot, result.Right.Start, result.Right.Length), _tag);

            return Tuple.Create(snapshot, new List<ITagSpan<ITextMarkerTag>> { leftTag, rightTag });
        }
    }
}