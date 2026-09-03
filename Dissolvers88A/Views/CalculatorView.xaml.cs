using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dissolvers88A.ViewModels;

namespace Dissolvers88A.Views;

public partial class CalculatorView : UserControl
{
    public CalculatorViewModel ViewModel { get; } = new();

    private static readonly Dictionary<string, string> Secondary = new()
    {
        ["sin("] = "asin(",
        ["cos("] = "acos(",
        ["tan("] = "atan(",
        ["sqrt("] = "cbrt(",
        ["^"] = "nPr(",
        ["^2"] = "^3",
        ["^-1"] = "abs(",
        ["!"] = "nCr(",
        ["ln("] = "e^(",
        ["log("] = "10^(",
    };

    public CalculatorView()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += (_, _) => FocusInput();
    }

    public void FocusInput()
    {
        InputBox.Focus();
        InputBox.CaretIndex = InputBox.Text.Length;
    }

    // ---- keypad --------------------------------------------------------

    private void Second_Click(object sender, RoutedEventArgs e) { /* state read from SecondKey.IsChecked */ }

    private void KeyClick(object sender, RoutedEventArgs e)
    {
        var tag = (string)((Control)sender).Tag;
        bool second = SecondKey.IsChecked == true;
        if (second && sender is Button) SecondKey.IsChecked = false;

        switch (tag)
        {
            case "EVAL": Evaluate(); return;
            case "DEL": DeleteAtCaret(); return;
            case "AC": ViewModel.ClearAll(); InputBox.Clear(); FocusInput(); return;
            case "NEG": InsertText("-"); return;
        }

        string insert = second && Secondary.TryGetValue(tag, out var alt) ? alt : tag;
        InsertText(insert);
    }

    private void InsertText(string text)
    {
        int start = InputBox.SelectionStart;
        int len = InputBox.SelectionLength;
        InputBox.Text = InputBox.Text.Remove(start, len).Insert(start, text);
        InputBox.CaretIndex = start + text.Length;
        InputBox.Focus();
    }

    private void DeleteAtCaret()
    {
        if (InputBox.SelectionLength > 0)
        {
            int s = InputBox.SelectionStart;
            InputBox.Text = InputBox.Text.Remove(s, InputBox.SelectionLength);
            InputBox.CaretIndex = s;
        }
        else if (InputBox.CaretIndex > 0)
        {
            int c = InputBox.CaretIndex;
            InputBox.Text = InputBox.Text.Remove(c - 1, 1);
            InputBox.CaretIndex = c - 1;
        }
        InputBox.Focus();
    }

    private void Evaluate()
    {
        var r = ViewModel.Commit(InputBox.Text);
        if (r.Ok) InputBox.Clear();
        ScrollHistoryToEnd();
        FocusInput();
    }

    // ---- input box ----------------------------------------------------

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        => ViewModel.UpdatePreview(InputBox.Text);

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Return)
        {
            Evaluate();
            e.Handled = true;
        }
    }

    // ---- history recall ---------------------------------------------

    private void History_ExprClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is HistoryEntry h)
        {
            InputBox.Text = h.Expression;
            InputBox.CaretIndex = h.Expression.Length;
            FocusInput();
        }
    }

    private void History_ResultClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is HistoryEntry { IsError: false } h)
            InsertText(h.Result);
    }

    private void ScrollHistoryToEnd()
    {
        HistoryScroll.UpdateLayout();
        HistoryScroll.ScrollToBottom();
    }
}
