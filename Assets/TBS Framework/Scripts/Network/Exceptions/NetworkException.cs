using System;

namespace TbsFramework.Network.Exceptions
{
    public class NetworkException : Exception
    {
        public NetworkException(string message) : base(message) { }
    }
}

