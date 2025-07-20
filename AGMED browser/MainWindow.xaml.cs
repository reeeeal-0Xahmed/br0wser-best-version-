using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AGMED_browser
{
    public partial class MainWindow : Window
    {
        public class BrowserTab
        {
            public string Header { get; set; }
            public WebView2 WebView { get; set; }
        }

        public class Bookmark
        {
            public string Title { get; set; }
            public string Url { get; set; }
        }

        private const string HomePage = "https://www.google.com";
        private const string BookmarksFile = "bookmarks.json";
        private readonly string _userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AGMED_Browser");

        private List<Bookmark> _bookmarks = new List<Bookmark>();
        private BrowserTab _currentTab;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            LoadBookmarks();
            InitializeNewTab();
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private async System.Threading.Tasks.Task InitializeWebViewAsync(WebView2 webView)
        {
            try
            {
                var environment = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: _userDataFolder);

                await webView.EnsureCoreWebView2Async(environment);
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                webView.CoreWebView2.Navigate(HomePage);

                webView.NavigationStarting += WebView_NavigationStarting;
                webView.NavigationCompleted += WebView_NavigationCompleted;
                webView.CoreWebView2.HistoryChanged += WebView_HistoryChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeNewTab(string url = null)
        {
            var webView = new WebView2();
            webView.Tag = tabControl.Items.Count + 1;

            var newTab = new BrowserTab
            {
                Header = "New Tab",
                WebView = webView
            };

            tabControl.Items.Add(newTab);
            tabControl.SelectedItem = newTab;
            _currentTab = newTab;

            webViewContainer.Children.Clear();
            webViewContainer.Children.Add(webView);

            if (!string.IsNullOrEmpty(url))
            {
                webView.Source = new Uri(url);
            }
            else
            {
                _ = InitializeWebViewAsync(webView);
            }
        }

        private void LoadBookmarks()
        {
            try
            {
                if (File.Exists(BookmarksFile))
                {
                    var json = File.ReadAllText(BookmarksFile);
                    _bookmarks = System.Text.Json.JsonSerializer.Deserialize<List<Bookmark>>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load bookmarks: {ex.Message}");
            }
        }

        private void SaveBookmarks()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_bookmarks);
                File.WriteAllText(BookmarksFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save bookmarks: {ex.Message}");
            }
        }

        private void UpdateNavigationButtons()
        {
            if (_currentTab?.WebView?.CoreWebView2 == null) return;

            Dispatcher.Invoke(() =>
            {
                btnBack.IsEnabled = _currentTab.WebView.CoreWebView2.CanGoBack;
                btnForward.IsEnabled = _currentTab.WebView.CoreWebView2.CanGoForward;
                txtAddress.Text = _currentTab.WebView.Source?.ToString() ?? "";
                _currentTab.Header = _currentTab.WebView.CoreWebView2.DocumentTitle ?? "New Tab";
            });
        }

        private void NavigateToAddress()
        {
            if (_currentTab?.WebView?.CoreWebView2 == null) return;

            var address = txtAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(address)) return;

            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri))
            {
                address = address.Contains(".") && !address.Contains(" ")
                    ? $"https://{address}"
                    : $"https://www.google.com/search?q={Uri.EscapeDataString(address)}";
            }

            _currentTab.WebView.CoreWebView2.Navigate(address);
        }

        private void DeleteBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is Bookmark bookmarkToDelete)
            {
                _bookmarks.Remove(bookmarkToDelete);
                SaveBookmarks();
                bookmarksList.ItemsSource = null;
                bookmarksList.ItemsSource = _bookmarks;
                e.Handled = true;
            }
        }

        #region Event Handlers
        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                lblStatus.Text = "Loading...";
                progressBar.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true;
            });
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                lblStatus.Text = "Done";
                progressBar.Visibility = Visibility.Collapsed;
                UpdateNavigationButtons();
            });
        }

        private void WebView_HistoryChanged(object sender, object e)
        {
            UpdateNavigationButtons();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl.SelectedItem is BrowserTab selectedTab)
            {
                _currentTab = selectedTab;
                webViewContainer.Children.Clear();
                webViewContainer.Children.Add(selectedTab.WebView);
                UpdateNavigationButtons();
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is BrowserTab tabToClose)
            {
                try
                {
                    tabToClose.WebView?.CoreWebView2?.ExecuteScriptAsync("document.querySelectorAll('video,audio').forEach(media => media.pause());");
                    tabToClose.WebView?.CoreWebView2?.Stop();
                    tabToClose.WebView?.Dispose();
                }
                catch { }

                if (tabControl.Items.Count == 1)
                {
                    InitializeNewTab();
                }

                tabControl.Items.Remove(tabToClose);
                e.Handled = true;
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e) => _currentTab?.WebView?.CoreWebView2?.GoBack();
        private void btnForward_Click(object sender, RoutedEventArgs e) => _currentTab?.WebView?.CoreWebView2?.GoForward();
        private void btnRefresh_Click(object sender, RoutedEventArgs e) => _currentTab?.WebView?.CoreWebView2?.Reload();
        private void btnHome_Click(object sender, RoutedEventArgs e) => _currentTab?.WebView?.CoreWebView2?.Navigate(HomePage);
        private void btnGo_Click(object sender, RoutedEventArgs e) => NavigateToAddress();

        private void txtAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) NavigateToAddress();
        }

        private void btnNewTab_Click(object sender, RoutedEventArgs e) => InitializeNewTab();

        private void btnBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab?.WebView?.Source != null)
            {
                var url = _currentTab.WebView.Source.ToString();
                var title = _currentTab.WebView.CoreWebView2?.DocumentTitle ?? url;

                if (!_bookmarks.Any(b => b.Url == url))
                {
                    _bookmarks.Add(new Bookmark { Title = title, Url = url });
                    SaveBookmarks();
                    MessageBox.Show("Bookmark added!", "Bookmark", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("This page is already bookmarked!", "Bookmark", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btnShowBookmarks_Click(object sender, RoutedEventArgs e)
        {
            bookmarksList.ItemsSource = _bookmarks;
            bookmarksPopup.IsOpen = true;
        }

        private void BookmarksList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBlock && bookmarksList.SelectedItem is Bookmark selectedBookmark)
            {
                InitializeNewTab(selectedBookmark.Url);
                bookmarksPopup.IsOpen = false;
            }
        }
        #endregion

        protected override void OnClosed(EventArgs e)
        {
            foreach (BrowserTab tab in tabControl.Items)
            {
                tab.WebView?.Dispose();
            }
            base.OnClosed(e);
        }
    }
}