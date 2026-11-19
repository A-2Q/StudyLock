using System.ComponentModel;
using System.Windows;

namespace StudyLock.Views;

public partial class BlackoutWindow : Window
{
    private bool _canClose;

    public BlackoutWindow()
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;
    }

    public void AllowClose()
    {
        _canClose = true;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_canClose)
        {
            e.Cancel = true;
        }
    }
}
