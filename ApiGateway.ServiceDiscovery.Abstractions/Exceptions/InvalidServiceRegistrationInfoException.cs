namespace Yarp.ServiceDiscovery.Abstractions.Exceptions
{
    public class InvalidServiceRegistrationInfoException : ApiGatewayException
    {
        public InvalidServiceRegistrationInfoException() { }
        public InvalidServiceRegistrationInfoException(string message) : base(message) { }
        public InvalidServiceRegistrationInfoException(string message, Exception inner) : base(message, inner) { }
    }
}
