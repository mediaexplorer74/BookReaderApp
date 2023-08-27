﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Data;

namespace BookViewerApp.BookFixed2ViewModels
{
    public class BookViewModel : INotifyPropertyChanged
    {
        public BookViewModel()
        {
            this.PropertyChanged += (s, e) =>
            {
                PageVisualAddCommand.OnCanExecuteChanged();
                PageVisualSetCommand.OnCanExecuteChanged();
                PageVisualMaxCommand.OnCanExecuteChanged();
            };
        }

        private Commands.ICommandEventRaiseable _PageVisualAddCommand;
        private Commands.ICommandEventRaiseable _PageVisualSetCommand;
        private Commands.ICommandEventRaiseable _PageVisualMaxCommand;
        private Commands.ICommandEventRaiseable _SwapReverseCommand;
        private Commands.ICommandEventRaiseable _AddCurrentPageToBookmarkCommand;

        private System.Windows.Input.ICommand _GoNextBookCommand;
        private System.Windows.Input.ICommand _GoPreviousBookCommand;

        public Commands.ICommandEventRaiseable PageVisualAddCommand
        { get {return _PageVisualAddCommand = _PageVisualAddCommand
                    ?? new Commands.PageVisualAddCommand(this); } }
        public Commands.ICommandEventRaiseable PageVisualSetCommand
        { get { return _PageVisualSetCommand = _PageVisualSetCommand 
                    ?? new Commands.PageVisualSetCommand(this); } }
        public Commands.ICommandEventRaiseable PageVisualMaxCommand
        { get { return _PageVisualMaxCommand = _PageVisualMaxCommand 
                    ?? new Commands.PageVisualMaxCommand(this); } }
        public Commands.ICommandEventRaiseable SwapReverseCommand
        { get { return _SwapReverseCommand = _SwapReverseCommand 
                    ?? new Commands.CommandBase((a)=> { return true; },
                    (a)=> { this.Reversed = !this.Reversed; }); } }
        public Commands.ICommandEventRaiseable AddCurrentPageToBookmarkCommand
        { get
            { return _AddCurrentPageToBookmarkCommand = _AddCurrentPageToBookmarkCommand 
                    ?? new Commands.AddCurrentPageToBookmark(this); } }
        public System.Windows.Input.ICommand GoNextBookCommand
        { get { return _GoNextBookCommand = _GoNextBookCommand 
                    ?? new Commands.AddNumberToSelectedBook(this, 1); } }
        public System.Windows.Input.ICommand GoPreviousBookCommand
        { get { return _GoPreviousBookCommand = _GoPreviousBookCommand 
                    ?? new Commands.AddNumberToSelectedBook(this, -1); } }

        public async void Initialize(Books.IBookFixed value, Control target=null)
        {
            if (BookInfo != null) SaveInfo();

            var pages = new ObservableCollection<PageViewModel>();
            var option = OptionCache = target == null ? OptionCache : new Books.PageOptionsControl(target);
            for (uint i = 0; i < value.PageCount; i++)
            {
                uint page = i;
                pages.Add(new PageViewModel(new Books.VirtualPage(() => 
                { var p = value.GetPage(page); p.Option = option; return p; })));
            }
            this._Reversed = false;
            this._PageSelected = 0;
            ID = value.ID;
            this.Pages = pages;
            BookInfo = await BookInfoStorage.GetBookInfoByIDOrCreateAsync(value.ID);
            this.PageSelected = (bool)SettingStorage.GetValue("SaveLastReadPage")
                ? (int)(BookInfo?.GetLastReadPage()?.Page ?? 1):1;
            this.Reversed = BookInfo?.PageReversed ?? false;
            OnPropertyChanged(nameof(Reversed));
            this.AsBookShelfBook = null;

            this.Bookmarks = new ObservableCollection<BookmarkViewModel>();
            foreach(var bm in BookInfo.Bookmarks)
            {
                this.Bookmarks.Add(new BookmarkViewModel(bm) );
            }
        }

