//
// RunOnMainThread.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System.Reflection;
using GLib;

namespace Meshwork.Client.GtkClient
{
    public class RunOnMainThread
    {
        private object methodClass;
        private string methodName;
        private object[] arguments;
       
        public static void Run(object methodClass, string methodName, params object[] arguments)
        {
            new RunOnMainThread(methodClass, methodName, arguments);
        }
   		
        public RunOnMainThread(object methodClass, string methodName, params object[] arguments)
        {
            this.methodClass = methodClass;
            this.methodName = methodName;
            this.arguments = arguments;
            GLib.Idle.Add(new IdleHandler(Go));
        }

        private bool Go()
        {
            methodClass.GetType().InvokeMember (methodName, BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null,methodClass, arguments);
            return false;
        }
    }
}
