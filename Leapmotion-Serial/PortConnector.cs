using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace Leapmotion_Serial
{
    class PortConnector
    {

        private SerialPort device = new SerialPort();

        public void OpenPort(string portname)
        {
            device.PortName = portname;               // 端口名
            device.BaudRate = 60 * 66 * 8;      // 波特率：60次刷新/秒 * 66bytes/一次刷新 * 8bits/byte
            device.Parity = Parity.None;        // 奇偶校验
            device.DataBits = 8 * 8;                // 8位的数据位
            device.StopBits = StopBits.One;     // 使用1个停止位
            device.Handshake = Handshake.None;  // 握手协议：待定

            device.ReadTimeout = 500;           // 读取超时：500ms
            device.WriteTimeout = 500;          // 写入超时：500ms

            try
            {
                device.Open();
            } catch
            {
                // TO-DO:打开端口失败处理
                throw;
            }

            device.DataReceived += new SerialDataReceivedEventHandler(DataReceiveHandler);
            device.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorHandler);
        }

        public void ClosePort()
        {
            device.Close();
        }

        public bool IsPortOpen()
        {
            return device.IsOpen;
        }

        public void Write(byte[,] state)
        {
            if (!device.IsOpen)
                return;

            if (state.GetLength(0) != 8 || state.GetLength(1) != 8)
            {
                throw new Exception("Data Size not compatible!");
            }

            byte[] bytes = new byte[64];
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    bytes[i * 8 + j] = state[i, j];
                }
            }

            try
            {
                device.Write(bytes, 0, bytes.Length);
            } catch
            {
                // TO-DO:发送失败处理
                //throw;
            }
        }

        private void DataReceiveHandler(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] ReDatas = new byte[device.BytesToRead];
            device.Read(ReDatas, 0, ReDatas.Length);
            // TO-DO:获得数据之后
        }

        private void ErrorHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            // TO-DO:错误处理
        }
    }
}
