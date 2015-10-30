using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace WPFServer
{
    public partial class MainWindow : Window
    {
        public IDisposable SignalR { get; set; } //Интерфейс освобождения неуправляемых рессурсов
        const string ServerURI = "http://localhost:8080";

        public MainWindow()
        {
            InitializeComponent(); //Инициализация всех графических компонентов конструктора форм
        }

        //Старт сервера в новой задаче
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            WriteToConsole("Starting server...");
            ButtonStart.IsEnabled = false;            
            Task.Run(() => StartServer()); //Асинхронное выполнение еденичной задачи
        }

        //Остановка сервера
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            SignalR.Dispose(); //Освобождение неуправляемого рессурса
            Close();
        }


        private void StartServer()
        {
            try
            {
                SignalR = WebApp.Start(ServerURI);
            }
            catch (TargetInvocationException) //Задача уже выполняется
            {
                WriteToConsole("A server is already running at " + ServerURI);
                this.Dispatcher.Invoke(() => ButtonStart.IsEnabled = true); //Invoke, для выполения метода из потока UI, при нахождении в другом
                return;
            }
            this.Dispatcher.Invoke(() => ButtonStop.IsEnabled = true);
            WriteToConsole("Server started at " + ServerURI);
        }


        public void WriteToConsole(String message)
        {
            if (!(RichTextBoxConsole.CheckAccess())) //Если находимся в другом потоке, доступ через Invoke
            {
                this.Dispatcher.Invoke(() =>
                    WriteToConsole(message)
                );
                return;
            }
            RichTextBoxConsole.AppendText(message + "\r");
        }
    }


    //Конфигурирование сервера под спецификацию owin
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }


    public class MyHub : Hub
    {
        //Передача сообщения все клиентам
        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
        }
        
        //Оповещение об установленном соединении с клиентом
        public override Task OnConnected()
        {
            //Доступ к другому потоку
            Application.Current.Dispatcher.Invoke(() => 
                ((MainWindow)Application.Current.MainWindow).WriteToConsole("Client connected: " + Context.ConnectionId));

            return base.OnConnected();
        }

        //Оповещение о разорванном соединении
        public override Task OnDisconnected()
        {
            //Доступ к другому потоку
            Application.Current.Dispatcher.Invoke(() => 
                ((MainWindow)Application.Current.MainWindow).WriteToConsole("Client disconnected: " + Context.ConnectionId));

            return base.OnDisconnected();
        }

    }
}
