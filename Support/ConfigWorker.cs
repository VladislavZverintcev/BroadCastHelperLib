using BroadCastHelperLib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace BroadCastHelperLib.Support
{
    public class ConfigWorker
    {
        static string appDataFolderPath;
        static string configPath;
        /// <summary>
        /// Событие нужное чтобы выводить ошибки или уведомления при чтении файла конфигураций, bool - false (Негативное сообщение);
        /// </summary>
        //public static event Action<string, bool> WarningMessageComming;
        public static ConfigData GetConfig(string _appDataFolderPath)
        {
            SetAppPath(_appDataFolderPath);
            ConfigData defaultConfig = new ConfigData
            {
                GroupIp = "225.225.225.225",
                Port = 9999
            };

            if (File.Exists(configPath))
            {
                try
                {
                    TextReader tr = new StreamReader(configPath);
                    return Read(tr);
                }
                catch
                {
                    //WarningMessageComming.Invoke($"Не удалось прочитать файл конфигураций Net_config.xml!", false);
                    return defaultConfig;
                }
            }
            else
            {
                //WarningMessageComming.Invoke($"Не найден файл конфигураций Net_config.xml!", false);
                try
                {
                    
                    Write(defaultConfig, _appDataFolderPath);
                    
                    //WarningMessageComming.Invoke($"Создан файл конфигураций, требующий настройки Net_config.xml!", true);
                    return defaultConfig;
                }
                catch
                {
                    //WarningMessageComming.Invoke($"Не удалось создать файл конфигураций Net_config.xml!", false);
                    return defaultConfig;
                }
            }
        }
        public static void Write(ConfigData config, string _appDataFolderPath)
        {
            SetAppPath(_appDataFolderPath);
            TextWriter writer = new StreamWriter(configPath);
            XmlSerializer x = new XmlSerializer(typeof(ConfigData));
            x.Serialize(writer, config);
            writer.Close();
        }
        static void SetAppPath(string _appDataFolderPath)
        {
            if (string.IsNullOrEmpty(_appDataFolderPath) || string.IsNullOrWhiteSpace(_appDataFolderPath))
            {
                throw new ArgumentException();
            }
            appDataFolderPath = _appDataFolderPath;
            configPath = @$"{appDataFolderPath}\Net_config.xml";
        }
        static ConfigData Read(TextReader reader)
        {
            XmlSerializer x = new XmlSerializer(typeof(ConfigData));
            var result = (ConfigData)x.Deserialize(reader);
            reader.Close();
            return result;
        }
    }
}
