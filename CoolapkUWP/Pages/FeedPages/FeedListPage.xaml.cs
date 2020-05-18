﻿using CoolapkUWP.Controls.ViewModels;
using CoolapkUWP.Helpers;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace CoolapkUWP.Pages.FeedPages
{
    enum FeedListType
    {
        UserPageList,
        TagPageList,
        DYHPageList
    }

    public sealed partial class FeedListPage : Page
    {
        interface IFeedListDataProvider
        {
            string Id { get; }
            FeedListType ListType { get; }
            Task<object> GetDetail();
            Task<List<FeedViewModel>> GetFeeds(int p = -1);
            string GetTitleBarText(object o);
        }
        interface ICanChangeSelectedIndex : IFeedListDataProvider
        {
            int SelectedIndex { get; set; }
            void Reset();
        }
        class UserPageDataProvider : IFeedListDataProvider
        {
            public string Id { get; private set; }
            int page;
            int firstItem, lastItem;
            public FeedListType ListType { get => FeedListType.UserPageList; }
            public UserPageDataProvider(string uid) => Id = uid;

            public async Task<object> GetDetail()
            {
                JObject detail = (JObject)await DataHelper.GetData(DataType.GetUserSpace, Id);
                if (detail != null)
                {
                    return new UserDetail
                    {
                        FollowStatus = detail.Value<int>("uid").ToString() == SettingsHelper.Get<string>("uid") ? string.Empty : detail.Value<int>("isFollow") == 0 ? "关注" : "取消关注",
                        FollowNum = detail.Value<int>("follow"),
                        FansNum = detail.Value<int>("fans"),
                        Level = detail.Value<int>("level"),
                        Gender = detail.Value<int>("gender") == 1 ? "♂" : (detail.Value<int>("gender") == 0 ? "♀" : string.Empty),
                        Logintime = $"{DataHelper.ConvertTime(detail.Value<int>("logintime"))}活跃",
                        FeedNum = detail.Value<int>("feed"),
                        UserFaceUrl = detail.Value<string>("userAvatar"),
                        UserName = detail.Value<string>("username"),
                        Bio = detail.Value<string>("bio"),
                        BackgroundUrl = detail.Value<string>("cover"),
                        Verify_title = detail.Value<string>("verify_title"),
                        City = $"{detail.Value<string>("province")} {detail.Value<string>("city")}",
                        Astro = detail.Value<string>("astro"),
                        UserFace = await ImageCacheHelper.GetImage(ImageType.SmallAvatar, detail.Value<string>("userSmallAvatar")),
                        Background = new ImageBrush { ImageSource = await ImageCacheHelper.GetImage(ImageType.OriginImage, detail.Value<string>("cover")), Stretch = Stretch.UniformToFill }
                    };
                }
                else return new UserDetail();
            }

            public async Task<List<FeedViewModel>> GetFeeds(int p = -1)
            {
                if (p == 1 && page == 0) page = 1;
                JArray array = (JArray)await DataHelper.GetData(DataType.GetUserFeeds,
                                                                Id,
                                                                p == -1 ? ++page : p,
                                                                firstItem == 0 ? string.Empty : $"&firstItem={firstItem}",
                                                                lastItem == 0 ? string.Empty : $"&lastItem={lastItem}");
                if (!(array is null) && array.Count != 0)
                {
                    if (page == 1 || p == 1)
                        firstItem = array.First.Value<int>("id");
                    lastItem = array.Last.Value<int>("id");
                    List<FeedViewModel> FeedsCollection = new List<FeedViewModel>();
                    foreach (var i in array) FeedsCollection.Add(new FeedViewModel(i));
                    return FeedsCollection;
                }
                else
                {
                    page--;
                    return null;
                }
            }

            public string GetTitleBarText(object o) => (o as UserDetail).UserName;
        }
        class TagPageDataProvider : ICanChangeSelectedIndex
        {
            public string Id { get; private set; }
            int page, _selectedIndex;
            public int SelectedIndex
            {
                get => _selectedIndex;
                set
                {
                    if (value > -1)
                        _selectedIndex = value;
                }
            }
            double firstItem, lastItem;
            public FeedListType ListType { get => FeedListType.TagPageList; }
            public TagPageDataProvider(string tag) => Id = tag;

            public void Reset() => firstItem = lastItem = page = 0;

            public async Task<object> GetDetail()
            {
                JObject detail = (JObject)await DataHelper.GetData(DataType.GetTagDetail, Id);
                if (detail != null)
                {
                    return new TopicDetail
                    {
                        Logo = await ImageCacheHelper.GetImage(ImageType.Icon, detail.Value<string>("logo")),
                        Title = detail.Value<string>("title"),
                        Description = detail.Value<string>("description"),
                        FollowNum = detail.TryGetValue("follownum", out JToken t) ? int.Parse(t.ToString()) : detail.Value<int>("follow_num"),
                        CommentNum = detail.TryGetValue("commentnum", out JToken tt) ? int.Parse(tt.ToString()) : detail.Value<int>("rating_total_num"),
                        SelectedIndex = SelectedIndex
                    };
                }
                else return new TopicDetail();
            }

            public async Task<List<FeedViewModel>> GetFeeds(int p = -1)
            {
                string sortType = string.Empty;
                switch (SelectedIndex)
                {
                    case 0:
                        sortType = "lastupdate_desc";
                        break;
                    case 1:
                        sortType = "dateline_desc";
                        break;
                    case 2:
                        sortType = "popular";
                        break;
                }
                if (p == 1 && page == 0) page = 1;
                JArray array = (JArray)await DataHelper.GetData(DataType.GetTagFeeds,
                                                                Id,
                                                                p == -1 ? ++page : p,
                                                                firstItem == 0 ? string.Empty : $"&firstItem={firstItem}",
                                                                lastItem == 0 ? string.Empty : $"&lastItem={lastItem}",
                                                                sortType);
                if (!(array is null) && array.Count != 0)
                {
                    if (page == 1 || p == 1)
                        firstItem = array.First.Value<int>("id");
                    lastItem = array.Last.Value<int>("id");
                    List<FeedViewModel> FeedsCollection = new List<FeedViewModel>();
                    foreach (var i in array) FeedsCollection.Add(new FeedViewModel(i));
                    return FeedsCollection;
                }
                else
                {
                    page--;
                    return null;
                }
            }

            public string GetTitleBarText(object o) => (o as TopicDetail).Title;
        }
        class DYHPageDataProvider : ICanChangeSelectedIndex
        {
            public string Id { get; private set; }
            int page, _selectedIndex;
            public int SelectedIndex
            {
                get => _selectedIndex;
                set
                {
                    if (value > -1)
                        _selectedIndex = value;
                }
            }
            double firstItem, lastItem;
            public FeedListType ListType { get => FeedListType.DYHPageList; }
            public DYHPageDataProvider(string id) => Id = id;

            public void Reset() => firstItem = lastItem = page = 0;

            public async Task<object> GetDetail()
            {
                JObject detail = (JObject)await DataHelper.GetData(DataType.GetDyhDetail, Id);
                if (detail != null)
                {
                    bool showUserButton = detail.Value<int>("uid") != 0;
                    return new DYHDetail
                    {
                        FollowNum = detail.Value<int>("follownum"),
                        ShowComboBox = detail.Value<int>("is_open_discuss") == 1,
                        Logo = await ImageCacheHelper.GetImage(ImageType.Icon, detail.Value<string>("logo")),
                        Title = detail.Value<string>("title"),
                        Description = detail.Value<string>("description"),
                        ShowUserButton = showUserButton,
                        Url = showUserButton ? detail["userInfo"].Value<string>("url") : string.Empty,
                        UserName = showUserButton ? detail["userInfo"].Value<string>("username") : string.Empty,
                        UserAvatar = showUserButton ? await ImageCacheHelper.GetImage(ImageType.SmallAvatar, detail["userInfo"].Value<string>("userSmallAvatar").Replace("\"", string.Empty)) : null,
                        SelectedIndex = SelectedIndex,
                    };
                }
                else return new DYHDetail();
            }

            public async Task<List<FeedViewModel>> GetFeeds(int p = -1)
            {
                if (p == 1 && page == 0) page = 1;
                JArray array = (JArray)await DataHelper.GetData(DataType.GetDyhFeeds,
                                                                Id,
                                                                SelectedIndex == 0 ? "all" : "square",
                                                                p == -1 ? ++page : p,
                                                                firstItem == 0 ? string.Empty : $"&firstItem={firstItem}",
                                                                (lastItem == 0) ? string.Empty : $"&lastItem={lastItem}");
                if (!(array is null) && array.Count != 0)
                {
                    if (page == 1 || p == 1)
                        firstItem = array.First.Value<int>("id");
                    lastItem = array.Last.Value<int>("id");
                    List<FeedViewModel> FeedsCollection = new List<FeedViewModel>();
                    foreach (var i in array) FeedsCollection.Add(new FeedViewModel(i));
                    return FeedsCollection;
                }
                else
                {
                    page--;
                    return null;
                }
            }

            public string GetTitleBarText(object o) => (o as DYHDetail).Title;
        }

        IFeedListDataProvider provider;
        ScrollViewer VScrollViewer;
        readonly ObservableCollection<object> itemCollection = new ObservableCollection<object>();

        public FeedListPage() => this.InitializeComponent();
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            object[] vs = e.Parameter as object[];
            string str = vs[1] as string;
            FeedListType feedListType = (FeedListType)vs[0];
            if (!string.IsNullOrEmpty(str))
            {
                if (feedListType != provider?.ListType || (feedListType == provider?.ListType && str != provider.Id))
                {
                    if (itemCollection.Count > 0) itemCollection.Clear();
                    switch (feedListType)
                    {
                        case FeedListType.UserPageList:
                            if (str == "0")
                            {
                                Frame.GoBack();
                                return;
                            }
                            else provider = new UserPageDataProvider(str);
                            titleBar.ComboBoxVisibility = Visibility.Collapsed;
                            break;
                        case FeedListType.TagPageList:
                            provider = new TagPageDataProvider(str);
                            titleBar.ComboBoxVisibility = Visibility.Visible;
                            titleBar.ComboBoxItemsSource = new string[] { "最近回复", "按时间排序", "按热度排序" };
                            titleBar.ComboBoxSelectedIndex = 0;
                            break;
                        case FeedListType.DYHPageList:
                            provider = new DYHPageDataProvider(str);
                            titleBar.ComboBoxVisibility = Visibility.Collapsed;
                            titleBar.ComboBoxItemsSource = new string[] { "精选", "广场" };
                            break;
                    }
                    Refresh();
                }
            }
            else Frame.GoBack();
            if (VScrollViewer is null)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(300);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        VScrollViewer = VisualTree.FindDescendantByName(listView, "ScrollViewer") as ScrollViewer;
                        VScrollViewer.ViewChanged += async (s, ee) =>
                        {
                            if (!ee.IsIntermediate && VScrollViewer.VerticalOffset == VScrollViewer.ScrollableHeight)
                            {
                                UIHelper.ShowProgressBar();
                                List<FeedViewModel> feeds = await provider.GetFeeds();
                                if (feeds != null)
                                    foreach (var item in feeds)
                                        itemCollection.Add(item);
                                UIHelper.HideProgressBar();
                            }
                        };
                    });
                });
            }
        }
        async void Refresh()
        {
            UIHelper.ShowProgressBar();
            if (itemCollection.Count > 0) itemCollection.RemoveAt(0);
            itemCollection.Insert(0, await provider.GetDetail());
            if (itemCollection[0] is DYHDetail detail)
            {
                titleBar.ComboBoxSelectedIndex = detail.SelectedIndex;
                titleBar.ComboBoxVisibility = detail.ShowComboBox ? Visibility.Visible : Visibility.Collapsed;
            }

            List<FeedViewModel> feeds = await provider.GetFeeds(1);
            if (feeds != null)
                for (int i = 0; i < feeds.Count; i++)
                    itemCollection.Insert(i + 1, feeds[i]);
            titleBar.Title = provider.GetTitleBarText(itemCollection[0]);
            UIHelper.HideProgressBar();
        }
        private void TitleBar_BackButtonClick(object sender, RoutedEventArgs e) => Frame.GoBack();

        private void UserDetailBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe != e.OriginalSource) return;
                if (fe.Tag is string s)
                    if (s == (itemCollection[0] as UserDetail).BackgroundUrl)
                        UIHelper.ShowImage(s, ImageType.OriginImage);
                    else UIHelper.ShowImage(s, ImageType.SmallAvatar);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            switch (button.Tag as string)
            {
                case "follow":
                    UIHelper.Navigate(typeof(UserListPage), new object[] { provider.Id, true, titleBar.Title });
                    break;
                case "fans":
                    UIHelper.Navigate(typeof(UserListPage), new object[] { provider.Id, false, titleBar.Title });
                    break;
                case "FollowUser":
                    switch ((itemCollection[0] as UserDetail).FollowStatus)
                    {
                        case "关注":
                            await DataHelper.GetData(DataType.OperateFollow, provider.Id);
                            break;
                        case "取消关注":
                            await DataHelper.GetData(DataType.OperateUnfollow, provider.Id);
                            break;
                    }
                    itemCollection.RemoveAt(0);
                    itemCollection.Insert(0, await provider.GetDetail());
                    break;
                default:
                    UIHelper.OpenLink(button.Tag as string);
                    break;
            }
        }

        private void FeedTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(provider.Id)) return;
            ICanChangeSelectedIndex dataProvider = provider as ICanChangeSelectedIndex;
            dataProvider.SelectedIndex = (sender as ComboBox).SelectedIndex;
            dataProvider.Reset();
            if (itemCollection.Count > 1)
            {
                for (int i = itemCollection.Count - 1; i > 0; i--)
                    itemCollection.RemoveAt(i);
                Refresh();
            }
        }
    }
    class UserDetail
    {
        public string UserFaceUrl;
        public ImageSource UserFace;
        public string UserName;
        public double FollowNum;
        public double FansNum;
        public double FeedNum;
        public double Level;
        public string Bio;
        public string BackgroundUrl;
        public ImageBrush Background;
        public string Verify_title;
        public string Gender;
        public string City;
        public string Astro;
        public string Logintime;
        public string FollowStatus;
        public bool ShowFollowStatus { get => !string.IsNullOrEmpty(FollowStatus); }
        public bool Has_bio { get => !string.IsNullOrEmpty(Bio); }
        public bool Has_verify_title { get => !string.IsNullOrEmpty(Verify_title); }
        public bool Has_Astro { get => !string.IsNullOrEmpty(Astro); }
        public bool Has_City { get => !string.IsNullOrWhiteSpace(City) && !string.IsNullOrEmpty(City); }
        public bool Has_Gender { get => !string.IsNullOrEmpty(Gender); }
    }
    class TopicDetail
    {
        public ImageSource Logo { get; set; }
        public string Title { get; set; }
        public double FollowNum { get; set; }
        public double CommentNum { get; set; }
        public string Description { get; set; }
        public int SelectedIndex { get; set; }
    }
    class DYHDetail
    {
        public ImageSource Logo { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public double FollowNum { get; set; }
        public bool ShowUserButton { get; set; }
        public string Url { get; set; }
        public string UserName { get; set; }
        public ImageSource UserAvatar { get; set; }
        public int SelectedIndex { get; set; }
        public bool ShowComboBox { get; set; }
    }
    class TemplateSelector : DataTemplateSelector
    {
        public DataTemplate DataTemplate1 { get; set; }
        public DataTemplate DataTemplate2 { get; set; }
        public DataTemplate DataTemplate3 { get; set; }
        public DataTemplate DataTemplate4 { get; set; }
        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is UserDetail) return DataTemplate1;
            else if (item is TopicDetail) return DataTemplate3;
            else if (item is DYHDetail) return DataTemplate4;
            else return DataTemplate2;
        }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
    }
}
