﻿namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using DataGridExtensions;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Controls;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Tools;

    /// <summary>
    /// Interaction logic for ResourceView.xaml
    /// </summary>
    [ContractVerification(false)]   // Too many warnings from generated code.
    public partial class ResourceView
    {
        private static readonly DependencyProperty HardReferenceToDgx = DataGridFilterColumn.FilterProperty;

        public ResourceView()
        {
            if (HardReferenceToDgx == null) // just use this...
            {
                Trace.WriteLine("HardReferenceToDgx failed");
            }

            InitializeComponent();

            BindingOperations.SetBinding(this, EntityFilterProperty, new Binding("DataContext.EntityFilter") { Source = this });
        }

        private static readonly DependencyProperty EntityFilterProperty =
            DependencyProperty.Register("EntityFilter", typeof(string), typeof(ResourceView), new FrameworkPropertyMetadata(null, (sender, e) => Settings.Default.ResourceFilter = (string)e.NewValue));

        public double TextFontSize
        {
            get { return (double)GetValue(TextFontSizeProperty); }
            set { SetValue(TextFontSizeProperty, value); }
        }
        public static readonly DependencyProperty TextFontSizeProperty =
            DependencyProperty.Register("TextFontSize", typeof (double), typeof (ResourceView), new UIPropertyMetadata(12.0));

        private ResourceManager ViewModel
        {
            get
            {
                return (ResourceManager)DataContext;
            }
        }

        private void self_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as ResourceManager;
            if (oldValue != null)
            {
                oldValue.Loaded -= ResourceManager_Loaded;
            }

            var newValue = e.NewValue as ResourceManager;
            if (newValue != null)
            {
                newValue.Loaded += ResourceManager_Loaded;
                newValue.EntityFilter = Settings.Default.ResourceFilter;
            }
        }

        private void NeutralLanguage_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.NeutralResourceLanguage = (CultureInfo)(((MenuItem)sender).DataContext);
        }

        private void ResourceManager_Loaded(object sender, EventArgs e)
        {
            DataGrid.SetupColumns(ViewModel.Languages);
        }

        private void AddLanguage_Click(object sender, RoutedEventArgs e)
        {
            var inputBox = new InputBox
            {
                Title = Properties.Resources.Title,
                Prompt = Properties.Resources.NewLanguageIdPrompt,
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var languages = ViewModel.Languages.Where(l => l != null).Select(l => l.ToString()).ToArray();

            inputBox.TextChanged += (_, args) =>
                inputBox.IsInputValid = !languages.Contains(args.Text, StringComparer.OrdinalIgnoreCase) && ResourceManager.IsValidLanguageName(args.Text);

            if (inputBox.ShowDialog() == true)
            {
                DataGrid.Columns.AddLanguageColumn(new CultureInfo(inputBox.Text));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used via XAML!")]
        private void DeleteCommandConverter_OnExecuting(object sender, CancelEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmDeleteItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                e.Cancel = true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used via XAML!")]
        private void CutCommandConverter_OnExecuting(object sender, CancelEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmCutItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                e.Cancel = true;
        }
    }
}
