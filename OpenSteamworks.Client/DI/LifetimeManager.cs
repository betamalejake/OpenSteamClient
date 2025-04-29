using OpenSteamClient.DI;
using OpenSteamClient.DI.Attributes;
using OpenSteamClient.DI.Lifetime;
using OpenSteamClient.Logging;
using OpenSteamworks.Client.Managers;

namespace OpenSteamworks.Client.DI;

[DIRegisterInterface<ILifetimeManager>]
public class LifetimeManager : ILifetimeManager {
	private readonly IContainer _container;
	private readonly ILogger _logger;

	public LifetimeManager(IContainer container) {
		this._container = container;
		this._logger = container.Get<ILoggerFactory>().CreateLogger("LifetimeManager");
	}

	private class LifetimeObject {
		public object? Object { get; set; }
		public Type? ContainerType { get; set; }
		public Type RealType {
			get {
				if (Object != null) {
					return Object.GetType();
				} else if (ContainerType != null) {
					return ContainerType;
				}

				throw new InvalidDataException("Neither Object nor ContainerType is specified");
			}
		}

		private readonly IContainer _container;
		public LifetimeObject(IContainer container) {
			this._container = container;
		}

		public override string ToString()
		{
			if (Object != null) {
				return Object.GetType().Name;
			} else if (ContainerType != null) {
				return ContainerType.Name;
			}

			throw new InvalidDataException("Neither Object nor ContainerType is specified");
		}

		public async Task RunClientStartup(IProgress<OperationProgress> progress) {
			IClientLifetime? obj = (IClientLifetime?)Object;
			if (obj == null && ContainerType != null) {
				obj = (IClientLifetime?)_container.Get(ContainerType);
			}

			if (obj == null) {
				throw new InvalidDataException("Neither Object nor ContainerType is specified");
			}

			await obj.RunStartup(progress);
		}

		public async Task RunClientShutdown(IProgress<OperationProgress> progress) {
			IClientLifetime? obj = (IClientLifetime?)Object;
			if (obj == null && ContainerType != null) {
				obj = (IClientLifetime?)_container.Get(ContainerType);
			}

			if (obj == null) {
				throw new InvalidDataException("Neither Object nor ContainerType is specified");
			}

			await obj.RunShutdown(progress);
		}

		public async Task RunLogon(IProgress<OperationProgress> progress) {
			ILogonLifetime? obj = (ILogonLifetime?)Object;
			if (obj == null && ContainerType != null) {
				obj = (ILogonLifetime?)_container.Get(ContainerType);
			}

			if (obj == null) {
				throw new InvalidDataException("Neither Object nor ContainerType is specified");
			}

			await obj.RunLogon(progress);
		}

		public async Task RunLogoff(IProgress<OperationProgress> progress) {
			ILogonLifetime? obj = (ILogonLifetime?)Object;
			if (obj == null && ContainerType != null) {
				obj = (ILogonLifetime?)_container.Get(ContainerType);
			}

			if (obj == null) {
				throw new InvalidDataException("Neither Object nor ContainerType is specified");
			}

			await obj.RunLogoff(progress);
		}
	}

	private readonly List<LifetimeObject> _clientLifetimeOrder = new();
    private readonly List<LifetimeObject> _logonLifetimeOrder = new();
	private bool _hasRanStartup;
	public bool IsShuttingDown { get; private set; }

	public void RegisterForClientLifetime(IClientLifetime obj) {
        // This function also gets called when the item is constructed and registered, so avoid double lifetime registration
        if (_clientLifetimeOrder.Any(a => a.Object == obj))
            return;

		_clientLifetimeOrder.Add(new(_container) { Object = obj });
		_logger.Debug("Registered factory of type '" + obj.GetType().Name + "' for client lifetime at index " + this._clientLifetimeOrder.Count);
	}

	public void RegisterForLogonLifetime(ILogonLifetime obj)
    {
        // This function also gets called when the item is constructed and registered, so avoid double lifetime registration
        if (_logonLifetimeOrder.Any(a => a.Object == obj))
            return;

		_logonLifetimeOrder.Add(new(_container) { Object = obj });
		_logger.Debug("Registered factory of type '" + obj.GetType().Name + "' for logon lifetime at index " + this._logonLifetimeOrder.Count);
	}

	public void RegisterContainerType(Type type) {
		if (type.IsAssignableTo(typeof(IClientLifetime)))
		{
            // This function also gets called when the item is constructed and registered, so avoid double lifetime registration
            if (_clientLifetimeOrder.All(a => a.ContainerType != type))
            {
                _clientLifetimeOrder.Add(new(_container) { ContainerType = type });
                _logger.Debug("Registered factory of type '" + type.Name + "' for client lifetime at index " + this._clientLifetimeOrder.Count);
            }
		}

		if (type.IsAssignableTo(typeof(ILogonLifetime)))
		{
            // This function also gets called when the item is constructed and registered, so avoid double lifetime registration
            if (_logonLifetimeOrder.All(a => a.ContainerType != type))
            {
                _logonLifetimeOrder.Add(new(_container) { ContainerType = type });
                _logger.Debug("Registered factory of type '" + type.Name + "' for logon lifetime at index " + this._logonLifetimeOrder.Count);
            }
		}
	}

	private readonly SemaphoreSlim _clientLifetimeLock = new(1, 1);
	public async Task RunClientStartup(IProgress<OperationProgress> progress)
    {
		await _clientLifetimeLock.WaitAsync();
		try
		{
			foreach (var component in _clientLifetimeOrder.ToList())
			{
				_logger.Info("Running startup for " + component.ToString());
				await component.RunClientStartup(progress);
				_logger.Info("Startup for " + component.ToString() + " finished");
			}

			_hasRanStartup = true;
		}
		finally
		{
			_clientLifetimeLock.Release();
		}
    }

    public async Task RunClientShutdown(IProgress<OperationProgress> progress)
    {
		await _clientLifetimeLock.WaitAsync();
        IsShuttingDown = true;

		try
		{
			if (!_hasRanStartup)
			{
				return;
			}

			_hasRanStartup = false;
			foreach (var component in _clientLifetimeOrder.ToList())
			{
				_logger.Info("Running shutdown for " + component.ToString());
				await component.RunClientShutdown(progress);
				_logger.Info("Shutdown for " + component.ToString() + " finished");
			}
		}
		finally
		{
			_clientLifetimeLock.Release();
		}
    }

	private readonly SemaphoreSlim _logonLock = new(1, 1);
    public async Task RunLogon(IProgress<OperationProgress> progress)
    {
		await _logonLock.WaitAsync();
		try
		{
			foreach (var component in _logonLifetimeOrder.ToList())
			{
				_logger.Info("Running logon for component " + component.ToString());
				await component.RunLogon(progress);
                _logger.Info("Login for " + component.ToString() + " finished");
			}
		}
		finally
		{
			_logonLock.Release();
		}
    }

    public async Task RunLogoff(IProgress<OperationProgress> progress)
    {
		await _logonLock.WaitAsync();
		try
		{
			foreach (var component in _logonLifetimeOrder.ToList())
			{
				_logger.Info("Running logoff for component " + component.ToString());
				await component.RunLogoff(progress);
				_logger.Info("Logoff for component " + component.ToString() + " finished");
			}
		}
		finally
		{
			_logonLock.Release();
		}
    }

	public bool IsClientLifetimeRegistered(Type type)
		=> _clientLifetimeOrder.Any(p => p.RealType == type);

	public bool IsLogonLifetimeRegistered(Type type)
		=> _logonLifetimeOrder.Any(p => p.RealType == type);
}
