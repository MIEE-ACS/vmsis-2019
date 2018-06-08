using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;

namespace PeerToPeerChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Принимающий сокет
        UdpClient receivingUdpClient;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик нажатия кнопки Start. Создает поток принимающий входящие сообщения.
        /// </summary>
        private void pbStart_Click(object sender, RoutedEventArgs e)
        {
            // Получаем номер порта который будет слушать наш клиент
            int port = int.Parse(tbLocalPort.Text);
            // Запускаем метод Receiver в отдельном потоке
            Thread tRec = new Thread(() => Receiver(port));
            tRec.Start();
        }

        /// <summary>
        /// Метод принимающий входящие сообщения
        /// </summary>
        private void Receiver(int port)
        {
            // Создаем UdpClient для чтения входящих данных
            receivingUdpClient = new UdpClient(port);
            IPEndPoint remoteIpEndPoint = null;

            try
            {
                while (true)
                {
                    // Ожидание дейтаграммы
                    byte[] receiveBytes = receivingUdpClient.Receive(ref remoteIpEndPoint);

                    // Преобразуем байтовые данные в строку
                    string returnData = Encoding.UTF8.GetString(receiveBytes);

                    // Выводим данные из нашего потока в основной
                    lbLog.Dispatcher.BeginInvoke(new Action( () => addMessage(" --> " + returnData)));
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки завершаем поток и выводим сообщение в лог чата
                lbLog.Dispatcher.BeginInvoke(new Action(() => 
                    addMessage("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message))
                );
            }
        }

        /// <summary>
        /// Метод отрабатывающий нажатие на кнопку Send
        /// </summary>
        private void pbSend_Click(object sender, RoutedEventArgs e)
        {
            // Создаем сокет для отправки данных
            UdpClient other = new UdpClient();

            // Создаем endPoint по информации об удаленном хосте
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(tbRemoteAddr.Text), 
                                                 int.Parse(tbRemotePort.Text));

            try
            {
                // Преобразуем данные в массив байтов, используя кодировку UTF-8
                byte[] bytes = Encoding.UTF8.GetBytes(tbMessage.Text);

                // Отправляем данные
                other.Send(bytes, bytes.Length, endPoint);
                // выводим наше сообщение в лог
                addMessage(tbMessage.Text);
                // очищаем поле ввода
                tbMessage.Clear();
            }
            catch (Exception ex)
            {
                // в случае ошибки выводим сообщение в лог
                addMessage("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
            finally
            {
                // Закрыть соединение
                other.Close();
            }
        }

        /// <summary>
        /// Метод выводящий текст в лог чата
        /// </summary>
        private void addMessage(string message)
        {
            // добавляем с сообщению метку времени и выводим в лог
            lbLog.Items.Add(DateTime.Now.ToString() + " > " + message);

            // прокручиваем лог до самого последнего сообщения
            var border = (Border)VisualTreeHelper.GetChild(lbLog, 0);
            var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
            scrollViewer.ScrollToBottom();
        }

        /// <summary>
        /// Обработчик закрытия окна
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // при закрытии окна обязательно закрываем принимающий сокет
            if (receivingUdpClient != null)
                receivingUdpClient.Close();
        }
    }
}
