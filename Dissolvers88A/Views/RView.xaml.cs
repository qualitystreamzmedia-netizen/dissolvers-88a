using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Dissolvers88A.ViewModels;

namespace Dissolvers88A.Views;

public partial class RView : UserControl
{
    public RViewModel ViewModel { get; } = new();

    public RView()
    {
        InitializeComponent();
        DataContext = ViewModel;

        var lists = Enumerable.Range(1, 6).Select(i => "L" + i).ToArray();
        PullTarget.ItemsSource = lists;
        PullTarget.SelectedIndex = 0;

        if (!ViewModel.IsAvailable)
            Unavailable.Visibility = Visibility.Visible;

        ViewModel.Console.CollectionChanged += Log_Changed;
    }

    /// <summary>Called by MainWindow when the R tab becomes visible.</summary>
    public void OnShown()
    {
        ViewModel.Activate();
        if (ViewModel.Ready) Input.Focus();
    }

    public void OnClosing() => ViewModel.Shutdown();

    private void Log_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            Dispatcher.BeginInvoke(new Action(() => LogScroll.ScrollToEnd()));
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Return && ViewModel.RunCommand.CanExecute(null))
        {
            ViewModel.RunCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Cran_Navigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
