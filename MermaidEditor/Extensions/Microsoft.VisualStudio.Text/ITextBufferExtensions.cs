using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// source : https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/blob/master/src/toolkit/Community.VisualStudio.Toolkit.Shared/ExtensionMethods/ITextBufferExtensions.cs
    /// </summary>
    public static class ITextBufferExtensions
    {
        public static ITextDocument GetTextDocument(this ITextBuffer buffer)
        {
            if (buffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var property))
            {
                return property;
            }

            return null;
        }

        /// <summary>
        /// Gets the file name on disk associated with the buffer.
        /// 
        /// 
        /// </summary>
        public static string GetFileName(this ITextBuffer buffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!buffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter))
            {
                return null;
            }

            string ppzsFilename = null;
            int returnCode = -1;

            if (bufferAdapter is IPersistFileFormat persistFileFormat)
            {
                try
                {
                    returnCode = persistFileFormat.GetCurFile(out ppzsFilename, out uint pnFormatIndex);
                }
                catch (NotImplementedException)
                {
                    return null;
                }
            }

            if (returnCode != VSConstants.S_OK)
            {
                return null;
            }

            return ppzsFilename;
        }
    }
}
