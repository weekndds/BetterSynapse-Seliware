﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SynapseX.CrackerSussyAssets
{
    /// <summary>
    /// Interaction logic for ScriptItem.xaml
    /// </summary>
    public partial class ScriptItem : UserControl
    {
        public static readonly DependencyProperty ScriptProperty = DependencyProperty.Register(
            "Script", typeof(ScriptObject), typeof(ScriptItem), new PropertyMetadata(default(ScriptObject)));

        public ScriptObject Script
        {
            get => (ScriptObject)GetValue(ScriptProperty);
            set
            {
                SetValue(ScriptProperty, value);
                ScriptTitle.Text = value.Title;
                ScriptImage.ImageSource = new BitmapImage(new Uri(value.Thumbnail));
            }
        }

        public event RoutedEventHandler Executed;

        public ScriptItem()
        {
            InitializeComponent();
        }

        private void ExecButton_Click(object sender, RoutedEventArgs e) => Executed?.Invoke(this, e);
        private void CopyButton_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(Script.Script);
    }
}
