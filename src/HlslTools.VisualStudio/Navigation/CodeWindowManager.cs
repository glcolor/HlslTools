﻿using System;
using HlslTools.VisualStudio.Util.Extensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;

namespace HlslTools.VisualStudio.Navigation
{
    internal sealed class CodeWindowManager : IVsCodeWindowManager
    {
        private IVsDropdownBarClient _dropdownBarClient;

        public CodeWindowManager(IVsCodeWindow codeWindow, SVsServiceProvider serviceProvider)
        {
            CodeWindow = codeWindow;
            ServiceProvider = serviceProvider;
        }

        public IVsCodeWindow CodeWindow { get; }
        public SVsServiceProvider ServiceProvider { get; }

        public int AddAdornments()
        {
            IVsTextView textView;
            if (ErrorHandler.Succeeded(CodeWindow.GetPrimaryView(out textView)) && textView != null)
                ErrorHandler.ThrowOnFailure(OnNewView(textView));
            if (ErrorHandler.Succeeded(CodeWindow.GetSecondaryView(out textView)) && textView != null)
                ErrorHandler.ThrowOnFailure(OnNewView(textView));

            if (ServiceProvider.GetHlslToolsService().LanguagePreferences.NavigationBar)
                return AddDropDownBar();

            return VSConstants.S_OK;
        }

        private int AddDropDownBar()
        {
            int comboBoxCount;
            IVsDropdownBarClient client;
            if (TryCreateDropdownBarClient(out comboBoxCount, out client))
            {
                ErrorHandler.ThrowOnFailure(AddDropdownBar(comboBoxCount, client));
                _dropdownBarClient = client;
            }

            return VSConstants.S_OK;
        }

        public int OnNewView(IVsTextView view)
        {
            return VSConstants.S_OK;
        }

        int IVsCodeWindowManager.OnNewView(IVsTextView pView)
        {
            if (pView == null)
                throw new ArgumentNullException(nameof(pView));
            return OnNewView(pView);
        }

        public int RemoveAdornments()
        {
            return RemoveDropDownBar();
        }

        private int RemoveDropDownBar()
        {
            var manager = CodeWindow as IVsDropdownBarManager;
            if (manager == null)
                return VSConstants.E_FAIL;

            IVsDropdownBar dropdownBar;
            int hr = manager.GetDropdownBar(out dropdownBar);
            if (ErrorHandler.Succeeded(hr) && dropdownBar != null)
            {
                IVsDropdownBarClient client;
                hr = dropdownBar.GetClient(out client);
                if (ErrorHandler.Succeeded(hr) && client == _dropdownBarClient)
                {
                    _dropdownBarClient = null;
                    return manager.RemoveDropdownBar();
                }
            }

            _dropdownBarClient = null;
            return VSConstants.S_OK;
        }

        private bool TryCreateDropdownBarClient(out int comboBoxCount, out IVsDropdownBarClient client)
        {
            var componentModel = ServiceProvider.GetComponentModel();
            var editorAdaptersFactory = componentModel.DefaultExportProvider.GetExportedValueOrDefault<IVsEditorAdaptersFactoryService>();
            var bufferGraphFactoryService = componentModel.DefaultExportProvider.GetExportedValue<IBufferGraphFactoryService>();

            var textView = editorAdaptersFactory.GetWpfTextView(CodeWindow.GetPrimaryView());

            var editorNavigationSourceProvider = componentModel.DefaultExportProvider.GetExportedValueOrDefault<EditorNavigationSourceProvider>();
            var editorNavigationSource = editorNavigationSourceProvider.TryCreateEditorNavigationSource(textView.TextBuffer);

            comboBoxCount = 2;
            client = new EditorNavigationDropdownBarClient(CodeWindow, editorAdaptersFactory, editorNavigationSource, bufferGraphFactoryService);

            return true;
        }

        private int AddDropdownBar(int comboBoxCount, IVsDropdownBarClient client)
        {
            var manager = CodeWindow as IVsDropdownBarManager;
            if (manager == null)
                throw new NotSupportedException();

            IVsDropdownBar dropdownBar;
            var hr = manager.GetDropdownBar(out dropdownBar);
            if (ErrorHandler.Succeeded(hr) && dropdownBar != null)
            {
                hr = manager.RemoveDropdownBar();
                if (ErrorHandler.Failed(hr))
                    return hr;
            }

            return manager.AddDropdownBar(comboBoxCount, client);
        }

        public int ToggleNavigationBar(bool fEnable)
        {
            return fEnable ? AddDropDownBar() : RemoveDropDownBar();
        }
    }
}