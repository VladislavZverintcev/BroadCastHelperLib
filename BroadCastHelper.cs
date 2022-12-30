using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace BroadCastHelperLib
{
    public class BroadCastHelper
    {
        #region Fields
        bool isListenerStarted = false; //Отвечает за контроль запуска прослушивания (не для ручного изменения)
        //Путь до папки в AppData
        private string appDataFolderPath; //Путь к папке проекта в APPData для того чтобы хранить там файл сетевых настроек (Обязательное требование при создании хелпера в конструкторе)
        private List<string> userNames = new List<string>(); //Известные имена пользователей которые откликались
        private List<string> newUserNames = new List<string>(); //Список пользователей которые были выявлены в момент проверки
        private int checkInterval = 5; //Интервал между опросом кто на месте
        private System.Timers.Timer checkIntervalTimer;
        private int checkTime = 5; //Время которое даётся другим пользователям на отклик "Я тут!", на наш вопрос "Кто здесь?"
        private System.Timers.Timer checkTimer;
        #endregion Fields
        #region Properties
        /// <summary>
        /// Список имён активных пользователей учавствующих в сетевой группе (Только для чтения)
        /// </summary>
        public List<string> UserNames
        {
            get { return userNames; }
        } //Доступ для чтения имён пользователей которые откликаются
        #endregion Properties
        #region Events
        /// <summary>
        /// Событие которое происходит при получении сообщения, событие можно получить только если оно было адресованно конкретному пользователю, или если оно было разосланно с пометкой "All"
        /// Совместно с событием передаётся: 1 - имя отправителя, 2 - сообщение; 
        /// </summary>
        public event Action<string, string> Incoming;
        public event Action CheckUsersFinished;
        #endregion Events
        #region Constructors
        public BroadCastHelper(string _appDataFolderPath)
        {
            if (string.IsNullOrEmpty(_appDataFolderPath) || string.IsNullOrWhiteSpace(_appDataFolderPath))
            {
                throw new ArgumentException();
            }
            appDataFolderPath = _appDataFolderPath;
            StartListener();
            Incoming += RequestHandler;
            CheckUsers();
        }
        #endregion Constructors
        #region Methods

        #region Privates
        void RequestHandler(string sender, string msg)
        {
            switch (msg)
            {
                case "Кто здесь?":
                    SendAsync("Я тут!", "All");
                    break;
                case "Я тут!":
                    if(!newUserNames.Contains(sender))
                    {
                        newUserNames.Add(sender);
                    }
                    break;
            }
        }
        void CheckUsers()
        {
            newUserNames.Clear();
            checkTimer = new System.Timers.Timer(checkTime * 1000);
            checkTimer.Elapsed += (s, e) => 
            {
                checkTimer.Stop();
                checkTimer.Enabled = false;
                userNames = new List<string>(newUserNames);
                CheckUsersFinished?.Invoke();
                checkIntervalTimer = new System.Timers.Timer(checkInterval * 1000);
                checkIntervalTimer.Elapsed += (s, e) => 
                {
                    checkIntervalTimer.Stop();
                    checkIntervalTimer.Enabled = false;
                    CheckUsers();
                };
                checkIntervalTimer.Enabled = true;
            };
            checkTimer.Enabled = true;
            SendAsync("Кто здесь?", "All");
        }
        #endregion Privates	

        #region Public
        /// <summary>
        /// Начало прослушивания
        /// </summary>
        public void StartListener()
        {
            if (!isListenerStarted)
                Task.Run(ReceiveMessageAsync);
        }
        /// <summary>
        /// Конец прослушивания
        /// </summary>
        public void StopListener()
        {
            isListenerStarted = false;
        }
        /// <summary>
        /// Отправка сообщения
        /// </summary>
        /// <param name="msg">Сообщение</param>
        /// <param name="to">Имя пользователя, которому адресовано сообщение. Если сообщение адресованно всем, то записать как "All"</param>
        public async void SendAsync(string msg, string to)
        {
            await SendMessageAsync();
            async Task SendMessageAsync()
            {
                using var sender = new UdpClient(); // создаем UdpClient для отправки
                                                    // отправляем сообщения
                msg = $"{Environment.UserName}☻{to}☺{msg}";
                byte[] data = Encoding.UTF8.GetBytes(msg);
                // и отправляем в группу
                await sender.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Parse(Support.ConfigWorker.GetConfig(appDataFolderPath).GroupIp), Support.ConfigWorker.GetConfig(appDataFolderPath).Port));
            }
        }
        /// <summary>
        /// Вызвать механизм установки сетевых настроек, возможно задать собственно окно вместо дефолтного
        /// </summary>
        /// <param name="configWindow"> Собственное окно, если не задано, то будет использоваться дефолтное окно </param>
        public void GetConfigWindow(Window configWindow = null)
        {
            var context = new ModelViews.ConfigWindowMV(appDataFolderPath);
            Window window;
            if (configWindow == null)
            {
                window = new Views.ConfigWindowView();
            }
            else
            {
                window = configWindow;
            }
            window.DataContext = context;
            window.ShowDialog();
            if (context.is_Ok)
            {
                StopListener();
                StartListener();
            }
        }
        #endregion Public
        #endregion Methods
        #region Tasks
        async Task ReceiveMessageAsync()
        {
            isListenerStarted = true;
            using var receiver = new UdpClient(Support.ConfigWorker.GetConfig(appDataFolderPath).Port); // UdpClient для получения данных
            receiver.JoinMulticastGroup(IPAddress.Parse(Support.ConfigWorker.GetConfig(appDataFolderPath).GroupIp));
            receiver.MulticastLoopback = false; // отключаем получение своих же сообщений
            while (isListenerStarted)
            {
                var result = await receiver.ReceiveAsync();
                string message = Encoding.UTF8.GetString(result.Buffer);
                if (!string.IsNullOrEmpty(message))
                {
                    if (message.Contains("☻"))
                    {
                        int start = message.IndexOf('☻');
                        int finish = message.IndexOf('☺');
                        var receiverPerson = message.Substring(start + 1, finish - start - 1);
                        var senderPerson = message.Substring(0, start);
                        var resultMsg = message.Substring(finish + 1, message.Length - finish - 1);
                        if (receiverPerson == "All" || receiverPerson == Environment.UserName)
                        {
                            Incoming?.Invoke(senderPerson, resultMsg);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
