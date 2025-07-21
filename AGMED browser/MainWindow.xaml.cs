using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace br0wser
{
    public partial class MainWindow : Window
    {
        private BrowserTab _currentTab;
        private ObservableCollection<BookmarkItem> _bookmarks = new ObservableCollection<BookmarkItem>();
        private string bookmarksFile = "bookmarks.json";

        public MainWindow()
        {
            InitializeComponent();
            bookmarksList.ItemsSource = _bookmarks;
            LoadBookmarks();
            InitializeNewTab("https://www.google.com");
        }

        private void InitializeNewTab(string url = null)
        {
            var webView = new WebView2();
            var tabItem = new TabItem { Header = "New Tab" };

            var browserTab = new BrowserTab
            {
                WebView = webView,
                TabItem = tabItem
            };

            tabItem.Content = webView;
            tabItem.Tag = browserTab;
            tabControl.Items.Add(tabItem);
            tabControl.SelectedItem = tabItem;
            _currentTab = browserTab;

            webView.NavigationCompleted += (s, e) =>
            {
                string title = webView.CoreWebView2?.DocumentTitle;
                browserTab.TabItem.Header = string.IsNullOrWhiteSpace(title) ? "New Tab" : title;
                addressBar.Text = webView.Source?.AbsoluteUri ?? string.Empty;
            };

            webView.CoreWebView2InitializationCompleted += (s, e) =>
            {
                if (!string.IsNullOrEmpty(url))
                {
                    webView.CoreWebView2.Navigate(url);
                    webView.CoreWebView2.NewWindowRequested += (s2, e2) =>
                    {
                        e2.Handled = true;
                        _currentTab.WebView.CoreWebView2.Navigate(e2.Uri);
                    };
                }
            };

            _ = webView.EnsureCoreWebView2Async();
        }

        private void NavigateToAddress()
        {
            if (_currentTab?.WebView?.CoreWebView2 != null)
            {
                string address = addressBar.Text.Trim();
                if (!address.StartsWith("http"))
                    address = "https://www.google.com/search?q=" + Uri.EscapeDataString(address);

                _currentTab.WebView.CoreWebView2.Navigate(address);
            }
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                NavigateToAddress();
        }

        private void Go_Click(object sender, RoutedEventArgs e) => NavigateToAddress();
        private void NewTab_Click(object sender, RoutedEventArgs e) => InitializeNewTab("https://www.google.com");

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab?.WebView?.CanGoBack == true)
                _currentTab.WebView.GoBack();
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab?.WebView?.CanGoForward == true)
                _currentTab.WebView.GoForward();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl.SelectedItem is TabItem selected && selected.Tag is BrowserTab browserTab)
            {
                _currentTab = browserTab;
                addressBar.Text = _currentTab.WebView.Source?.AbsoluteUri ?? string.Empty;
            }
        }

        private void AddBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab?.WebView?.CoreWebView2 != null)
            {
                string url = _currentTab.WebView.Source?.AbsoluteUri;
                string title = _currentTab.WebView.CoreWebView2.DocumentTitle;
                if (!string.IsNullOrWhiteSpace(url) && !_bookmarks.Any(b => b.Url == url))
                {
                    _bookmarks.Add(new BookmarkItem { Url = url, Title = title });
                    SaveBookmarks();
                }
            }
        }

        private void RemoveBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (bookmarksList.SelectedItem is BookmarkItem selected)
            {
                _bookmarks.Remove(selected);
                SaveBookmarks();
            }
        }

        private void ToggleBookmarks_Click(object sender, RoutedEventArgs e)
        {
            BookmarksPopup.IsOpen = !BookmarksPopup.IsOpen;
        }

        private void BookmarksList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (bookmarksList.SelectedItem is BookmarkItem selected)
            {
                addressBar.Text = selected.Url;
                NavigateToAddress();
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab?.TabItem != null)
            {
                tabControl.Items.Remove(_currentTab.TabItem);
                if (tabControl.Items.Count > 0)
                    tabControl.SelectedIndex = 0;
                else
                    _currentTab = null;
            }
        }

        private void SaveBookmarks()
        {
            var json = JsonSerializer.Serialize(_bookmarks);
            File.WriteAllText(bookmarksFile, json);
        }

        private void LoadBookmarks()
        {
            if (File.Exists(bookmarksFile))
            {
                try
                {
                    var json = File.ReadAllText(bookmarksFile);
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<BookmarkItem>>(json);
                    if (loaded != null)
                    {
                        foreach (var item in loaded)
                            _bookmarks.Add(item);
                    }
                }
                catch { }
            }
        }
    }

    public class BrowserTab
    {
        public WebView2 WebView { get; set; }
        public TabItem TabItem { get; set; }
    }

    public class BookmarkItem
    {
        public string Url { get; set; }
        public string Title { get; set; }
    }
}