        private Books.PageOptionsControl OptionCache;

        public BookShelfViewModels.BookViewModel AsBookShelfBook
        {
            get { return _AsBookShelfBook; }
            set { _AsBookShelfBook = value;OnPropertyChanged(nameof(AsBookShelfBook)); }
        }

        private BookShelfViewModels.BookViewModel _AsBookShelfBook;

        private BookInfoStorage.BookInfo BookInfo=null;

        public void SaveInfo()
        {
            if (BookInfo != null)
            {
                BookInfo.Bookmarks.Clear();
                BookInfo.SetLastReadPage((uint)this.PageSelected);
                foreach (var bm in this.Bookmarks)
                {
                    if (!bm.AutoGenerated)
                        BookInfo.Bookmarks.Add
                        (new BookInfoStorage.BookInfo.BookmarkItem()
                          {
                            Page = (uint)bm.Page,
                            Title = bm.Title,
                            Type = BookInfoStorage.BookInfo.BookmarkItem.BookmarkItemType.UserDefined
                          }
                        );
                }
                BookInfo.PageReversed = this.Reversed;
            }
        }

        public string ID { get { return _ID; } private set { _ID = value;OnPropertyChanged(nameof(ID)); } }
        private string _ID;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public int PageSelected
        {
            get { return _PageSelected+1; }
            set { if (value > PagesCount) return; _PageSelected = value-1;
                OnPropertyChanged(nameof(PageSelected));
                OnPropertyChanged(nameof(PageSelectedVisual)); OnPropertyChanged(nameof(ReadRate));
                OnPropertyChanged(nameof(CurrentBookmarkName)); }
        }
        private int _PageSelected = -1;

        public int PageSelectedVisual
        {
            get { return Reversed ? Pages.Count() - _PageSelected - 1 : _PageSelected; }
            set { _PageSelected = Reversed ? Pages.Count() - value - 1 : value;
                OnPropertyChanged(nameof(PageSelected)); OnPropertyChanged(nameof(PageSelectedVisual)); OnPropertyChanged(nameof(ReadRate)); OnPropertyChanged(nameof(CurrentBookmarkName)); }
        }

        public ObservableCollection<PageViewModel> Pages
        {
            get { return _Pages; }
            set { _Pages = value;OnPropertyChanged(nameof(Pages));
                OnPropertyChanged(nameof(PagesCount)); OnPropertyChanged(nameof(ReadRate)); }
        }
        private ObservableCollection<PageViewModel> _Pages = new ObservableCollection<PageViewModel>();

        public int PagesCount { get { return _Pages.Count(); } }

        public double ReadRate
        {
            get { return Math.Min((double)PageSelected / Pages.Count(),1.0); }
            set { PageSelected = (int)(value * Pages.Count);  OnPropertyChanged(nameof(ReadRate)); }
        }

        public string CurrentBookmarkName { get
            {
                foreach (var bm in this.Bookmarks)
                {
                    if (bm.Page == this.PageSelected && bm.AutoGenerated == false)
                    {
                        return bm.Title;
                    }
                }
                return "";
            }
            set
            {
                OnPropertyChanged(nameof(CurrentBookmarkName));
                OnPropertyChanged(nameof(Bookmarks));
                foreach (var bm in this.Bookmarks)
                {
                    if (bm.Page == this.PageSelected && bm.AutoGenerated == false)
                    {
                        if (value == "" || value == null)
                        {
                            this.Bookmarks.Remove(bm);
                            return;
                        }
                        else {
                            bm.Title = value;
                            return;
                        }
                    }
                }
                if (value == "" || value == null) return;
                this.Bookmarks.Add(new BookmarkViewModel() { AutoGenerated = false,
                    Page = this.PageSelected, Title = value });
                BookmarksSort();
                OnPropertyChanged(nameof(Bookmarks));
            }
        }

