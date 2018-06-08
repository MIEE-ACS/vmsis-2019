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

        enum CMD
        {
            NewGame,
            Waiting,
            Move,
            EndGame
        }

        enum Type
        {
            None,
            Zero,
            Cross
        }

        TcpClient socket;
        Type myType = Type.None;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var column = Grid.GetColumn(button);
                var row = Grid.GetRow(button);

                var index = column + (row * 3);

                var stream = socket.GetStream();
                stream.Write(new byte[] { (byte)CMD.Move,
                                      (byte)index,
                                      (byte)myType }, 0, 3);
                
            }
            catch
            {
                lblStatus.Content = "Сервер недоступен";
            }

        }

        private void connect(object sender, RoutedEventArgs e)
        {
            try
            {
                string server = tbServer.Text;
                int port = int.Parse(tbPort.Text);
                socket = new TcpClient(server, port);

                Thread thread = new Thread(receiveServer);
                thread.Start();
            }
            catch
            {
                lblStatus.Content = "Сервер недоступен";
            }
        }

        private void receiveServer()
        {
            byte[] buf = new byte[1024];
            var stream = socket.GetStream();
            while(true)
            {
                try
                {
                    int count = stream.Read(buf, 0, 1024);
                    if (count > 0)
                    {
                        gameWindow.Dispatcher.BeginInvoke(new Action(() => parseMessage(buf)));
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch
                {
                    break;
                }
            }
        }
        private void parseMessage(byte[] buf)
        {
            switch ((CMD)buf[0])
            {
                case CMD.NewGame:
                    lblStatus.Content = "Начинаем игру!";
                    newGame((Type) buf[1]);
                    break;
                case CMD.Waiting:
                    lblStatus.Content = "Ожидаем второго игрока";
                    break;
                case CMD.Move:
                    int position = buf[1];
                    Type type = (Type) buf[2];
                    move(position, type);
                    break;
                case CMD.EndGame:
                    endGame(buf[1]);
                    break;
            }
        }

        private void endGame(byte result)
        {
            if (result == 0)
                lblStatus.Content = "Вы проиграли!";
            else if (result == 1)
                lblStatus.Content = "Вы победили!";
            else
                lblStatus.Content = "Ничья!";
            socket.Close();
        }

        private void newGame(Type type)
        {
            myType = type;
            foreach(var b in Container.Children.Cast<Button>())
            {
                b.Content = "";
                b.Background = Brushes.White;
                b.Foreground = Brushes.Blue;
            }
        }

        private void move(int position, Type type)
        {
            var button = Container.Children.Cast<Button>().ToList()[position];
            if (type == Type.Cross)
            {
                button.Content = "X";
            }
            else
            {
                button.Content = "O";
            }
        }
    }
}
