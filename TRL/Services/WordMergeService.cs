using System.Diagnostics;

namespace TRL.Services;

public static class WordMergeService
{
    public static void SaveMergedDocument(
        string csvPath,
        string templatePath,
        string outputDocxPath,
        string siteName = "",
        int startSerial = 0,
        int endSerial = 0,
        bool forceCloseExistingWord = false)
    {
        if (forceCloseExistingWord)
        {
            KillWord();
            Thread.Sleep(1000);
        }

        var outputFolder = Path.GetDirectoryName(outputDocxPath) 
            ?? throw new ArgumentException("Invalid output path: " + outputDocxPath);

        Directory.CreateDirectory(outputFolder);
        if (File.Exists(outputDocxPath))
            File.Delete(outputDocxPath);

        EnsureSchemaIniForCsv(csvPath);

        var scriptPath = Path.Combine(Path.GetTempPath(), "trl_save_merge.vbs");
        var nl = Environment.NewLine;
        var csvFolder = Path.GetDirectoryName(csvPath) ?? "";
        var csvFile = Path.GetFileName(csvPath);



var script =
    "On Error Resume Next" + nl +
    "Dim oWord, oDoc, oMerged, originalDocName" + nl +
    "Dim csvPath, csvFolder, csvFile" + nl +
    "csvPath = \"" + EscapeVbs(csvPath) + "\"" + nl +
    "csvFolder = \"" + EscapeVbs(csvFolder) + "\"" + nl +
    "csvFile = \"" + EscapeVbs(csvFile) + "\"" + nl +

    "Set oWord = CreateObject(\"Word.Application\")" + nl +
    "oWord.Visible = True" + nl +
    "oWord.DisplayAlerts = 0" + nl +
    "oWord.ScreenUpdating = True" + nl +
    "oWord.AutomationSecurity = 3" + nl +

    "WScript.Echo \"Opening template...\"" + nl +
    "Set oDoc = oWord.Documents.Open(\"" + EscapeVbs(templatePath) + "\", False, True)" + nl +
    "originalDocName = oDoc.Name" + nl +
    "WScript.Echo \"Template opened: \" & originalDocName" + nl +

    "WScript.Echo \"Opening data source...\"" + nl +
    "oDoc.MailMerge.OpenDataSource csvPath, False, True, False, False, , , , , , , " +
    "\"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=\" & csvFolder & \";Extended Properties=\"\"text;HDR=YES;FMT=Delimited;CharacterSet=65001\"\";\", " +
    "\"SELECT * FROM [\" & csvFile & \"]\", , False, 5" + nl +

    "Dim recordCount" + nl +
    "recordCount = oDoc.MailMerge.DataSource.RecordCount" + nl +
    "WScript.Echo \"Record count: \" & recordCount" + nl +

    "If recordCount = 0 Then" + nl +
    "   WScript.Echo \"ERROR: No records found in CSV\"" + nl +
    "Else" + nl +
    "   WScript.Echo \"Executing mail merge with \" & recordCount & \" records...\"" + nl +
    "   oDoc.MailMerge.Destination = 0" + nl +
    "   oDoc.MailMerge.SuppressBlankLines = True" + nl +
    "   oDoc.MailMerge.DataSource.FirstRecord = 1" + nl +
    "   oDoc.MailMerge.DataSource.LastRecord = recordCount" + nl +
    "   oDoc.MailMerge.Execute False" + nl +
    "   WScript.Echo \"Mail merge executed\"" + nl +

    "   ' Wait a moment for the merge to complete" + nl +
    "   WScript.Sleep 2000" + nl +

    "   ' Find the merged document (it will be a different document than the template)" + nl +
    "   Set oMerged = Nothing" + nl +
    "   For i = 1 To oWord.Documents.Count" + nl +
    "       If oWord.Documents(i).Name <> originalDocName Then" + nl +
    "           Set oMerged = oWord.Documents(i)" + nl +
    "           WScript.Echo \"Found merged document: \" & oMerged.Name" + nl +
    "           Exit For" + nl +
    "       End If" + nl +
    "   Next" + nl +
    "End If" + nl +

    " ' Close the original template document" + nl +
    "If Not oDoc Is Nothing Then" + nl +
    "   oDoc.Close False" + nl +
    "   WScript.Echo \"Closed template document\"" + nl +
    "End If" + nl +
    "Set oDoc = Nothing" + nl +

    "If oMerged Is Nothing Then" + nl +
    "   WScript.Echo \"ERROR: No merged document found\"" + nl +
    "Else" + nl +
    "   WScript.Echo \"Saving merged document to: \" & \"" + EscapeVbs(outputDocxPath) + "\"" + nl +
    "   oMerged.SaveAs2 \"" + EscapeVbs(outputDocxPath) + "\", 16" + nl +
    "   If Err.Number <> 0 Then" + nl +
    "      WScript.Echo \"SaveAs2 failed: \" & Err.Number & \" - \" & Err.Description" + nl +
    "      Err.Clear" + nl +
    "      oMerged.SaveAs \"" + EscapeVbs(outputDocxPath) + "\", 16" + nl +
    "      If Err.Number <> 0 Then" + nl +
    "         WScript.Echo \"SaveAs also failed: \" & Err.Number & \" - \" & Err.Description" + nl +
    "      Else" + nl +
    "         WScript.Echo \"SAVE_COMPLETE\"" + nl +
    "      End If" + nl +
    "   Else" + nl +
    "      WScript.Echo \"SAVE_COMPLETE\"" + nl +
    "   End If" + nl +
    "   " + nl +
    "   ' === ADD FILE METADATA === " + nl +
    "   On Error Resume Next" + nl +
    "   WScript.Echo \"Adding document properties...\"" + nl +
    "   With oMerged.BuiltInDocumentProperties" + nl +
    "       .Item(\"Title\").Value = \"" + EscapeVbs($"{siteName} Labels") + "\"" + nl +
    "       .Item(\"Subject\").Value = \"NLS Return Labels\"" + nl +
    "       .Item(\"Author\").Value = \"muyepaa@gmail.com\"" + nl +
    "       .Item(\"Company\").Value = \"AnthonyMuyepa\"" + nl +
    "       .Item(\"Comments\").Value = \"Serial range: " + startSerial + " to " + endSerial + "\"" + nl +
    "   End With" + nl +
    "   WScript.Echo \"Document properties added\"" + nl +
    "   " + nl +
    "   ' === MEMORY FIX: Close the merged document === " + nl +
    "   oMerged.Close True" + nl +
    "   WScript.Echo \"Closed merged document to free memory\"" + nl +
    "   " + nl +
    "   ' === MEMORY FIX: Close Word application === " + nl +
    "   oWord.Quit False" + nl +
    "   WScript.Echo \"Word application closed to free memory\"" + nl +
    "   Set oWord = Nothing" + nl +
    "   " + nl +
    "   ' Give system time to release resources before next batch" + nl +
    "   WScript.Sleep 1000" + nl +
    "End If" + nl;

var result = RunVbsAndWait(scriptPath, script, AppSettings.WordMergeTimeoutMinutes * 60 * 1000);

 

        // Check for errors in the result
        if (result.Contains("ERROR") || !result.Contains("SAVE_COMPLETE"))
        {
            throw new Exception($"Word merge/save failed.\n\nError details:\n{result}");
        }

        // Extra safety check
        if (!File.Exists(outputDocxPath))
            throw new Exception("Document not saved: " + outputDocxPath);
    }
    // ==================== Helpers ====================

