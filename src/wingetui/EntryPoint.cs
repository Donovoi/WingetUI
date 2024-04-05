﻿using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using ModernWindow.Core.Data;
using ModernWindow.Structures;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModernWindow
{
    public static class EntryPoint
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Having an async main method breaks WebView2
            try
            {
                CoreData.IsDaemon = args.Contains("--daemon");

                if (args.Contains("--uninstall-wingetui"))
                    // If the app is being uninstalled, run the cleaner and exit
                    UninstallPreps();
                else
                    // Otherwise, run WingetUI as normal
                    _ = AsyncMain(args);
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }
        
        /// <summary>
        /// WingetUI app main entry point
        /// </summary>
        /// <param name="args">Call arguments</param>
        /// <returns></returns>
        static async Task AsyncMain(string[] args)
        {
            try
            {

                AppTools.Log("Welcome to WingetUI Version " + CoreData.VersionName);
                AppTools.Log("               Version Code " + CoreData.VersionNumber.ToString());
                AppTools.Log("              ");

                // WinRT single-instance fancy stuff
                WinRT.ComWrappersSupport.InitializeComWrappers();
                var isRedirect = await DecideRedirection();

                // If this is the main instance, start the app
                if (!isRedirect)
                {
                    Microsoft.UI.Xaml.Application.Start((p) =>
                    {
                        DispatcherQueueSynchronizationContext context = new(
                            DispatcherQueue.GetForCurrentThread());
                        SynchronizationContext.SetSynchronizationContext(context);
                        new MainApp();
                    });
                }
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }

        /// <summary>
        /// Default WinUI Redirector
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> DecideRedirection()
        {
            try
            {
                // Idk how does this work, I copied it from the MS Docs
                // example on single-instance apps using unpackaged AppSdk + WinUI3
                var isRedirect = false;
                var args = AppInstance.GetCurrent().GetActivatedEventArgs();
                var kind = args.Kind;

                var keyInstance = AppInstance.FindOrRegisterForKey("MartiCliment.WingetUI.NeXT");

                if (keyInstance.IsCurrent)
                {
                    keyInstance.Activated += async (s, e) =>
                    {
                        var AppInstance = MainApp.Current as MainApp;
                        await AppInstance.ShowMainWindowFromRedirectAsync();
                    };
                }
                else
                {
                    isRedirect = true;
                    await keyInstance.RedirectActivationToAsync(args);
                }
                return isRedirect;
            }
            catch (Exception e)
            {
                AppTools.Log(e);
                return false;
            }
        }
        
        /// <summary>
        /// This method should be called when the app is being uninstalled
        /// It removes system links and other stuff that should be removed on uninstall
        /// </summary>
        private static void UninstallPreps()
        {
            try
            {
                ToastNotificationManagerCompat.Uninstall();
            }
            catch
            { 
            }
        }
    }
}
