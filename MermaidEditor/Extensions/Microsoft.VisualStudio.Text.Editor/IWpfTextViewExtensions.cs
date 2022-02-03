using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.Editor
{
    internal static class IWpfTextViewExtensions
    {
        public static bool IsTextViewAvailable(this IWpfTextView textView) => (textView != null) && (textView.IsClosed != true);
    }
}
