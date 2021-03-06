﻿using System;
using System.ComponentModel.Composition;
using HlslTools.VisualStudio.Util.Extensions;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace HlslTools.VisualStudio.Parsing
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(HlslConstants.ContentTypeName)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class BackgroundParserManager : IWpfTextViewCreationListener
    {
        public void TextViewCreated(IWpfTextView textView)
        {
            // Ensure BackgroundParser is created.
            textView.TextBuffer.GetBackgroundParser();
            textView.Closed += OnViewClosed;
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;
            view.Closed -= OnViewClosed;

            var backgroundParser = view.TextBuffer.GetBackgroundParser();
            backgroundParser?.Dispose();
        }
    }
}