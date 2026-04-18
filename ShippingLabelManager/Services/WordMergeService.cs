using System.Diagnostics;

namespace ShippingLabelManager.Services;

public static class WordMergeService
{
    public static void ExportAndPrint(string csvPath, string templatePath, string printerName)
    {
        KillWord();
        Thread.Sleep(800);

        var scriptPath  = Path.Combine(Path.GetTempPath(), "slm_merge.vbs");
        var printerLine = string.IsNullOrEmpty(printerName)
            ? ""
            : "oWord.ActivePrinter = \"" + EscapeVbs(printerName) + "\"" + "\r\n";

        var script =
            "On Error Resume Next" + "\r\n" +
            "Dim oWord, oDoc" + "\r\n" +
            "Set oWord = CreateObject(\"Word.Application\")" + "\r\n" +
            "If Err.Number <> 0 Then WScript.Echo \"Failed to start Word: \" & Err.Description : WScript.Quit" + "\r\n" +
            "oWord.Visible = False" + "\r\n" +
            "oWord.DisplayAlerts = 0" + "\r\n" +
            "oWord.AutomationSecurity = 3" + "\r\n" +
            "Set oDoc = oWord.Documents.Open(\"" + EscapeVbs(templatePath) + "\")" + "\r\n" +
            "If Err.Number <> 0 Then WScript.Echo \"Failed to open template: \" & Err.Description : oWord.Quit False : WScript.Quit" + "\r\n" +
            // 3-argument form: Name, ConfirmConversions, ReadOnly — safest for VBScript
            "oDoc.MailMerge.OpenDataSource \"" + EscapeVbs(csvPath) + "\", False, True" + "\r\n" +
            "If Err.Number <> 0 Then WScript.Echo \"Failed to open data source: \" & Err.Description : oDoc.Close False : oWord.Quit False : WScript.Quit" + "\r\n" +
            printerLine +
            "oDoc.MailMerge.Destination = 0" + "\r\n" +
            "oDoc.MailMerge.SuppressBlankLines = True" + "\r\n" +
            "oDoc.MailMerge.DataSource.FirstRecord = 1" + "\r\n" +
            "oDoc.MailMerge.Execute False" + "\r\n" +
            "If Err.Number <> 0 Then WScript.Echo \"Merge failed: \" & Err.Description : oDoc.Close False : oWord.Quit False : WScript.Quit" + "\r\n" +
            "oDoc.Close False" + "\r\n" +
            "Set oDoc = Nothing" + "\r\n" +
            "WScript.Sleep 600" + "\r\n" +
            "oWord.DisplayAlerts = 1" + "\r\n" +
            "oWord.Visible = True" + "\r\n" +
            "oWord.WindowState = 1" + "\r\n" +
            "oWord.Activate" + "\r\n" +
            "WScript.Sleep 400" + "\r\n" +
            "oWord.ActiveDocument.PrintPreview" + "\r\n";

        RunVbs(scriptPath, script);
    }

