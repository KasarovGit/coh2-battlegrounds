﻿using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

using Battlegrounds.Networking.Communication;
using Battlegrounds.Networking.Server;

namespace Battlegrounds.Networking {

    /// <summary>
    /// 
    /// </summary>
    public static class NetworkingInstance {

        private static string BestAddress;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool HasLocalServer()
            => Process.GetProcessesByName("bg-server").Length > 0;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetBestAddress() {
            if (BestAddress is null) {
                Trace.WriteLine("No best address specified - will attempt to find it.");
#if DEBUG
                if (HasLocalServer()) {
                    BestAddress = "localhost";
                } else {
                    try {
                        var p = new Ping().Send("http://192.168.1.107:11000");
                        BestAddress = p.Status == IPStatus.Success ? "192.168.1.107" : "194.37.80.249";
                        //BestAddress = TcpConnection.CanConnect("192.168.1.107", 11000) ? "192.168.1.107" : "194.37.80.249";
                    } catch (Exception e) {
                        Trace.WriteLine(e, nameof(NetworkingInstance));
                        BestAddress = "194.37.80.249";
                    }
                }
#else
                BestAddress = "194.37.80.249";
#endif
                Trace.WriteLine($"Using {BestAddress} as connection address", nameof(NetworkingInstance));
            }
            return BestAddress;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static ServerAPI GetServerAPI()
            => new ServerAPI(GetBestAddress());

    }

}
