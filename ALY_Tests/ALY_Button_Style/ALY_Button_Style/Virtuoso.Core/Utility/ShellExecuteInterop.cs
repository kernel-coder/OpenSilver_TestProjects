#region Usings

using System;
using System.IO;
using System.Runtime.InteropServices;

#endregion

namespace Virtuoso.Core.Utility
{
    public class ShellExecuteInterop
    {
#if OPENSILVER
        private static void DownloadFile(byte[] data, string filename, bool download = true, string fileType = null, bool openInATab = false)
        {
            const string JS_DownloadFile = @"
                    document.FILE_Download = function(wasmArray) {
                        const dataPtr = Blazor.platform.getArrayEntryPtr(wasmArray, 0, 4);
                        const length = Blazor.platform.getArrayLength(wasmArray);
                        let data = new Uint8Array(Module.HEAP8.buffer, dataPtr, length * 4);
                        var blob;
                        if ($2) blob = new Blob([data], { type: $2});
                        else  blob = new Blob([data]);
                        let fileURL = URL.createObjectURL(blob);
                        if ($1) {
                            const link = document.createElement('a');
                            link.href = fileURL;
                            link.setAttribute('download', $0);
                            link.click();
                            link.remove();
                        }
                        if ($3) window.open(fileURL);
                        return 0;
                    }";
            OpenSilver.Interop.ExecuteJavaScript(JS_DownloadFile, filename, download, fileType, openInATab);
            DotNetForHtml5.Core.INTERNAL_Simulator.JavaScriptExecutionHandler.InvokeUnmarshalled<byte[], object>("document.FILE_Download", data);
        }
#else
        public enum ShowCommands
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }

        [DllImport("shell32.dll")]
        static extern IntPtr ShellExecute(
            IntPtr hwnd,
            string lpOperation,
            string lpFile,
            string lpParameters,
            string lpDirectory,
            ShowCommands nShowCmd);
#endif

        enum Operations
        {
            Open,
            Edit
        }

        public static void OpenCSV(byte[] csvByteStream, string fileName = "")
        {
            string file = string.Empty;
            if (string.IsNullOrEmpty(fileName))
            {
                file = string.Format("{0}.csv", Guid.NewGuid().ToString());
            }
            else
            {
                file = string.Format("{0}-{1}.csv", fileName, Guid.NewGuid().ToString());
            }

#if OPENSILVER
            DownloadFile(csvByteStream, file);
#else
            var _tempFolder = Client.Core.ApplicationStoreInfo.GetUserStoreForApplication(Constants.TEMP_FOLDER);
            string csvFile = Path.Combine(_tempFolder, file);
            File.WriteAllBytes(csvFile, csvByteStream);
            __shell_execute(csvFile, Operations.Open);
#endif
        }

        public static void OpenPDF(byte[] pdfByteStream, string fileName = "")
        {
            string file = string.Empty;
            if (string.IsNullOrEmpty(fileName))
            {
                file = string.Format("{0}.pdf", Guid.NewGuid().ToString());
            }
            else
            {
                file = string.Format("{0}-{1}.pdf", fileName, Guid.NewGuid().ToString());
            }
#if OPENSILVER
            DownloadFile(pdfByteStream, file, false, "application/pdf", true);
#else
            var _tempFolder = Client.Core.ApplicationStoreInfo.GetUserStoreForApplication(Constants.TEMP_FOLDER);
            var pdfFile = Path.Combine(_tempFolder, file);
            File.WriteAllBytes(pdfFile, pdfByteStream);
            __shell_execute(pdfFile, Operations.Open);
#endif
        }

        public static void OpenFile(byte[] byteStream, string fileName = "")
        {
#if OPENSILVER
            DownloadFile(byteStream, Guid.NewGuid() + "-" + fileName);
#else
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var _tempFolder = Client.Core.ApplicationStoreInfo.GetUserStoreForApplication(Constants.TEMP_FOLDER);
            var openFile = Path.Combine(_tempFolder, (Guid.NewGuid() + "-" + fileName));
            File.WriteAllBytes(openFile, byteStream);
            __shell_execute(openFile, Operations.Open);
#endif
        }

        public static void OpenUsingDefault(string uriOrPath)
        {
            __shell_execute(uriOrPath, Operations.Open);
        }

        public static void EditUsingDefault(string uriOrPath)
        {
            __shell_execute(uriOrPath, Operations.Edit);
        }

        private static void __shell_execute(string uriOrPath, Operations operation)
        {
#if !OPENSILVER
            switch (operation)
            {
                case Operations.Open:
                    ShellExecute(IntPtr.Zero, "open", uriOrPath, "", "", ShowCommands.SW_SHOWNOACTIVATE);
                    break;
                case Operations.Edit:
                    ShellExecute(IntPtr.Zero, "edit", uriOrPath, "", "", ShowCommands.SW_SHOWNOACTIVATE);
                    break;
            }
#endif
        }
    }
}