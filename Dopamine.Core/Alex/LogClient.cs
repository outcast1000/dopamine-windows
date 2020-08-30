using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Core.Alex
{
    public static class LogClientA
    {
        //public static LogClient Instance { get; }

        public static void Error(string message, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null, object arg5 = null, object arg6 = null, object arg7 = null, object arg8 = null)//, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log("Error", message, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);//, sourceFilePath, memberName, lineNumber);
        }
        public static string GetAllExceptions(Exception ex) { return String.Format("Exception: {0}", ex.Message); }
        public static void Info(string message, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null, object arg5 = null, object arg6 = null, object arg7 = null, object arg8 = null)//, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log("Info", message, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);//, sourceFilePath, memberName, lineNumber);
        }
        public static void Initialize(int archiveAboveSize, int maxArchiveFiles) { }
        public static string Logfile() { return "Console"; }
        public static void Warning(string message, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null, object arg5 = null, object arg6 = null, object arg7 = null, object arg8 = null)//, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log("Warning", message, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);//, sourceFilePath, memberName, lineNumber);
        }



        private static void Log(string category, string message, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null, object arg5 = null, object arg6 = null, object arg7 = null, object arg8 = null)//, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            Console.WriteLine(String.Format("{0}: {1}", category, message));
            //Console.WriteLine(String.Format(logMessage, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));//, sourceFilePath, memberName, lineNumber));
        }
    }
}