    public static void OpenInWord(string csvPath, string templatePath)
    {
        KillWord();
        Thread.Sleep(800);

        var scriptPath = Path.Combine(Path.GetTempPath(), "slm_open.vbs");
        var nl         = Environment.NewLine;

        var script =
            "On Error Resume Next"                                                                         + nl +
            "Dim oWord, oDoc, oMerged, oAddIn"                                                             + nl +
            "Set oWord = CreateObject(\"Word.Application\")"                                               + nl +
            "If Err.Number <> 0 Then"                                                                      + nl +
            "  WScript.Echo \"Failed to start Word: \" & Err.Description"                                  + nl +
            "  WScript.Quit"                                                                               + nl +
            "End If"                                                                                       + nl +
            "oWord.Visible = False"                                                                        + nl +
            "oWord.DisplayAlerts = 0"                                                                      + nl +
            "oWord.AutomationSecurity = 3"                                                                 + nl +
            "For Each oAddIn In oWord.COMAddIns"                                                           + nl +
            "  If InStr(LCase(oAddIn.Description), \"tbarcode\") > 0 Then"                                 + nl +
            "    oAddIn.Connect = False"                                                                    + nl +
            "  End If"                                                                                     + nl +
            "Next"                                                                                         + nl +
            "Set oDoc = oWord.Documents.Open(\"" + EscapeVbs(templatePath) + "\", False, True)"           + nl +
            "If Err.Number <> 0 Then"                                                                      + nl +
            "  WScript.Echo \"Failed to open template: \" & Err.Description"                               + nl +
            "  oWord.Quit False"                                                                           + nl +
            "  WScript.Quit"                                                                               + nl +
            "End If"                                                                                       + nl +
            "oDoc.MailMerge.OpenDataSource \"" + EscapeVbs(csvPath) + "\", False, True"                   + nl +
            "If Err.Number <> 0 Then"                                                                      + nl +
            "  WScript.Echo \"Failed to link data: \" & Err.Description"                                   + nl +
            "  oDoc.Close False"                                                                           + nl +
            "  oWord.Quit False"                                                                           + nl +
            "  WScript.Quit"                                                                               + nl +
            "End If"                                                                                       + nl +
            "oDoc.MailMerge.Destination = 0"                                                               + nl +
            "oDoc.MailMerge.SuppressBlankLines = True"                                                     + nl +
            "oDoc.MailMerge.DataSource.FirstRecord = 1"                                                    + nl +
            "oDoc.MailMerge.Execute False"                                                                 + nl +
            "If Err.Number <> 0 Then"                                                                      + nl +
            "  WScript.Echo \"Merge failed: \" & Err.Description"                                          + nl +
            "  oDoc.Close False"                                                                           + nl +
            "  oWord.Quit False"                                                                           + nl +
            "  WScript.Quit"                                                                               + nl +
            "End If"                                                                                       + nl +
            "WScript.Sleep 1000"                                                                           + nl +
            "Set oMerged = oWord.ActiveDocument"                                                           + nl +
            "oDoc.Close False"                                                                             + nl +
            "Set oDoc = Nothing"                                                                           + nl +
            "oMerged.Fields.Update"                                                                        + nl +
            "On Error Resume Next"                                                                         + nl +
            "oWord.ActiveWindow.View.ShowFieldCodes = False"                                               + nl +
            "oMerged.Fields.Update"                                                                        + nl +
            "On Error Resume Next"                                                                         + nl +
            "For Each oAddIn In oWord.COMAddIns"                                                           + nl +
            "  If InStr(LCase(oAddIn.Description), \"tbarcode\") > 0 Then"                                 + nl +
            "    oAddIn.Connect = True"                                                                     + nl +
            "  End If"                                                                                     + nl +
            "Next"                                                                                         + nl +
            "On Error Resume Next"                                                                         + nl +
            "oMerged.Protect 3, True"                                                                      + nl +
            "On Error Resume Next"                                                                         + nl +
            "oWord.Visible = True"                                                                         + nl +
            "oWord.WindowState = 1"                                                                        + nl +
            "oMerged.Activate"                                                                             + nl +
            "AppActivate oWord.Caption"                                                                    + nl +
            "WScript.Sleep 400"                                                                            + nl +
            "oWord.Options.PrintBackground = False"                                                        + nl +
            "Dim oDlg"                                                                                     + nl +
            "Set oDlg = oWord.Dialogs(88)"                                                                 + nl +
            "Dim dlgResult"                                                                                + nl +
            "dlgResult = oDlg.Show"                                                                        + nl +
            "WScript.Sleep 5000"                                                                           + nl +
            "On Error Resume Next"                                                                         + nl +
            "oWord.Options.PrintBackground = True"                                                         + nl +
            "oMerged.Close False"                                                                          + nl +
            "oWord.Quit False"                                                                             + nl +
            "Set oDlg    = Nothing"                                                                        + nl +
            "Set oMerged = Nothing"                                                                        + nl +
            "Set oWord   = Nothing"                                                                        + nl;

        RunVbs(scriptPath, script);
    }

    private static void KillWord()
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("WINWORD"))
            {
                try { proc.Kill(); proc.WaitForExit(3000); } catch { }
            }
        }
        catch { }
    }

    private static void RunVbs(string scriptPath, string script)
    {
        File.WriteAllText(scriptPath, script);
        Process.Start(new ProcessStartInfo
        {
            FileName        = "wscript.exe",
            Arguments       = "\"" + scriptPath + "\"",
            UseShellExecute = true,
            WindowStyle     = ProcessWindowStyle.Hidden
        });
    }

    private static string EscapeVbs(string path) => path.Replace("\"", "\"\"");
}
