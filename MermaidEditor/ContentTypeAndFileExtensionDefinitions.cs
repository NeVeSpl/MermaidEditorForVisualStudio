using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MermaidEditor
{
    internal class ContentTypeAndFileExtensionDefinitions
    {
        public const string MMD = "mmd";




        [Export]
        [DisplayName("Mermaid editor file")]
        [Name(MMD)]
        [BaseDefinition("code")]
        [BaseDefinition(CommonEditorConstants.ContentTypeName)]
        internal static ContentTypeDefinition MDDefinition = null;

        [Export]
        [FileExtension(".mmd")]
        [ContentType(MMD)]
        internal static FileExtensionToContentTypeDefinition FileExtensionToMMDDefinition = null;
    }
}
