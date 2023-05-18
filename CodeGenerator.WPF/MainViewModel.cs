using SourceGenerator;
using System;
using System.ComponentModel;
using System.Windows;

namespace CodeGenerator.WPF
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [AutoNotify]
        private Guid id;

        [AutoNotify]
        private string code;

        [AutoNotify("Username")]
        private string name;

        [AutoNotify]
        private string description;

        [RelayCommand]
        private void Loaded(Window main)
        {
            main.WindowState = WindowState.Maximized;
            main.WindowState = WindowState.Maximized;
            main.WindowState = WindowState.Maximized;
            main.WindowState = WindowState.Maximized;

            Username = "测试";
        }

        [RelayCommand]
        private void ChangedData()
        {
            this.SetId(Guid.NewGuid())
                .SetCode("123")
                .SetUsername("Wx")
                .SetDescription("描述");

            Username = "王熙";
            Code = "123456789011123";
            Code = "123456789011123";
            Code = "123456789011123";
        }
    }
}
