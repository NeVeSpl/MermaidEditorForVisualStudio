using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;


namespace MermaidEditor.LivePreview
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name("MermaidEditor LivePreviewMarginProvider")]
    [Order(After = PredefinedMarginNames.RightControl)]
    [MarginContainer(PredefinedMarginNames.Right)]
    [ContentType(ContentTypeAndFileExtensionDefinitions.MMD)]   
    [TextViewRole(PredefinedTextViewRoles.Debuggable)]
    internal class LivePreviewMarginProvider : IWpfTextViewMarginProvider
    {
        [Import]
        private VisualStudioWorkspace vsWorkspace = null;

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {            
            ITextDocument document = wpfTextViewHost.TextView.TextDataModel.DocumentBuffer.GetTextDocument();
            
            return wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(() => new LivePreviewMargin(wpfTextViewHost.TextView));
        }
    }
}