using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
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

using System.Net.Sockets;

namespace WpfApp4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpListener serverSocket;
        List<TcpClient> clients = new List<TcpClient>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            // Считываем номер порта
            int port = int.Parse(tbPort.Text);
            // Создаем слушающий сокет
            serverSocket = new TcpListener(IPAddress.Any, port);
            serverSocket.Start();
            // Создаем и запускаем поток ожидания нового клиента
            Thread thread = new Thread(waitForClient);
            thread.Start();            
        }
        private void waitForClient()
        {
            // Пока сокет подключен к порту
            while (serverSocket.Server.IsBound)
            {
                try
                {
                    // Начинаем ожидание нового клиента
                    // Эта функция блокирует поток и прервется 
                    // либо по исключению
                    // либо по подключению нового клиента
                    var clientSocket = serverSocket.AcceptTcpClient();
                    // если мы попали сюда, значит к нам подключился клиент
                    // передаем сокет клиента в основной поток
                    SrvWindow.Dispatcher.BeginInvoke(
                        new Action(() => addClient(clientSocket)));
                }
                catch (SocketException)
                {
                    continue;
                }                
            }            
        }
        private void addClient(TcpClient client)
        {
            // Добавляем клиента в список
            clients.Add(client);
            // Выводим сообщение
            lbLog.Items.Add("Client connected: " + client);
            // Создаем и запускаем новый поток
            Thread thread = new Thread(() => clientThread(client));
            thread.Start();
        }

        private void clientThread(TcpClient client)
        {
            // буфер чтения
            byte[] bytesFrom = new byte[10000];
            string dataFromClient = null;
        
            while (true)
            {
                if(!client.Connected) // если клиент отключился, то закрываем соединение
                {
                    lbLog.Dispatcher.BeginInvoke(new Action(() => disconnect(client)));
                    return;
                }
                // Получаем поток ввода\вывода клиента
                NetworkStream networkStream = client.GetStream();
                try
                {   // Читаем данные пришедшие от клиента
                    int read = networkStream.Read(bytesFrom, 0, 10000);
                    if (read > 0)
                    {   // Если клиент что-то прислал, то декодируем сообщение
                        dataFromClient = Encoding.UTF8.GetString(bytesFrom, 0, read);
                        // Ретранслируем сообщение остальным клиентам
                        lbLog.Dispatcher.BeginInvoke(new Action(() => relayMessage(client, dataFromClient)));
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" >> " + ex.ToString());
                }
            }
        }
        private void relayMessage(TcpClient client, String message)
        {
            // Проходим по каждому из клиентов
            foreach (var c in clients)
            {
                // Получаем его поток ввода\вывода
                var clientStream = c.GetStream();
                // Кодируем сообщение в UTF-8
                byte[] outStream = System.Text.Encoding.UTF8.GetBytes(message);
                // Отправляем данные
                clientStream.Write(outStream, 0, outStream.Length);
                clientStream.Flush();                
            }
            // Выводим сообщение в лог сервера для отладки
            addMessage(message);
        }

        private void disconnect(TcpClient client)
        {
            addMessage("Client disconnected");
            clients.Remove(client);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            serverSocket.Stop();
            foreach (var c in clients)
            {
                c.Close();
            }
        }

        private void addMessage(String message)
        {
            lbLog.Items.Add(message);
            var border = (Border)VisualTreeHelper.GetChild(lbLog, 0);
            var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
            scrollViewer.ScrollToBottom();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
