using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using StudyLock.Models;

namespace StudyLock.Views;

public partial class SessionNotesViewerDialog : Window
{
    private readonly StudyRecord _record;

    public SessionNotesViewerDialog(StudyRecord record)
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;
        _record = record;

        SessionTitleText.Text = string.IsNullOrWhiteSpace(record.SiteName) ? record.SiteUrl : record.SiteName;
        SessionMetaText.Text = $"{record.LocalDateDisplay}  •  {record.ActualDurationDisplay}";
        NotesViewerBox.Text = record.Notes;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_record.Notes);
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new()
        {
            Filter = "Text Files (*.txt)|*.txt",
            FileName = $"StudyLock_Notes_{_record.StartTimeUtc.ToLocalTime():yyyyMMdd_HHmm}.txt"
        };

        if (dialog.ShowDialog(this) == true)
        {
            string header = $"StudyLock Session Notes\nSite: {_record.SiteName} ({_record.SiteUrl})\nDate: {_record.LocalDateDisplay}\nDuration: {_record.ActualDurationDisplay}\n----------------------------------------\n\n";
            File.WriteAllText(dialog.FileName, header + _record.Notes, Encoding.UTF8);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
