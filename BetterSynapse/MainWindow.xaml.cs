// the unmodified source is from https://github.com/rice-cracker-2234/BetterSynapse/
// this is ported to support seliware by https://github.com/weekndds (it was extremely easy cause we love seliware api)

using CefSharp;
using CefSharp.Wpf;
using Newtonsoft.Json;
using SeliwareAPI;
using SynapseX.CrackerSussyAssets;
using SynapseX.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Controls.ContextMenu;
using FontFamily = System.Windows.Media.FontFamily;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Orientation = System.Windows.Controls.Orientation;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SynapseX
{
    public partial class MainWindow : Window
    {
        private ManagementEventWatcher processStartWatcher;
        private const string processName = "RobloxPlayerBeta.exe";
        public MainWindow()
        {
            Settings.Default.PropertyChanged += (_, _) => Settings.Default.Save();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => File.AppendAllText("bs_bin/error.log", args.ExceptionObject + Environment.NewLine + Environment.NewLine);

            InitializeComponent();
            Seliware.Initialize();

            Seliware.Injected += OnSeliwareInjected;
            LoadSettings();
            SetupProcessWatcher();
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Directory.CreateDirectory("scripts");
            Directory.CreateDirectory("bs_bin/tabs");
            Editor.InitializeDirectory("bs_bin/tabs");

            await PopulateScriptTree(ScriptList, "scripts");
            var watcher = new FileSystemWatcher("scripts")
            {
                NotifyFilter = NotifyFilters.Attributes
                               | NotifyFilters.CreationTime
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.FileName
                               | NotifyFilters.LastAccess
                               | NotifyFilters.LastWrite
                               | NotifyFilters.Security
                               | NotifyFilters.Size
            };


            async void OnFileEvent(object obj, FileSystemEventArgs eventArgs)
            {
                await Dispatcher.InvokeAsync(async delegate
                {
                    await PopulateScriptTree(ScriptList, "scripts");
                });
            }

            watcher.Changed += OnFileEvent;
            watcher.Created += OnFileEvent;
            watcher.Deleted += OnFileEvent;
            watcher.Renamed += OnFileEvent;

            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }


        private void DraggableBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
        private void StateButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Window_StateChanged(object sender, EventArgs e)
        {
            StateButton.Content = WindowState == WindowState.Maximized ? '\xe923' : '\xe922';
            Main.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        }

        private async Task PopulateScriptTree(ItemsControl treeView, string directory)
        {
            // this code is pretty bad but it'll work for now.
            // ^^ this comment was NOT made by me - wish
            treeView.Items.Clear();

            foreach (var path in Directory.GetDirectories(directory))
            {
                var info = new DirectoryInfo(path);

                void OnDeleteDirectoryClick(object sender, RoutedEventArgs e)
                {
                    if (Messages.ShowGenericWarningMessage())
                        Directory.Delete(path, true);
                }

                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                var icon = new TextBlock
                {
                    FontFamily = TryFindResource("Segoe Fluent Icons") as FontFamily,
                    FontSize = 14,
                    Text = "\xe8b7",
                    Margin = new Thickness(0, 0, 3, 0)
                };

                var content = new TextBlock
                {
                    FontSize = 12,
                    Text = info.Name,
                    VerticalAlignment = VerticalAlignment.Center
                };

                panel.Children.Add(icon);
                panel.Children.Add(content);

                var item = new TreeViewItem { Header = panel };

                var menu = new ContextMenu();

                var deleteDirectory = new MenuItem { Header = "Delete Directory" };
                deleteDirectory.Click += OnDeleteDirectoryClick;

                menu.Items.Add(deleteDirectory);
                item.ContextMenu = menu;
                treeView.Items.Add(item);
                await PopulateScriptTree(item, path);
            }

            foreach (var file in Directory.GetFiles(directory))
            {
                var info = new FileInfo(file);

                void OnExecuteClick(object sender, RoutedEventArgs e)
                {
                    var script = File.ReadAllText(file);
                    Seliware.Execute(script);
                }

                void OnLoadScriptClick(object sender, RoutedEventArgs e)
                {
                    if (Messages.ShowGenericQuestionMessage("Open in a new tab?"))
                        Editor.CreateTab(info.Name, File.ReadAllText(file));
                    else
                        Editor.SelectedEditor.ExecuteScriptAsync($"setText({JsonConvert.SerializeObject(File.ReadAllText(file))})");
                }

                void OnSaveScriptClick(object sender, RoutedEventArgs e) => File.WriteAllText(file, Editor.SelectedEditor.EvaluateScript("getText()"));

                void OnDeleteScriptClick(object sender, RoutedEventArgs e)
                {
                    if (Messages.ShowGenericWarningMessage())
                        File.Delete(file);
                }

                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                var icon = new TextBlock
                {
                    FontFamily = TryFindResource("Codicon") as FontFamily,
                    FontSize = 14,
                    Text = "\xea7b",
                    Margin = new Thickness(0, 0, 3, 0)
                };

                var content = new TextBlock
                {
                    FontSize = 12,
                    Text = info.Name,
                    VerticalAlignment = VerticalAlignment.Center
                };

                panel.Children.Add(icon);
                panel.Children.Add(content);

                var item = new TreeViewItem { Header = panel };

                var menu = new ContextMenu();

                var executeScript = new MenuItem { Header = "Execute Script" };
                executeScript.Click += OnExecuteClick;

                var loadScript = new MenuItem { Header = "Load Script" };
                loadScript.Click += OnLoadScriptClick;

                var saveScript = new MenuItem { Header = "Save Script" };
                saveScript.Click += OnSaveScriptClick;

                var deleteScript = new MenuItem { Header = "Delete Script" };
                deleteScript.Click += OnDeleteScriptClick;

                menu.Items.Add(executeScript);
                menu.Items.Add(loadScript);
                menu.Items.Add(saveScript);
                menu.Items.Add(deleteScript);
                item.ContextMenu = menu;
                treeView.Items.Add(item);
            }
        }

        public static readonly string FileFilter = FilterInstance.ToString(new[]
        {
            new FilterInstance
            {
                Title = "Text Document",
                Filter = "*.txt",
                IncludeFilter = true
            },

            new FilterInstance
            {
                Title = "LUA Script",
                Filter = "*.lua",
                IncludeFilter = true
            }
        });

        private void Panels_Click(object sender, RoutedEventArgs e) => ((Storyboard)TryFindResource("PanelClosedStoryboard")).Begin();
        private void SettingsButton_Click(object sender, RoutedEventArgs e) => ((Storyboard)TryFindResource("SettingsOpenStoryboard")).Begin();
        private void ScriptHubButton_Click(object sender, RoutedEventArgs e) => ((Storyboard)TryFindResource("ScriptHubOpenStoryboard")).Begin();
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Save File",
                Filter = FileFilter,
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (saveDialog.ShowDialog() == true) File.WriteAllText(saveDialog.FileName, Editor.SelectedEditor.EvaluateScript("getText()"));
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Open File",
                Filter = FileFilter,
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openDialog.ShowDialog() != true) return;
            if (Messages.ShowGenericQuestionMessage("Open in a new tab?"))
                Editor.CreateTab(Path.GetFileName(openDialog.FileName), File.ReadAllText(openDialog.FileName));
            else Editor.SelectedEditor.ExecuteScriptAsync("setText", File.ReadAllText(openDialog.FileName));
        }

        private void LoadSettings()
        {
            TopMost.IsChecked = Topmost = Settings.Default.TopMost;
            AutoAttach.IsChecked = Settings.Default.AutoAttach;
            UnlockFPS.IsChecked = Settings.Default.UnlockFPS;
        }

        private void SetupProcessWatcher()
        {
            string startQuery = $"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{processName}'";
            processStartWatcher = new ManagementEventWatcher(startQuery);
            processStartWatcher.EventArrived += OnRobloxProcessStarted;
            processStartWatcher.Start();
        }


        private void OnRobloxProcessStarted(object sender, EventArrivedEventArgs e)
        {
            if (Settings.Default.AutoAttach)
            {
                Dispatcher.Invoke(() =>
                {
                    Seliware.Inject();
                });
            }
        }

        private void OnSeliwareInjected(object sender, EventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    InjectState.Content = "Attached";
                    StateColor.Fill = Brushes.Lime;

                    //  for some reason seliware's setfpscap doesnt work so
                    //if (Settings.Default.UnlockFPS)
                    //{
                    //    Seliware.Execute("setfpscap(99999)");
                    //}
                });
            }
            catch (Exception ex)
            {
                File.AppendAllText("bs_bin/error.log", $"Error in OnSeliwareInjected: {ex}" + Environment.NewLine);
            }
        }

        private void TopMost_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.TopMost = Topmost = TopMost.IsChecked.GetValueOrDefault();
            Settings.Default.Save();
        }

        private void AutoAttach_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.AutoAttach = AutoAttach.IsChecked.GetValueOrDefault();
            Settings.Default.Save();
        }

        private void UnlockFPS_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.UnlockFPS = UnlockFPS.IsChecked.GetValueOrDefault();
            Settings.Default.Save();
        }


        private void AttachButton_Click(object sender, RoutedEventArgs e) => Seliware.Inject();
        private void ExecuteButton_Click(object sender, RoutedEventArgs e) => Seliware.Execute(Editor.SelectedEditor.EvaluateScript("getText()"));

        private void KillRobloxButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var process in Process.GetProcessesByName("RobloxPlayerBeta"))
                process.Kill();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            processStartWatcher?.Stop();
            processStartWatcher?.Dispose();
            Process.GetCurrentProcess().Kill();
        }
    }
}
