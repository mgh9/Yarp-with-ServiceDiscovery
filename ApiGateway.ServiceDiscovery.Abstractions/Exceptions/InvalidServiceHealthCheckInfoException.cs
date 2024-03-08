namespace Yarp.ServiceDiscovery.Abstractions.Exceptions
{
    public class InvalidServiceHealthCheckInfoException : ApiGatewayException
    {
        public InvalidServiceHealthCheckInfoException() { }
        public InvalidServiceHealthCheckInfoException(string message) : base(message) { }
        public InvalidServiceHealthCheckInfoException(string message, Exception inner) : base(message, inner) { }
    }
}
