﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RemoteWork.Expect
{
    //класс Telnet другая имплементация
    //ожидает возврата требуемой строки с указанным таймаутом для каждой строки
    public class TelnetMint
    {
        TcpClient _client;//клиент
        IPEndPoint _remote;//данные о подключении
        NetworkStream _networkStream;//поток
        int _buffSize = 256;//буфер
        public bool _isConnected = true;
        //требуется ли? если строка не соответсвует, то выкидывать исключение???
        public bool _isReceiveWordHasMatched = false;
        public string ENTER
        {
            get
            {
                return "\r\n";
            }
        }//возврат значения клавиши Enter
        //конструктор с параметрами 
        public TelnetMint(string host, int port)
        {
            if (port < 1 || port > 65536)
            {
                //проверка валидности порта
                throw new Exception("Invalid port!");
            }
            IPAddress ip = System.Net.Dns.GetHostAddresses(host)[0];
            _remote = new IPEndPoint(ip, port);
            _client = new TcpClient();
            //подключаемся
            try
            {
                _client.Connect(_remote);
                _networkStream = _client.GetStream();
            }
            catch (Exception)
            {
                //указываем состояние клиента, подключено или нет
                _isConnected=_client.Connected;
            }

        }
        //конструктор по умолчанию
        public TelnetMint(string host)
            : this(host, 23)
        {
        }
        //закрыть подключение клиента
        public void Close()
        {
            _client.Close();
        }
        //деструктор
         ~TelnetMint()
        {
            _client.Close(); 
        }     
        //отправка сообщения      
        public void SendData(string command)
        {
            if (!command.EndsWith(ENTER))
                command += ENTER;
            byte[] data = System.Text.Encoding.Default.GetBytes(command);
            _networkStream.Write(data, 0, data.Length);
        }
       //вернуть информацию, если строка имеет совпадения по паттерну регулярных выражений за промежуток времени
        public string ReceiveDataWaitWord(Regex msg, int second)
        {
            StringBuilder result = new StringBuilder(_buffSize);
            string temp = string.Empty;
            DateTime current = DateTime.Now.AddSeconds(second); //DateTime p = DateTime.Now.AddMilliseconds(second); уменьшение интервала
            do
            {
                temp = ReceiveData();//принимаем данные
                result.Append(temp);//добавляем принятые данные в строку
                if (msg.Match(temp).Success)//если паттерн сравнения работает
                {
                    //_isReceiveWordHasMatched = true;//указываем, что мы получили ожидаемую строку
                    return result.ToString();//возвращаем результат
                }

            } while (DateTime.Now < current);
            //возвращаем полученный результат, который не совпадает с ожидаемой строкой
            return result.ToString();
        }
        //вернуть информацию, если строка имеет совпадения по строке за промежуток времени
        public string ReceiveDataWaitWord(string msg, int second)
        {
            StringBuilder result = new StringBuilder(_buffSize);
            string temp = string.Empty;
            DateTime current = DateTime.Now.AddSeconds(second);
            do
            {
                temp = ReceiveData();
                result.Append(temp);
                if (temp.TrimEnd().EndsWith(msg))//если строка оканчивается на ожидаемую строку
                {
                   // _isReceiveWordHasMatched = true;//указываем, что мы получили ожидаемую строку
                    return result.ToString();//возвращаем строку
                }

            } while (DateTime.Now < current);
            //возвращаем полученный результат, который не совпадает с ожидаемой строкой
            return result.ToString();
        }
        //вернуть полученные данные
        public string ReceiveData()
        {
            byte[] tempData = new byte[_buffSize];
            List<byte> data = new List<byte>();
            int count = 0;
            do
            {
                if (!_networkStream.DataAvailable)//пока подключение доступно
                {
                    //если нет данных вернуть пустую строку
                    return "";
                }
                count = _networkStream.Read(tempData, 0, tempData.Length);
                data.AddRange(tempData.Take(count));//добавляем данные 
            } while (count == _buffSize);
            return Negotiate(data.ToArray()); //возвращаем
        }
        //обработать корректность данных???
        string Negotiate(byte[] data)
        {
            List<byte> sendData = new List<byte>();
            for (int i = 0; i < data.Length; i += 3)
            {
                if (data[i] == 255)
                {
                    byte[] remoteData = data.Skip(i).Take(3).ToArray();//обрабатываем данные
                    for (int j = 0; j < remoteData.Length; j++)
                    {
                        if (remoteData[j] == 253)
                            remoteData[j] = 252;
                    }
                    sendData.AddRange(remoteData);//добавляем данные 
                }
                else
                {
                    break;
                }
            }
            byte[] sendByte = sendData.ToArray();
            _networkStream.Write(sendByte, 0, sendByte.Length);
            if (sendByte.Length == data.Length)
            {
                //вернуть полученные данные
                return ReceiveData();
            }
            return System.Text.Encoding.Default.GetString(data.Skip(sendByte.Length).ToArray());

        }
    }
}
