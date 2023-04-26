using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

public class Processes
{
    public static string processNameChrome = "chrome";
    public static string processNameChromeDriver = "chromedriver";

    //[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    //static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    //[DllImport("psapi.dll", CharSet = CharSet.Unicode)]
    //static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, uint nSize);

    public static async Task CheckRunningChromeAsync()
    {
        await Task.Run(() =>
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                try
                {
                    if (process.ProcessName.ToLower() == processNameChrome.ToLower())
                    {
                        if (process.MainWindowTitle == "Новая вкладка – Chromium")
                        {
                            process.Kill();
                            Console.WriteLine($"Процесс {processNameChrome} был закрыт форсированно");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при попытке закрыть процесс {processNameChrome}: {ex.Message}");
                }
            }
        });
    }

    public static async Task CheckRunningChromeDriverAsync()
    {
        await Task.Run(() =>
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                if (process.ProcessName.ToLower() == processNameChromeDriver.ToLower())
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception) { }
                }
            }
        });
    }



    //public static void CheckRunningChromeDriver()
    //{
    //    Process[] processes = Process.GetProcesses();
    //    foreach (Process process in processes)
    //    {
    //        try
    //        {
    //            if (process.ProcessName.ToLower() == processNameChromeDriver.ToLower())
    //            {
    //                var asdf = process;
    //                process.Kill();
    //                //Console.WriteLine($"Процесс {processNameChromeDriver} был закрыт форсированно");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Ошибка при попытке закрыть процесс {processNameChromeDriver}: {ex.Message}");
    //        }
    //    }
    //}
}




