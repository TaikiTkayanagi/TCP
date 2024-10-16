using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

namespace TCP
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 使用可能なネットワークデバイスを取得
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            if (allDevices.Count == 0)
            {
                Console.WriteLine("No devices found on this machine.");
                return;
            }

            for (int i = 0; i < allDevices.Count; i++)
            {
                LivePacketDevice device = allDevices[i];
                Console.WriteLine($"{i}: {device.Description}");
                foreach (DeviceAddress address in device.Addresses)
                {
                    if (address.Address.Family == SocketAddressFamily.Internet)
                    {
                        Console.WriteLine($"  IP: {address.Address}");
                    }
                }
            }

            // 使用するデバイスを選択
            PacketDevice selectedDevice = allDevices[4];  // 1つ目のデバイスを選択

            using (PacketCommunicator communicator = selectedDevice.Open(100, PacketDeviceOpenAttributes.Promiscuous, 1000))
            {
                // Ethernetレイヤー（必須）
                EthernetLayer ethernetLayer = new EthernetLayer
                {
                    EtherType = EthernetType.None,  // 自動でEtherTypeを決定
                };

                // IPレイヤー（TCPパケットを送信するためには必要）
                IpV4Layer ipV4Layer = new IpV4Layer
                {
                    Source = new IpV4Address(""),  // 適当な送信元IPアドレス
                    CurrentDestination = new IpV4Address(""),  // 送信先のIPアドレス
                    Ttl = 128,
                    Protocol = IpV4Protocol.Tcp,  // TCPプロトコル
                };

                // TCPレイヤー（ここをカスタマイズ）
                TcpLayer tcpLayer = new TcpLayer
                {
                    SourcePort = 12345,  // 送信元ポート
                    DestinationPort = 443,  // 送信先ポート
                    SequenceNumber = 1000,
                    AcknowledgmentNumber = 0,
                    ControlBits = TcpControlBits.Synchronize,  // SYNフラグをセット
                    Window = 8192,
                    UrgentPointer = 0,
                    Options = TcpOptions.None,
                };

                // パケットのビルド
                PacketBuilder builder = new PacketBuilder(ethernetLayer, ipV4Layer, tcpLayer);
                Packet packet = builder.Build(DateTime.Now);

                // パケットを送信
                communicator.SendPacket(packet);

                Console.WriteLine("TCP SYNパケットを送信しました。");
            }
        }
    }
}
