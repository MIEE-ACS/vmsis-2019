using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient clientSocket;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            // создаем новый сокет для подключения к серверу
            clientSocket = new TcpClient();
            // получаем адрес и порт
            string address = tbAddress.Text;
            int port = int.Parse(tbPort.Text);
            // пытаемся подключиться
            clientSocket.Connect(address, port);
            // все получилось
            lblStatus.Content = "Подключен";
            // создаем новый поток для получения сообщений от сервера
            Thread thread = new Thread(() => receiveMessages(clientSocket));
            thread.Start();
        }

        private void pbSend_Click(object sender, RoutedEventArgs e)
        {
            // получаем поток ввода-вывода связанный с сокетом
            NetworkStream serverStream = clientSocket.GetStream();
            // преобразуем введенный текст в массив байтов
            byte[] outStream = System.Text.Encoding.UTF8.GetBytes(tbMessage.Text);
            // передаем этот массив в сокет
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();
        }

        private void receiveMessages(TcpClient socket)
        {
            byte[] bytesFrom = new byte[10025];
            string dataFromServer = null;
            while (true)
            {
                if (!socket.Connected)
                {
                    lbLog.Dispatcher.BeginInvoke(new Action(() => addMessage("Отключен")));
                    return;
                }
                NetworkStream networkStream = socket.GetStream();
                try
                {
                    int read = networkStream.Read(bytesFrom, 0, 10025);
                    if (read > 0)
                    {
                        dataFromServer = Encoding.UTF8.GetString(bytesFrom, 0, read);
                        lbLog.Dispatcher.BeginInvoke(new Action(() => addMessage(dataFromServer)));
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

        private void addMessage(String message)
        {
            lbLog.Items.Add(message);
            var border = (Border)VisualTreeHelper.GetChild(lbLog, 0);
            var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
            scrollViewer.ScrollToBottom();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            clientSocket.Close();
        }
    }
}
