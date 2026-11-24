using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace Laba_6
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // По умолчанию WPF разбирает числа в привязках по культуре en-US ("21.5").
            // Переключаем на культуру системы, чтобы в таблице можно было вводить "21,5".
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            base.OnStartup(e);
        }
    }
}
