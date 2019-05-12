using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Net;
using System.Threading.Tasks;

namespace TicTacToe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Перечисление описывающее набор команд 
        // пересылаемых между сервером и клиентами
        enum CMD
        {
            NewGame, // Начало новой игры
            Waiting, // Ожидание второго игрока
            Move,    // Ход игрока
            EndGame  // Игра завершена
        }

        // Перечисление описывающее состояние
        // ячейки поля
        enum Type
        {
            None,   // Пустая ячейка
            Zero,   // Нолик
            Cross   // Крестик
        }

        TcpListener server;
        TcpClient player1;
        TcpClient player2;

        // ссылка на игрока, который будет ходить
        // следующим
        TcpClient nextTurn;

        // Состояние игрового поля
        Type[] field = new Type[9];

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            int port = int.Parse(tbPort.Text);
            server = new TcpListener(IPAddress.Any, port);
            Thread thread = new Thread(waitingClients);
            thread.Start();
        }

        private void waitingClients()
        {
            server.Start();
            serverWindow.Dispatcher.BeginInvoke(new Action(() => addMessage("Сервер запущен")));
            while (true)
            {
                var client = server.AcceptTcpClient();
                serverWindow.Dispatcher.BeginInvoke(new Action(() => addMessage("Клиент подключен")));
                acceptClient(client);
            }
        }

        private void acceptClient(TcpClient client)
        {
            // Если не подключен ни один игрок, то этот клиент будет первым
            if (player1 == null)
                player1 = client;
            else //иначе - вторым
                player2 = client;

            // создаем поток для обмена данными с клиентом
            Thread thread = new Thread(() => listenClient(client));
            thread.Start();

            // если подключился первый игрок
            if(client == player1)
            {
                // то сообщаем ему, что нужно долждаться второго
                serverWindow.Dispatcher.BeginInvoke(new Action(() => addMessage("Waiting for second player")));
                NetworkStream stream = client.GetStream();
                stream.Write(new byte[] { (byte)CMD.Waiting }, 0, 1);
            }
            else
            {   // если подключился второй игрок, то начинаем новую игру
                serverWindow.Dispatcher.BeginInvoke(new Action(() => addMessage("Starting new game")));
                // Очищаем поле
                for (int i = 0; i < field.Length; i++)
                    field[i] = Type.None;
                // Сообщаем игрокам о начале игры
                var stream = player1.GetStream();
                stream.Write(new byte[] { (byte)CMD.NewGame,(byte) Type.Zero }, 0, 2);
                stream = player2.GetStream();
                stream.Write(new byte[] { (byte)CMD.NewGame, (byte)Type.Cross }, 0, 2);
                // первым ходит игрок подключившийся первым
                nextTurn = player1;
            }
        }
        
        private void listenClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buf = new byte[1024];
            while (true)
            {
                try
                {
                    int count = stream.Read(buf, 0, 1024);
                    if (count > 0)
                    {
                        serverWindow.Dispatcher.BeginInvoke(new Action(() => parseClient(client, buf)));
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
        private void parseClient(TcpClient client, byte[] response)
        {   
            // от клиента мы принимаем только одну команду - ход
            if ((CMD) response[0] == CMD.Move)
            {
                // если команда пришла не от того игрока
                // который должен ходить, то игнорируем
                if (client != nextTurn)
                    return;

                // Следующим будет ходить другой игрок
                if (client == player1)
                    nextTurn = player2;
                else
                    nextTurn = player1;

                // Получаем номер поля в которе походил игрок
                int move = response[1];
                // Получаем тип символа - О или Х
                Type type = (Type) response[2];

                // Здесь должна быть проверка на правильность хода!

                // Ставим символ в это поле
                field[move] = type;
                
                // Отправляем игрокам сообщение о том что был сделан ход
                var stream = player1.GetStream();
                stream.Write(new byte[] { (byte)CMD.Move, (byte) move, (byte)type }, 0 , 3);
                stream = player2.GetStream();
                stream.Write(new byte[] { (byte)CMD.Move, (byte)move, (byte)type }, 0, 3);

                // с небольшой задержкой проверяем не закончилась ли игра
                Task.Delay(101).ContinueWith(t => checkForWinner());
            }
        }

        private void checkForWinner()
        {
            bool gameEnded = false;
            TcpClient winner = null;
            for(int i = 0; i < 3; i++)
            {   // Горизонтальные линии
                if (field[i * 3] != Type.None && (field[i * 3 + 0] & field[i * 3 + 1] & field[i * 3 + 2]) == field[i * 3])
                {
                    gameEnded = true;
                    winner = field[i * 3] == Type.Zero ? player1 : player2;
                }
                // Вертикальные линии
                if (field[i] != Type.None && (field[i + 0] & field[i + 3] & field[i + 6]) == field[i])
                {
                    gameEnded = true;
                    winner = field[i] == Type.Zero ? player1 : player2;
                }
            }   // Диагональ 1
            if(field[0] != Type.None && (field[0] & field[4] & field[8]) == field[0])
            {
                gameEnded = true;
                winner = field[0] == Type.Zero ? player1 : player2;
            }   // Диагональ 2
            if (field[2] != Type.None && (field[2] & field[4] & field[6]) == field[2])
            {
                gameEnded = true;
                winner = field[2] == Type.Zero ? player1 : player2;
            }
            // Осталось ли место на поле
            if (!field.Any(f => f == Type.None))
            {
                gameEnded = true;
            }
            if (gameEnded)
            {
                gameOver(winner);
            }
        }

        private void gameOver(TcpClient winner)
        {
            if (winner == player1)
            {
                var stream = player1.GetStream();
                stream.Write(new byte[] { (byte)CMD.EndGame, 1 }, 0, 2);
                stream = player2.GetStream();
                stream.Write(new byte[] { (byte)CMD.EndGame, 0 }, 0, 2);
            }
            else if (winner == player2)
            {
                var stream = player1.GetStream();
                stream.Write(new byte[] { (byte)CMD.EndGame, 0 }, 0, 2);
                stream = player2.GetStream();
                stream.Write(new byte[] { (byte)CMD.EndGame, 1 }, 0, 2);
            }
            else
            {   // если ничья
                var stream = player1.GetStream();
                stream.Write(new byte[] { (byte)CMD.EndGame, 2 }, 0, 2);
                stream = player2.GetStream();
                stream.Write(new byte[] { (byte)CMD.EndGame, 2 }, 0, 2);
            }
            // Закрываем подключение
            player1.Close();
            player2.Close();
            player1 = null;
            player2 = null;
        }

        private void addMessage(String message)
        {
            lblLog.Items.Add(message);
        }

        private void tbPort_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
