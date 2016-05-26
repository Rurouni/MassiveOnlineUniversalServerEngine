using System;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using MOUSE.Core.Logging;
using Owin;

namespace Core.Azure.Web
{
    public class OwinCommunicationListener : ICommunicationListener
    {
        readonly ILog _logger = LogProvider.GetLogger("OwinSelfHost");
        readonly Action<IAppBuilder> _startup;
        readonly ServiceContext _serviceContext;
        readonly string _endpointName;
        readonly string _appRoot;

        IDisposable _webApp;
        string _publishAddress;
        string _listeningAddress;

        public OwinCommunicationListener(Action<IAppBuilder> startup, ServiceContext serviceContext, string endpointName)
            : this(startup, serviceContext, endpointName, null)
        {
        }

        public OwinCommunicationListener(Action<IAppBuilder> startup, ServiceContext serviceContext, string endpointName, string appRoot)
        {
            if (startup == null)
            {
                throw new ArgumentNullException(nameof(startup));
            }

            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }

            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

            _startup = startup;
            _serviceContext = serviceContext;
            _endpointName = endpointName;
            _appRoot = appRoot;
        }

        public bool ListenOnSecondary { get; set; }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var serviceEndpoint = _serviceContext.CodePackageActivationContext.GetEndpoint(_endpointName);
            var port = serviceEndpoint.Port;

            if (_serviceContext is StatefulServiceContext)
            {
                var statefulServiceContext = _serviceContext as StatefulServiceContext;

                _listeningAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}{2}/{3}/{4}",
                    port,
                    string.IsNullOrWhiteSpace(_appRoot)
                        ? string.Empty
                        : _appRoot.TrimEnd('/') + '/',
                    statefulServiceContext.PartitionId,
                    statefulServiceContext.ReplicaId,
                    Guid.NewGuid());
            }
            else if (_serviceContext is StatelessServiceContext)
            {
                _listeningAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}",
                    port,
                    string.IsNullOrWhiteSpace(_appRoot)
                        ? string.Empty
                        : _appRoot.TrimEnd('/') + '/');
            }
            else
            {
                throw new InvalidOperationException();
            }

            _publishAddress = _listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            try
            {
                _logger.Info("Starting web server on " + _listeningAddress);

                _webApp = WebApp.Start(_listeningAddress, appBuilder => _startup.Invoke(appBuilder));

                _logger.Info("Listening on " + _publishAddress);

                return Task.FromResult(_publishAddress);
            }
            catch (Exception ex)
            {
                _logger.Error("Web server failed to open. " + ex);

                StopWebServer();

                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Closing web server");

            StopWebServer();

            return Task.FromResult(true);
        }

        public void Abort()
        {
            _logger.Info("Aborting web server");

            StopWebServer();
        }

        private void StopWebServer()
        {
            if (_webApp != null)
            {
                try
                {
                    _webApp.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // no-op
                }
            }
        }
    }
}
