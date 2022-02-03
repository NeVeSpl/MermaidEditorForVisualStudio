using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.Web.WebView2.Core;


namespace MermaidEditor.LivePreview
{
    public partial class LivePreviewMargin : UserControl, IWpfTextViewMargin, INotifyPropertyChanged
    {
        private readonly IWpfTextView textView;
        private readonly string startupText;        
        private static readonly string TempPath = Path.Combine(Path.GetTempPath(), "Mermaid.EditorForVisualStudio");            


        public LivePreviewMargin(IWpfTextView textView) : this(textView.TextSnapshot.GetText())
        {
            this.textView = textView;
            this.textView.TextBuffer.PostChanged += TextBuffer_PostChanged;           
        }        
        public LivePreviewMargin(string startupText) : this()
        {
            this.startupText = startupText;
        }
        public LivePreviewMargin()
        {
            InitializeComponent();
            this.Loaded += LivePreviewMargin_Loaded;           
            this.DataContext = this;
        }
       

        bool isInitialized = false;
        private void LivePreviewMargin_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized == false)
            {
                InitializeAsync().Forget();
                isInitialized = true;
            }
        }
     

        async Task InitializeAsync()
        {
            try
            {
                DebugWriteLine($"0 InitializeAsync BEGIN");
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: TempPath);                 
                await cWebView.EnsureCoreWebView2Async(env);

                DebugWriteLine($"1 EnsureCoreWebView2Async END");
                var webViewRes = Path.Combine(Path.GetDirectoryName(typeof(LivePreviewMargin).Assembly.Location), "LivePreview");
                cWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("mermaideditor.example", webViewRes, CoreWebView2HostResourceAccessKind.Allow);
               
                cWebView.Source = new Uri("http://mermaideditor.example/index.html"); 
          
                cWebView.NavigationCompleted += WebView_NavigationCompleted;
                cWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                cWebView.WebMessageReceived += CWebView_WebMessageReceived;

                DebugWriteLine($"2 InitializeAsync END");
            }
            catch (WebView2RuntimeNotFoundException)
            {
                cMsgWebView2NotInstalled.Visibility = Visibility.Visible;
                cWebView.Visibility = Visibility.Collapsed;
            }
        }

        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            DebugWriteLine($"3 CoreWebView2_DOMContentLoaded");
        }
        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            DebugWriteLine($"4 WebView_NavigationCompleted");
            UpdateWebView(startupText);
        }


        private void TextBuffer_PostChanged(object sender, EventArgs e)
        {
            if (textView.IsTextViewAvailable())
            {
                var text = this.textView.TextSnapshot.GetText();
                UpdateWebView(text);
            }
        }
        public void UpdateWebView(string text)
        {
            if (cWebView.CoreWebView2 != null)
            {
                var enc = JsonEncodedText.Encode(text);
                cWebView.ExecuteScriptAsync($"updateGraph(\"{enc}\");").Forget();
            }
        }


        private async void SaveAsPNG_Button_Click(object sender, RoutedEventArgs e)
        {
            if (textView.IsTextViewAvailable() && cWebView.CoreWebView2 != null)
            {
                var dataUrl = await cWebView.ExecuteScriptAsync($"getPNG();"); // result will be delivered through CWebView_WebMessageReceived
            }
        }
        private void CWebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string dataUrl = e.TryGetWebMessageAsString();
            SaveImage(dataUrl);
        }
        private void SaveImage(string dataUrl)
        {
            if (textView.IsTextViewAvailable())
            {
                var mmdPath = textView.TextBuffer.GetFileName();
                var pngPath = Path.ChangeExtension(mmdPath, "png");
                var prefixLength = "data:image/png;base64,".Length;

                if (!string.IsNullOrEmpty(dataUrl) && dataUrl != "null" && dataUrl != "{}" && dataUrl.Length > prefixLength)
                {
                    var base64Data = dataUrl.Remove(0, prefixLength);
                    var img = base64Data.ConvertBase64ToImage();
                    try
                    {
                        img.Save(pngPath);
                    }
                    catch (Exception ex)
                    { 
                    }
                }
            }
        }     


        #region IWpfTextViewMargin
        public FrameworkElement VisualElement => this;
        public double MarginSize => this.Width;
        public bool Enabled => this.Enabled;

        public void Dispose()
        {
            textView.TextBuffer.PostChanged -= TextBuffer_PostChanged;
            if (cWebView.CoreWebView2 != null)
            {
                cWebView.Dispose();
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return this;
        }
        #endregion

        private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            Debug.WriteLine($"GridSplitter_DragDelta {contentColumn.ActualWidth} {e.HorizontalChange}");
            double newWidth = contentColumn.ActualWidth;
            if (!double.IsNaN(newWidth))
            {
                
            }
        }
        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Debug.WriteLine($"GridSplitter_DragCompleted {contentColumn.ActualWidth} {e.HorizontalChange}");
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion     
        
        private void DebugWriteLine(string text) =>  Debug.WriteLine($"===> {text} " + DateTimeOffset.Now.ToString("hh:mm:ss.fff")); 
    }
}