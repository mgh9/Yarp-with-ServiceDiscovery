namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions.Exceptions
{
    public class ApiGatewayException : Exception
    {
        public ApiGatewayException() { }
        public ApiGatewayException(string message) : base(message) { }
        public ApiGatewayException(string message, Exception inner) : base(message, inner) { }
    }
}