    private static string RunVbsAndWait(string scriptPath, string script, int timeoutMilliseconds = 20 * 60 * 1000)
    {
        File.WriteAllText(scriptPath, script);

        using var proc = new Process();
        proc.StartInfo = new ProcessStartInfo
        {
            FileName = "cscript.exe",
            Arguments = "//nologo \"" + scriptPath + "\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        proc.Start();

        var outputTask = proc.StandardOutput.ReadToEndAsync();
        var errorTask = proc.StandardError.ReadToEndAsync();

        var exited = proc.WaitForExit(timeoutMilliseconds);
        if (!exited)
        {
            try { proc.Kill(true); } catch { }
            throw new TimeoutException("VBScript timed out.");
        }

        Task.WaitAll(outputTask, errorTask);
        var output = outputTask.Result + Environment.NewLine + errorTask.Result;

        try { File.Delete(scriptPath); } catch { }

        return output.Trim();
    }

    private static void KillWord()
    {
        // Only kill if explicitly requested - otherwise do nothing
        try
        {
            foreach (var p in Process.GetProcessesByName("WINWORD"))
            {
                try { p.Kill(); } catch { }
            }
        }
        catch { }
    }

    private static string EscapeVbs(string value) => value.Replace("\"", "\"\"");

    private static void EnsureSchemaIniForCsv(string csvPath)
    {
        var folder = Path.GetDirectoryName(csvPath);
        var fileName = Path.GetFileName(csvPath);
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(fileName)) return;

        var schemaPath = Path.Combine(folder, "schema.ini");
        var schema = 
            $"[{fileName}]" + Environment.NewLine +
            "Format=CSVDelimited" + Environment.NewLine +
            "ColNameHeader=True" + Environment.NewLine +
            "CharacterSet=65001" + Environment.NewLine +
            "MaxScanRows=0" + Environment.NewLine;
        File.WriteAllText(schemaPath, schema);
    }
}