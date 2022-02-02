using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;


namespace MermaidEditor.LivePreview
{
    

    public partial class LivePreviewMargin : UserControl, IWpfTextViewMargin, INotifyPropertyChanged
    {
        private readonly IWpfTextView textView;
        private readonly string startupText;
        public static readonly string TempPath = Path.Combine(Path.GetTempPath(), "Mermaid.EditorForVisualStudio");
            


        public LivePreviewMargin(IWpfTextView textView, string startupText) : this(startupText)
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
            InitializeAsync();
            this.DataContext = this;
        }

        

        async void InitializeAsync()
        {
            try
            {
                DebugWriteLine($"0 CoreWebView2Environment.CreateAsync");
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: TempPath);

                DebugWriteLine($"1 EnsureCoreWebView2Async");
                // NOTE: this waits until the first page is navigated - then continues
                //       executing the next line of code!
                await cWebView.EnsureCoreWebView2Async(env);

                DebugWriteLine($"2 SetVirtualHostNameToFolderMapping");
                var webViewRes = Path.Combine(Path.GetDirectoryName(typeof(LivePreviewMargin).Assembly.Location), "LivePreview");
                cWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("vwresources", webViewRes, CoreWebView2HostResourceAccessKind.Allow);

                DebugWriteLine($"3 cWebView.Source");
                cWebView.Source = new Uri("http://vwresources/index.html");            
               

               
                // Almost always need this event for something    
                cWebView.NavigationCompleted += WebView_NavigationCompleted;
                cWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                cWebView.WebMessageReceived += CWebView_WebMessageReceived;   


                DebugWriteLine($"4 end"); 
            }
            catch (WebView2RuntimeNotFoundException)
            {
                cMsgWebView2NotInstalled.Visibility = Visibility.Visible;
                cWebView.Visibility = Visibility.Collapsed;
            }
        }

        private void CWebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string s = e.TryGetWebMessageAsString();

            var path = textView.TextBuffer.GetFileName();
            var pp = Path.ChangeExtension(path, "png");
            // window.Content = lp;
            // window.Show();
            var dataUrl = s;

            // var dataUrls = JsonSerializationUtils.Deserialize<string>(dataUrl);
            if (!string.IsNullOrEmpty(dataUrl) && dataUrl != "null" && dataUrl != "{}")
            {
                var base64Data = dataUrl.Remove(0, "data:image/png;base64,".Length);

                //var matchGroups = Regex.Match(dataUrl, @"^data:((?<type>[\w\/]+))?;base64,(?<data>.+)$").Groups;
                // var base64Data = matchGroups["data"].Value;

                var im = ConvertBase64ToImage(base64Data);
                im.Save(pp);

                //SaveAsPNG(pp);
            }
        }

        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            DebugWriteLine($"? CoreWebView2_DOMContentLoaded");
        }

        private void TextBuffer_PostChanged(object sender, EventArgs e)
        {
            if (this.textView != null)
            {
                if (textView.IsClosed != true)
                {
                    var text = this.textView.TextSnapshot.GetText();
                    UpdateWEbView(text);
                }
            }
        }
        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            DebugWriteLine($"5 WebView_NavigationCompleted");
            UpdateWEbView(startupText);
        }

        void DebugWriteLine(string text)
        {
            Debug.WriteLine($"===> {text} " + DateTimeOffset.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"));
        }

     
        public void UpdateWEbView(string text)
        {
            
                if (cWebView.CoreWebView2 != null)
                {
                    var enc = JsonEncodedText.Encode(text);
                    cWebView.ExecuteScriptAsync($"updateGraph(\"{enc}\");").Forget();
                }
            
        }







        #region IWpfTextViewMargin
        public FrameworkElement VisualElement => this;
        public double MarginSize => this.Width;
        public bool Enabled => this.Enabled;

        public void Dispose()
        {
            
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

        protected void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        #endregion


        private async void SaveAsPNG(string pp)
        {
            MemoryStream ms = new MemoryStream();
            await cWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
            var bm = new Bitmap(ms);
            bm.Save(pp);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //var window = new Window();
            //var lp =  new LivePreviewMargin(this.textView.TextSnapshot.GetText());
            var path = textView.TextBuffer.GetFileName();
            var pp =  Path.ChangeExtension(path, "png");
            // window.Content = lp;
            // window.Show();

            var dataUrl =  await cWebView.ExecuteScriptAsync($"getPNG();");
           // var dataUrls = JsonSerializationUtils.Deserialize<string>(dataUrl);
            if (!string.IsNullOrEmpty(dataUrl) && dataUrl != "null" && dataUrl != "{}")
            {
                var base64Data = dataUrl.Remove(0, "data:image/png;base64,".Length + 1);

                //var matchGroups = Regex.Match(dataUrl, @"^data:((?<type>[\w\/]+))?;base64,(?<data>.+)$").Groups;
                // var base64Data = matchGroups["data"].Value;

                var im = ConvertBase64ToImage(base64Data);
                im.Save(pp);

                //SaveAsPNG(pp);
            }
        }

        public System.Drawing.Image ConvertBase64ToImage(string base64)    => (Bitmap)new ImageConverter().ConvertFrom(Convert.FromBase64String(base64));
    }
}