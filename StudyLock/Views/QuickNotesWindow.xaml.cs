using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StudyLock.Views;

public partial class QuickNotesWindow : Window
{
    public event Action<string>? NotesChanged;

    public QuickNotesWindow(string initialNotes)
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;
        NotesTextBox.Text = initialNotes;
    }

    public string CurrentNotes => NotesTextBox.Text;

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void NotesTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        NotesChanged?.Invoke(NotesTextBox.Text ?? string.Empty);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
