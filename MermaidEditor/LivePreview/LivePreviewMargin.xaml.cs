using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MermaidEditor.Configuration;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.Web.WebView2.Core;


namespace MermaidEditor.LivePreview
{
    public partial class LivePreviewMargin : UserControl, IWpfTextViewMargin, INotifyPropertyChanged
    {
        private static readonly string TempPath = Path.Combine(Path.GetTempPath(), "Mermaid.EditorForVisualStudio");        
        private readonly IWpfTextView textView;
        private readonly string startupText;        
        
        private string selectedExt = "png";
        public double previewWidth = 900;

        public string SelectedExt
        {
            get
            {
                return selectedExt;
            }
            set
            {
                if (selectedExt != value)
                {
                    selectedExt = value;
                    OnPropertyChanged();
                    TextBuffer_PostChanged(null, null);
                    SaveConfiguration();
                }
            }
        }
        public double PreviewWidth
        {
            get
            {
                return previewWidth;
            }
            set
            {
                if (previewWidth != value)
                {
                    previewWidth = value;
                    OnPropertyChanged();
                    SaveConfiguration();
                }
            }
        }


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
            LoadConfiguration();
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


        private void LoadConfiguration()
        {
            ConfigurationManager.Load();
            SelectedExt = ConfigurationManager.Configuration.SelectedExt;
            PreviewWidth = ConfigurationManager.Configuration.PreviewWidth;
        }
        private void SaveConfiguration()
        {            
            ConfigurationManager.Configuration.SelectedExt = SelectedExt;
            ConfigurationManager.Configuration.PreviewWidth = PreviewWidth;
            ConfigurationManager.Save();
        }


        private async Task InitializeAsync()
        {
            try
            {
                Log($"TempPath = {TempPath}");
                Log($"UserDataPath = {ConfigurationManager.UserDataPath}");

                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: TempPath);                 
                await cWebView.EnsureCoreWebView2Async(env);
                
                var webViewRes = Path.Combine(Path.GetDirectoryName(typeof(LivePreviewMargin).Assembly.Location), "LivePreview");
                cWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("mermaideditor.example", webViewRes, CoreWebView2HostResourceAccessKind.Allow);
               
                cWebView.Source = new Uri("http://mermaideditor.example/index.html"); 
          
                cWebView.NavigationCompleted += WebView_NavigationCompleted;
                cWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                cWebView.WebMessageReceived += CWebView_WebMessageReceived;                
            }
            catch (WebView2RuntimeNotFoundException)
            {
                cMsgWebView2NotInstalled.Visibility = Visibility.Visible;
                cWebView.Visibility = Visibility.Collapsed;
            }
        }


        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Log("WebView_NavigationCompleted()");
            UpdateWebView(startupText);
        }
        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            Log("CoreWebView2_DOMContentLoaded()");
        }
        


        private void TextBuffer_PostChanged(object sender, EventArgs e)
        {
            if (textView.IsTextViewAvailable())
            {
                var text = this.textView.TextSnapshot.GetText();
                UpdateWebView(text);
            }
        }
        private void UpdateWebView(string text)
        {
            if (cWebView.IsWebViewAvailable())
            {
                var enc = JsonEncodedText.Encode(text);
                cWebView.ExecuteScriptAsync($"updateGraph(\"{enc}\", \"{SelectedExt}\");").Forget();
            }
        }


        private async void SaveAsPNG_Button_Click(object sender, RoutedEventArgs e)
        {
            if (textView.IsTextViewAvailable() && cWebView.IsWebViewAvailable())
            {
                var method = selectedExt == "png" ? "getPNG();" : "getSVG();";
                var dataUrl = await cWebView.ExecuteScriptAsync(method); // result will be delivered through CWebView_WebMessageReceived
            }
        }
        private void CWebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string response = e.TryGetWebMessageAsString();
            Log("WebMessageReceived: " + response);

            if (string.IsNullOrEmpty(response))
                return;


            var splited = response.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var prefix = splited[0];
            var data = splited[1];

            switch(prefix)
            {
                case "getPNG":
                    SaveImage(data);
                    break;
                case "getSVG":
                    SaveSvg(data);
                    break;
            }
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
        private void SaveSvg(string data)
        {
            if (textView.IsTextViewAvailable())
            {
                var mmdPath = textView.TextBuffer.GetFileName();
                var svgPath = Path.ChangeExtension(mmdPath, "svg");
                File.WriteAllText(svgPath, data);
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
         

        private static void Log(string message)
        {
            string line = $"=ME4VS=> {DateTimeOffset.Now.ToString("hh:mm:ss.fff")}: {message}";
            Trace.WriteLine(line);
            Debug.WriteLine(line);
        }
    }
}