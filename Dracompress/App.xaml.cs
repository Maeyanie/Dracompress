using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Threading;

// ...existing usings...

namespace Dracompress
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string LightTheme = "Themes/Light.xaml";
        private const string DarkTheme = "Themes/Dark.xaml";
        private string _currentTheme = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ApplySystemTheme();

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }

        private void SystemEvents_UserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        {
            // Theme change can be signaled via UserPreferenceChanged; re-apply theme on UI thread
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => ApplySystemTheme()));
        }

        private void ApplySystemTheme()
        {
            bool isLight = IsSystemUsingLightTheme();
            string desired = isLight ? LightTheme : DarkTheme;
            if (_currentTheme == desired) return;

            // Remove existing theme dictionaries we added
            var existing = new System.Collections.Generic.List<ResourceDictionary>();
            foreach (var rd in Resources.MergedDictionaries)
            {
                if (rd.Source != null && (rd.Source.OriginalString.EndsWith(LightTheme, StringComparison.OrdinalIgnoreCase) || rd.Source.OriginalString.EndsWith(DarkTheme, StringComparison.OrdinalIgnoreCase)))
                {
                    existing.Add(rd);
                }
            }

            foreach (var rd in existing)
                Resources.MergedDictionaries.Remove(rd);

            var themeDict = new ResourceDictionary { Source = new Uri(desired, UriKind.Relative) };
            Resources.MergedDictionaries.Add(themeDict);
            _currentTheme = desired;
        }

        private static bool IsSystemUsingLightTheme()
        {
            try
            {
                const string key = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
                var val = Registry.GetValue(key, "AppsUseLightTheme", 1);
                if (val is int iv)
                    return iv != 0;
                if (val is long lv)
                    return lv != 0;
                return true;
            }
            catch
            {
                return true;
            }
        }
    }

}
