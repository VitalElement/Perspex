using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Avalonia.Designer.AppHost;
using Avalonia.Designer.Comm;
using Avalonia.Designer.Metadata;

namespace Avalonia.Designer
{
    /// <summary>
    /// Interaction logic for PerpexDesigner.xaml
    /// </summary>
    public partial class AvaloniaDesigner
    {
        public static readonly DependencyProperty TargetExeProperty = DependencyProperty.Register(
            "TargetExe", typeof (string), typeof (AvaloniaDesigner), new FrameworkPropertyMetadata(TargetExeChanged));

        private static void TargetExeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AvaloniaDesigner) d).RestartProcess();
        }
        public string TargetExe
        {
            get { return (string) GetValue(TargetExeProperty); }
            set { SetValue(TargetExeProperty, value); }
        }

        public static readonly DependencyProperty XamlProperty = DependencyProperty.Register(
            "Xaml", typeof (string), typeof (AvaloniaDesigner), new FrameworkPropertyMetadata(XamlChanged));

        private static void XamlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AvaloniaDesigner) d).OnXamlChanged();
        }

        public string Xaml
        {
            get { return (string) GetValue(XamlProperty); }
            set { SetValue(XamlProperty, value); }
        }

        public AvaloniaDesignerMetadata Metadata { get; private set; }
        
        private readonly ProcessHost _host = new ProcessHost();
        

        public AvaloniaDesigner()
        {
            InitializeComponent();
            BindingOperations.SetBinding(State, TextBox.TextProperty,
                new Binding(nameof(ProcessHost.State)) {Source = _host, Mode = BindingMode.OneWay});

            _host.PropertyChanged += _host_PropertyChanged;
            _host.MetadataArrived += data => Metadata = data;
        }

        private void _host_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProcessHost.WindowHandle))
            {
                if (NativeContainer.Child != null)
                {
                    var child = NativeContainer.Child;
                    NativeContainer.Child = null;
                    child.Dispose();
                }
                NativeContainer.Child = new WindowHost(false);
                var wndHost = ((WindowHost) NativeContainer.Child);
                wndHost.SetWindow(_host.WindowHandle);


            }
        }


        public void KillProcess()
        {
            _host.Kill();
        }

        bool CheckTargetExeOrSetError()
        {
            if (string.IsNullOrEmpty(TargetExe))
            {
                _host.State = "No target exe found";
                return false;
            }

            if (File.Exists(TargetExe ?? ""))
                return true;
            _host.State = "No target binary found, build your project";
            return false;
        }

        public void RestartProcess()
        {
            KillProcess();
            if(!CheckTargetExeOrSetError())
                return;
            if(string.IsNullOrEmpty(Xaml))
                return;
            _host.Start(TargetExe, Xaml);
        }

        private void OnXamlChanged()
        {
            if (!CheckTargetExeOrSetError())
                return;
            if (!_host.IsAlive)
                _host.Start(TargetExe, Xaml);
            else
                _host.UpdateXaml(Xaml ?? "");
        }

    }
}
