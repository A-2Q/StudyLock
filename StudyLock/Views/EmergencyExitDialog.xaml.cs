using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StudyLock.Services;

namespace StudyLock.Views;

public partial class EmergencyExitDialog : Window
{
    private readonly string _expectedCode;

    public bool EmergencyConfirmed { get; private set; }

    public EmergencyExitDialog()
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;

        _expectedCode = EmergencyCodeService.GenerateCode();
        TargetCodeText.Text = _expectedCode;

        // Hard-block paste via DataObject handler & CommandManager
        DataObject.AddPastingHandler(CodeInputBox, OnPasteAttempt);
        CommandManager.AddPreviewExecutedHandler(CodeInputBox, OnPreviewExecuted);

        Loaded += (_, _) => CodeInputBox.Focus();
    }

    private void OnPasteAttempt(object sender, DataObjectPastingEventArgs e)
    {
        e.CancelCommand();
        ShowPasteWarning();
    }

    private void OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Paste ||
            e.Command == ApplicationCommands.Copy ||
            e.Command == ApplicationCommands.Cut)
        {
            e.Handled = true;
            if (e.Command == ApplicationCommands.Paste)
            {
                ShowPasteWarning();
            }
        }
    }

    private void CodeInputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.V) ||
            (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && e.Key == Key.Insert))
        {
            e.Handled = true;
            ShowPasteWarning();
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    private void ShowPasteWarning()
    {
        PasteWarningText.Visibility = Visibility.Visible;
    }

    private void CodeInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string typed = CodeInputBox.Text ?? string.Empty;
        int matchedPrefix = 0;
        int limit = Math.Min(typed.Length, _expectedCode.Length);
        for (int i = 0; i < limit; i++)
        {
            if (typed[i] == _expectedCode[i])
            {
                matchedPrefix++;
            }
            else
            {
                break;
            }
        }

        bool prefixValid = matchedPrefix == typed.Length;
        bool exactMatch = EmergencyCodeService.IsExactMatch(_expectedCode, typed);

        ProgressText.Text = $"{matchedPrefix} / {_expectedCode.Length}";
        if (exactMatch)
        {
            ProgressText.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush");
        }
        else if (!prefixValid)
        {
            ProgressText.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
        }
        else
        {
            ProgressText.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        }

        ConfirmButton.IsEnabled = exactMatch;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (EmergencyCodeService.IsExactMatch(_expectedCode, CodeInputBox.Text))
        {
            EmergencyConfirmed = true;
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        EmergencyConfirmed = false;
        DialogResult = false;
        Close();
    }
}
