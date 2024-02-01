using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace CIB.Core.Utils
{
    public static class AddressHelper
    {
        public static string GetMacAddress()
        {
            var macAddr = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                           where nic.OperationalStatus == OperationalStatus.Up
                           select nic.GetPhysicalAddress().ToString()).FirstOrDefault();
            return macAddr;
        }
    }
}