        public bool Reversed
        {
            get { return _Reversed; }
            set
            {
                if (Reversed != value)
                {
                    var oldPage = this.PageSelected;
                    _Reversed = value;
                    Pages = new ObservableCollection<PageViewModel>(Pages.Reverse());
                    PageSelected = oldPage;
                    OnPropertyChanged(nameof(Reversed));
                }
            }

        }
        private bool _Reversed = false;

        public ObservableCollection<BookmarkViewModel> Bookmarks
        {
            get { return _Bookmarks; }
            set { _Bookmarks = value; BookmarksSort(); OnPropertyChanged(nameof(Bookmarks)); OnPropertyChanged(nameof(CurrentBookmarkName));  }
        }
        private ObservableCollection<BookmarkViewModel> _Bookmarks = new ObservableCollection<BookmarkViewModel>();

        private void BookmarksSort()
        {
            _Bookmarks = new ObservableCollection<BookmarkViewModel>(_Bookmarks.ToList().OrderBy((a) => { return a.AutoGenerated ? -1 : a.Page; }));
        }
    }

    public class PageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public PageViewModel(Books.IPageFixed page) {
            this.Page = page;
        }

        private Books.IPageFixed Page;

        public ImageSource Source { get
            {
                if (_Source != null) return _Source;
                _Source = new BitmapImage();
                SetImageNoWait(_Source);
                //Page.Option.PropertyChanged += (s, e) => SetImageNoWait(_Source);
                return _Source;
            }
        }
        public BitmapImage _Source;

        public void UpdateSource()
        {
            SetImageNoWait(_Source);
        }

