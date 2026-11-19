using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StudyLock.Views;

public partial class CalculatorWindow : Window
{
    private double _accumulator;
    private string? _pendingOperator;
    private bool _startNewEntry = true;

    public CalculatorWindow()
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void Digit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string digit)
        {
            AppendDigit(digit);
        }
    }

    private void AppendDigit(string digit)
    {
        if (_startNewEntry || MainDisplay.Text == "0" || MainDisplay.Text == "Error")
        {
            MainDisplay.Text = digit;
            _startNewEntry = false;
        }
        else if (MainDisplay.Text.Length < 16)
        {
            MainDisplay.Text += digit;
        }
    }

    private void Decimal_Click(object sender, RoutedEventArgs e)
    {
        if (_startNewEntry || MainDisplay.Text == "Error")
        {
            MainDisplay.Text = "0.";
            _startNewEntry = false;
        }
        else if (!MainDisplay.Text.Contains('.'))
        {
            MainDisplay.Text += ".";
        }
    }

    private void Operator_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string op)
        {
            ApplyOperator(op);
        }
    }

    private void ApplyOperator(string op)
    {
        if (!double.TryParse(MainDisplay.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double current))
        {
            return;
        }

        if (_pendingOperator is not null && !_startNewEntry)
        {
            if (!EvaluatePending(current))
            {
                return;
            }
        }
        else
        {
            _accumulator = current;
        }

        _pendingOperator = op;
        _startNewEntry = true;
        ExpressionDisplay.Text = $"{FormatNumber(_accumulator)} {DisplaySymbol(op)}";
    }

    private void Equals_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingOperator is null)
        {
            return;
        }

        if (!double.TryParse(MainDisplay.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double current))
        {
            return;
        }

        string prevExpr = $"{FormatNumber(_accumulator)} {DisplaySymbol(_pendingOperator)} {FormatNumber(current)} =";
        if (EvaluatePending(current))
        {
            ExpressionDisplay.Text = prevExpr;
            _pendingOperator = null;
            _startNewEntry = true;
        }
    }

    private bool EvaluatePending(double rhs)
    {
        double result;
        switch (_pendingOperator)
        {
            case "+":
                result = _accumulator + rhs;
                break;
            case "-":
                result = _accumulator - rhs;
                break;
            case "*":
                result = _accumulator * rhs;
                break;
            case "/":
                if (Math.Abs(rhs) < 1e-15)
                {
                    MainDisplay.Text = "Error";
                    ExpressionDisplay.Text = string.Empty;
                    _pendingOperator = null;
                    _startNewEntry = true;
                    return false;
                }
                result = _accumulator / rhs;
                break;
            default:
                result = rhs;
                break;
        }

        _accumulator = result;
        MainDisplay.Text = FormatNumber(result);
        return true;
    }

    private void Sqrt_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(MainDisplay.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double current))
        {
            return;
        }

        if (current < 0)
        {
            MainDisplay.Text = "Error";
            _startNewEntry = true;
            return;
        }

        double res = Math.Sqrt(current);
        ExpressionDisplay.Text = $"√({FormatNumber(current)})";
        MainDisplay.Text = FormatNumber(res);
        _startNewEntry = true;
    }

    private void Negate_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(MainDisplay.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double current) && current != 0)
        {
            MainDisplay.Text = FormatNumber(-current);
        }
    }

    private void Backspace_Click(object sender, RoutedEventArgs e)
    {
        if (_startNewEntry || MainDisplay.Text == "Error")
        {
            return;
        }

        if (MainDisplay.Text.Length > 1)
        {
            MainDisplay.Text = MainDisplay.Text[..^1];
        }
        else
        {
            MainDisplay.Text = "0";
            _startNewEntry = true;
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _accumulator = 0;
        _pendingOperator = null;
        _startNewEntry = true;
        MainDisplay.Text = "0";
        ExpressionDisplay.Text = string.Empty;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case >= Key.D0 and <= Key.D9 when Keyboard.Modifiers == ModifierKeys.None:
                AppendDigit((e.Key - Key.D0).ToString(CultureInfo.InvariantCulture));
                e.Handled = true;
                break;
            case >= Key.NumPad0 and <= Key.NumPad9:
                AppendDigit((e.Key - Key.NumPad0).ToString(CultureInfo.InvariantCulture));
                e.Handled = true;
                break;
            case Key.OemPeriod or Key.Decimal:
                Decimal_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.Add or Key.OemPlus when Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || e.Key == Key.Add:
                ApplyOperator("+");
                e.Handled = true;
                break;
            case Key.Subtract or Key.OemMinus:
                ApplyOperator("-");
                e.Handled = true;
                break;
            case Key.Multiply:
                ApplyOperator("*");
                e.Handled = true;
                break;
            case Key.Divide or Key.OemQuestion:
                ApplyOperator("/");
                e.Handled = true;
                break;
            case Key.Enter:
                Equals_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.Back:
                Backspace_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.Escape:
                Clear_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

    private static string FormatNumber(double value) =>
        Math.Round(value, 10).ToString("G12", CultureInfo.InvariantCulture);

    private static string DisplaySymbol(string op) => op switch
    {
        "+" => "+",
        "-" => "−",
        "*" => "×",
        "/" => "÷",
        _ => op
    };
}
