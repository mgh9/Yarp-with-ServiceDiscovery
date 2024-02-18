using System.Collections;
using ApiGateway.ServiceDiscovery.Abstractions;
using Swashbuckle.AspNetCore.SwaggerUI;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Extensions
{
    public class SwaggerEndpointEnumerator : IEnumerable<UrlDescriptor>
    {
        private readonly IServiceDiscovery _serviceDiscovery;

        public SwaggerEndpointEnumerator(IServiceDiscovery serviceDiscovery)
        {
            this._serviceDiscovery = serviceDiscovery;
        }

        public IEnumerator<UrlDescriptor> GetEnumerator()
        {
            var clusters = _serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();

            yield return new UrlDescriptor {  Name = "Your swagger name 1 here", Url = "https://localhost:7094/swagger/v1/swagger.json" };
            yield return new UrlDescriptor {  Name = "Your swagger name 2 here", Url = "https://localhost:7005/swagger/v1/swagger.json" };
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