        private async void SetImageNoWait(BitmapImage im)
        {
            await Page.SetBitmapAsync(im);
        }
    }

    public class Commands
    {
        public interface ICommandEventRaiseable : System.Windows.Input.ICommand
        {
            void OnCanExecuteChanged();
        }

        public class PageVisualAddCommand : ICommandEventRaiseable
        {
            public event EventHandler CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

            private BookViewModel model;

            public PageVisualAddCommand(BookViewModel model) { this.model = model; }

            public bool CanExecute(object parameter)
            {
                return model.PagesCount > model.PageSelectedVisual + int.Parse(parameter as string) && model.PageSelectedVisual >= -int.Parse(parameter as string);
            }

            public void Execute(object parameter)
            {
                model.PageSelectedVisual += int.Parse(parameter as string);
            }
        }

        public class PageVisualSetCommand : ICommandEventRaiseable
        {
            public event EventHandler CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

            private BookViewModel model;

            public PageVisualSetCommand(BookViewModel model) { this.model = model; }

            public bool CanExecute(object parameter)
            {
                return model.PagesCount > int.Parse(parameter as string) && 0 <= int.Parse(parameter as string) && model.PageSelectedVisual != int.Parse(parameter as string);
            }

            public void Execute(object parameter)
            {
                model.PageSelectedVisual = int.Parse(parameter as string);
            }
        }

        public class PageVisualMaxCommand : ICommandEventRaiseable
        {
            public event EventHandler CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

            private BookViewModel model;

            public PageVisualMaxCommand(BookViewModel model) { this.model = model; }

            public bool CanExecute(object parameter)
            {
                return model.PagesCount > 0 && model.PageSelectedVisual != model.PagesCount - 1;
            }

            public void Execute(object parameter)
            {
                model.PageSelectedVisual = model.PagesCount - 1;
            }
        }

        public class AddNumberToSelectedBook : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

            private BookViewModel ViewModel;
            private int NumberToAdd;

            public AddNumberToSelectedBook(BookViewModel vm, int i)
            {
                vm.PropertyChanged += (s, e) => { OnCanExecuteChanged(); };
                ViewModel = vm;
                NumberToAdd = i;
            }

            public bool CanExecute(object parameter)
            {
                return GetAddedBook(ViewModel.ID) != null;
            }

            public async void Execute(object parameter)
            {
                var bookFVM = GetAddedBook(ViewModel.ID);
                var book = await bookFVM.TryGetBook();
                if (book is Books.IBookFixed)
                {
                    ViewModel.Initialize(book as Books.IBookFixed);
                    ViewModel.AsBookShelfBook = bookFVM;
                }
            }

            public BookShelfViewModels.BookViewModel GetAddedBook(string ID)
            {
                var books = ViewModel.AsBookShelfBook?.Parent?.Books;
                if (books == null) return null;
                //var book = books.Where(a => (a is BookShelfViewModels.BookViewModel && (a as BookShelfViewModels.BookViewModel).ID == ID)).First();
                var book = ViewModel.AsBookShelfBook;

                int cnt = 0;
                if (NumberToAdd == 0) return book as BookShelfViewModels.BookViewModel;
                if (this.NumberToAdd > 0)
                {
                    for (int i = books.IndexOf(book) + 1; i < books.Count; i++)
                    {
                        if (books[i] is BookShelfViewModels.BookViewModel) cnt++;
                        if (cnt == this.NumberToAdd) return books[i] as BookShelfViewModels.BookViewModel;
                    }
                }
                else
                {
                    for (int i = books.IndexOf(book) - 1; i >= 0; i--)
                    {
                        if (books[i] is BookShelfViewModels.BookViewModel) cnt--;
                        if (cnt == this.NumberToAdd) return books[i] as BookShelfViewModels.BookViewModel;
                    }
                }
                return null;
            }
        }

        public class AddCurrentPageToBookmark : ICommandEventRaiseable
        {
            public event EventHandler CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

            private BookViewModel model;

            public AddCurrentPageToBookmark(BookViewModel model) { this.model = model; model.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(model.PageSelected)) { OnCanExecuteChanged(); } }; }

            public bool CanExecute(object parameter)
            {
                foreach(var bm in model.Bookmarks)
                {
                    if(bm.Page==model.PageSelected && bm.AutoGenerated == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Execute(object parameter)
            {
                model.Bookmarks.Add(new BookmarkViewModel() { AutoGenerated = false, Page = model.PageSelected, Title = parameter is string ? parameter as string : "" });
            }
        }

        public class CommandBase : ICommandEventRaiseable
        {
            public event EventHandler CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

            private Func<object, bool> CanExecuteFunc;
            private Action<object> ExecuteAction;

            public CommandBase(Func<object, bool> CanExecuteFunc, Action<object> ExecuteAction)
            {
                this.CanExecuteFunc = CanExecuteFunc;
                this.ExecuteAction = ExecuteAction;
            }

            public bool CanExecute(object parameter)
            {
                return CanExecuteFunc(parameter);
            }

            public void Execute(object parameter)
            {
                ExecuteAction(parameter);
            }
        }
    }

    public class BookmarkViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name)); }

        public BookmarkViewModel() { }
        public BookmarkViewModel(BookInfoStorage.BookInfo.BookmarkItem item)
        {
            var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
            this.Page = (int)item.Page;
            this.Title = item.Title == "" || item.Title==null ? item.Type == BookInfoStorage.BookInfo.BookmarkItem.BookmarkItemType.UserDefined ? rl.GetString("BookmarkBasic/Title") : rl.GetString("BookmarkLastread/Title") : item.Title;
            this.AutoGenerated = item.Type == BookInfoStorage.BookInfo.BookmarkItem.BookmarkItemType.LastRead;
        }

        public int Page
        {
            get { return _Page; }
            set { _Page = value; OnPropertyChanged(nameof(Page)); }
        }
        private int _Page;

        public string Title
        {
            get { return _Title; }
            set { _Title = value;OnPropertyChanged(nameof(Title)); }
        }
        private string _Title;

        public bool AutoGenerated { get { return _AutoGenerated; } set { _AutoGenerated = value;OnPropertyChanged(nameof(_AutoGenerated)); } }
        private bool _AutoGenerated;
    }
}
