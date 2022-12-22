using SourceGenerator;
using System.ComponentModel;
using System.Windows;

namespace CodeGenerator.WPF
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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

            Username = "测试一";
            Username = "测试一";
            Username = "测试一";
            Username = "测试一";
        }

        [RelayCommand]
        private void ChangedData()
        {
            Username = "王熙";
            Code = "123456789011123";
        }
    }
}
