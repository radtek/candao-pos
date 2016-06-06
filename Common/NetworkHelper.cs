﻿using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Common
{
    /// <summary>
    /// 网络辅助类。
    /// </summary>
    public class NetworkHelper
    {
        /// <summary>
        /// 检测与某个网络地址是否连通。
        /// </summary>
        /// <param name="ipAddr">IP地址。如192.168.1.1，或者是网站地址如：www.baidu.com。</param>
        /// <returns>如果连通返回true，否则返回false。</returns>
        public static bool DetectNetworkConnection(string ipAddr)
        {
            try
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions { DontFragment = true };
                byte[] buffer = Encoding.UTF8.GetBytes("");
                var reply = pingSender.Send(ipAddr, 2000, buffer, options);//2000是响应时间（毫秒）。
                if (reply != null)
                {
                    var info = reply.Status.ToString();
                    return info.Equals("Success");
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DetectNetworkConnection(string ipAddr, int port)
        {
            try
            {
                TcpClient tcp = new TcpClient(ipAddr, port);
                tcp.GetStream();
                tcp.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}