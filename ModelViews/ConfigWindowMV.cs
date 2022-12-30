using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace BroadCastHelperLib.ModelViews
{
    public class ConfigWindowMV : Base.ModelView
    {

        #region Fields
        string ip;
        int port;
        string message;
        private System.Timers.Timer msgTimer;
        public string AppDataFolderPath { get; set; }

        public bool is_Ok = false;
        #endregion Fields
        #region Properties
        public string Ip
        {
            get => ip;
            set => Set(ref ip, value);
        }
        public int Port
        {
            get => port;
            set => Set(ref port, value);
        }
        public string Message
        {
            get => message;
            set => Set(ref message, value);
        }
        #endregion Properties
        #region Constructors
        public ConfigWindowMV(string _appDataFolderPath)
        {
            if(string.IsNullOrEmpty(_appDataFolderPath) || string.IsNullOrWhiteSpace(_appDataFolderPath))
            {
                throw new ArgumentNullException();
            }
            AppDataFolderPath = _appDataFolderPath;
            var config = Support.ConfigWorker.GetConfig(AppDataFolderPath);
            Ip = config.GroupIp;
            Port = config.Port;
            //Support.ConfigWorker.WarningMessageComming += (m, b) => SetMessage(m, b);
        }
        public ConfigWindowMV()
        {
            //Support.ConfigWorker.WarningMessageComming += (m, b) => SetMessage(m, b);
        }
        #endregion Constructors
        #region Methods

        #region Privates
        void SetMessage(string msg, bool positive)
        {
            Message = msg;
            SetmsgTimer();
            void SetmsgTimer()
            {
                msgTimer = new System.Timers.Timer(5000);
                msgTimer.Elapsed += OnTimedEvent;
                msgTimer.Enabled = true;
            }
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Message = string.Empty;
            msgTimer.Stop();
            msgTimer.Enabled = false;
            //try
            //{
            //Dispatcher.FromThread(ownerThread)?.Invoke(() => SomeMethod());
            //}
            //catch(System.Threading.Tasks.TaskCanceledException)
            //{ return; }
        }
        bool IsResultOk()
        {
            try
            {
                var nip = IPAddress.Parse(Ip);
            }
            catch (Exception)
            {

                return false;
            }
            if(Port <= 0 || Port > 9999)
            {
                return false;
            }
            return true;

        }
        #endregion Privates	

        #region Public

        #endregion Public

        #endregion Methods
        #region Commands
        public ICommand OkComm
        {
            get
            {
                return new Commands.VMCommands(parameter =>
                {
                    Models.ConfigData netCfg = new Models.ConfigData
                    {
                        GroupIp = Ip,
                        Port = this.Port
                    };
                    Support.ConfigWorker.Write(netCfg, AppDataFolderPath);
                    if (parameter is Window)
                        ((Window)parameter).Close();
                    is_Ok = true;
                }, (obj) => IsResultOk());
            }
        }
        #endregion
    }
}
