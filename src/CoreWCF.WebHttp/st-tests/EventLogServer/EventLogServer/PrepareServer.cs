// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;

using Castle.DynamicProxy;

using ST.Core;
using ST.EventLog.Server;
using ST.Utils;
using ST.Utils.Attributes;
using ST.Utils.Config;
using ST.Utils.Threading;

namespace ST.Core
{
    public partial class BaseServer : IBaseServer
    {
        private const int c_EventLog_Max_Event_Message_Chars = ST.Utils.Constants.EVENTLOG_MAXEVENTMESSAGEBYTES / 2;

        private static BaseServer _instance;
        private static object _instanceLock = new object();

        protected static BaseServer ServerInstance
        {
            get { return _instance; }
        }

        /// <summary>
        /// Тип сервера.
        /// </summary>
        protected virtual ServerType ServerType
        {
            get { return ServerType.ApplicationServer; }
        }

        private static readonly FieldInfo _serverField = typeof(BaseModule).GetField("_", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

        private readonly Type[] _supportedInterfaces;

        private readonly List<BaseModule> _modules = new List<BaseModule>();
        private ReadOnlyCollection<BaseModule> _modulesRO = new ReadOnlyCollection<BaseModule>(new BaseModule[0]);

        internal static bool UseDefaultConfig { get; set; }

        [ThreadStatic]
        [ThreadStaticContext]
        private static BaseModule _callerModule;

        /// <summary>
        /// Текущий модуль - модуль, который в данный момент обращается к серверу.
        /// </summary>
        protected BaseModule CallerModule
        {
            get { return _callerModule; }
        }

        /// <summary>
        /// Список загруженных модулей.
        /// </summary>
        protected ReadOnlyCollection<BaseModule> Modules
        {
            get { return _modulesRO; }
        }


        public BaseServer()
        {
            SetInstance(this);

            _supportedInterfaces = GetType().GetInterfaces().Where(i => i.IsDefined<ServerInterfaceAttribute>() && i != typeof(IBaseServer)).ToArray();

            ThreadPool.SetMinThreads(4, 4);
            ThreadPool.SetMaxThreads(128, 128);
        }

        #region .Dtor
        ~BaseServer()
        {
            SetInstance(null);
        }
        #endregion

        public TService GetService<TService>() where TService : BaseModule
        {
            Type type = typeof(TService);

            var module = (BaseModule)type.CreateFast();

            _serverField.SetValue(module, _proxyGenerator.CreateInterfaceProxyWithTargetInterface(typeof(IBaseServer), _supportedInterfaces, this, new ServerInterceptor(module)));

            OnModuleInitializing(module);

            AddModule(module);

            if (!Task.Factory.StartNew(obj => (obj as ThreadStaticContext)?.Execute(() => module.Initialize()), ThreadStaticContext.Capture(), TaskCreationOptions.PreferFairness).Wait(module.GetInitializeTimeout() ?? GetOperationTimeout()))
                throw new Exception("### module.Initialize()");

            OnModuleInitialized(module);

            module.PostInitialize();

            return (TService)module;

            //throw new NotImplementedException();
        }

        private void OnModuleInitializing(BaseModule module)
        {
            //if (module is ISubscriber)
            //    _subscriptions.Add(module, new Subscription(module as ISubscriber, e => WriteToLog(e)));
        }


        #region OnModuleInitialized
        /// <summary>
        /// Вызывается после инициализации модуля.
        /// </summary>
        /// <param name="module">Модуль.</param>
        protected virtual void OnModuleInitialized(BaseModule module)
        {
            var type = module.GetType();

            if (type.IsDefined<ConfigurableAttribute>())
            {
                SetModuleConfig(type, ServerType.ApplicationServer, GetModuleConfig(type, ServerType.ApplicationServer));
            }
        }
        #endregion

        #region AddModule
        private void AddModule(BaseModule module)
        {
            _modules.Add(module);

            UpdateModules();
        }
        #endregion

        #region UpdateModules
        private void UpdateModules()
        {
            _modulesRO = new ReadOnlyCollection<BaseModule>(_modules.ToArray()); // Копирование коллекции здесь обязательно.
        }
        #endregion

        #region GetOperationTimeout
        /// <summary>
        /// Получает заданный тайм-аут операции.
        /// </summary>
        protected virtual int GetOperationTimeout()
        {
#if DEBUG
            return Timeout.Infinite;
#else
            return 90000;
#endif
        }
        #endregion

        #region GetModuleConfig
        /// <summary>
        /// Возвращает конфигурацию модуля.
        /// </summary>
        /// <param name="moduleType">Тип модуля.</param>
        /// <param name="serverType">Тип сервера.</param>
        /// <returns>Конфигурация модуля.</returns>
        public static ModuleConfig GetModuleConfig([NotNull] Type moduleType, ServerType serverType)
        {
            var configType = moduleType.GetAttribute<ConfigurableAttribute>();

            if (configType == null)
                return null;

            ModuleConfig config;

            if (UseDefaultConfig)
                config = configType.ConfigType.CreateFast() as ModuleConfig;
            else
            {
                config = ConfigController.Get<ModuleConfig>(GetModuleConfigRootPath(moduleType, serverType), GetModuleConfigSubPath(moduleType, serverType), null);

                if (config == null || config.GetType() != configType.ConfigType)
                    config = configType.ConfigType.CreateFast() as ModuleConfig;
            }

            return config;
        }
        #endregion


        #region SetModuleConfig
        /// <summary>
        /// Устанавливает конфигурацию модуля.
        /// </summary>
        /// <param name="moduleType">Тип модуля.</param>
        /// <param name="serverType">Тип сервера.</param>
        /// <param name="config">Конфигурация модуля.</param>
        public static void SetModuleConfig([NotNull] Type moduleType, ServerType serverType, [NotNull] ModuleConfig config)
        {
            ConfigController.Set(config, GetModuleConfigRootPath(moduleType, serverType), GetModuleConfigSubPath(moduleType, serverType), null);
        }
        #endregion

        #region GetModuleConfigPath
        /// <summary>
        /// Возвращает корневой путь, по которому хранится конфигурация модуля.
        /// </summary>
        /// <param name="moduleType">Тип модуля.</param>
        /// <param name="serverType">Тип сервера.</param>
        /// <returns>Путь реестра.</returns>
        public static string GetModuleConfigRootPath([NotNull] Type moduleType, ServerType serverType)
        {
            return (serverType == ServerType.ApplicationServer ? Constants.SERVER_REGISTRY_ROOT_PATH : Constants.SHELL_REGISTRY_ROOT_PATH);
        }

        /// <summary>
        /// Возвращает относительный путь раздела, по которому хранится конфигурация модуля.
        /// </summary>
        /// <param name="moduleType">Тип модуля.</param>
        /// <param name="serverType">Тип сервера.</param>
        /// <returns>Путь реестра.</returns>
        public static string GetModuleConfigSubPath([NotNull] Type moduleType, ServerType serverType)
        {
            return (serverType == ServerType.ApplicationServer ? Constants.SERVER_MODULES_REGISTRY_PATH : Constants.SHELL_MODULES_REGISTRY_PATH) + "\\" + moduleType.FullName;
        }
        #endregion

        #region IsServiceSupported
        private static bool IsServiceSupported<T>()
        {
            var t = typeof(T);

            return t.IsInterface && !t.IsDefined<NotServiceInterfaceAttribute>() && t != typeof(IServiceProvider) && t != typeof(IContextServiceProvider);
        }
        #endregion

        #region GetModuleParameter
        /// <summary>
        /// Возвращает значение параметра модуля.
        /// </summary>
        /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
        /// <param name="moduleType">Тип модуля.</param>
        /// <param name="serverType">Тип сервера.</param>
        /// <param name="name">Название параметра.</param>
        /// <returns>Значение параметра.</returns>
        public static T GetModuleParameter<T>([NotNull] Type moduleType, ServerType serverType, [NotNullNotEmpty] string name)
        {
            return GetParameter<T>(GetModuleConfigRootPath(moduleType, serverType), GetModuleConfigSubPath(moduleType, serverType), name);
        }
        #endregion

        #region GetParameter
        /// <summary>
        /// Возвращает значение параметра сервера.
        /// </summary>
        /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
        /// <param name="serverType">Тип сервера.</param>
        /// <param name="name">Название параметра.</param>
        /// <returns>Значение параметра.</returns>
        public static T GetParameter<T>(ServerType serverType, [NotNullNotEmpty] string name)
        {
            return GetParameter<T>(GetConfigRootPath(serverType), GetConfigSubPath(serverType), name);
        }

        private static T GetParameter<T>(string rootPath, string subPath, string name)
        {
            return ConfigController.GetValue<T>(rootPath, subPath, name);
        }
        #endregion

        #region GetConfigPath
        /// <summary>
        /// Возвращает корнево путь, по которому хранится конфигурация сервера.
        /// </summary>
        /// <param name="serverType">Тип сервера.</param>
        /// <returns>Путь реестра.</returns>
        public static string GetConfigRootPath(ServerType serverType)
        {
            return serverType == ServerType.ApplicationServer ? Constants.SERVER_REGISTRY_ROOT_PATH : Constants.SHELL_REGISTRY_ROOT_PATH;
        }

        /// <summary>
        /// Возвращает относительный путь раздела, по которому хранится конфигурация сервера.
        /// </summary>
        /// <param name="serverType">Тип сервера.</param>
        /// <returns>Путь реестра.</returns>
        public static string GetConfigSubPath(ServerType serverType)
        {
            return serverType == ServerType.ApplicationServer ? Constants.SERVER_REGISTRY_SUB_PATH : Constants.SHELL_REGISTRY_SUB_PATH;
        }
        #endregion

        #region SetModuleParameter
        /// <summary>
        /// Устанавливает значение параметра модуля.
        /// </summary>
        /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
        /// <param name="moduleType">Тип модуля.</param>
        /// <param name="serverType">Тип сервера.</param>
        /// <param name="name">Название параметра.</param>
        /// <param name="value">Значение параметра. Если null, то значение будет удалено.</param>
        public static void SetModuleParameter<T>([NotNull] Type moduleType, ServerType serverType, [NotNullNotEmpty] string name, T value)
        {
            SetParameter(GetModuleConfigRootPath(moduleType, serverType), GetModuleConfigSubPath(moduleType, serverType), name, value);
        }
        #endregion

        #region SetParameter
        /// <summary>
        /// Устанавливает значение параметра сервера.
        /// </summary>
        /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
        /// <param name="serverType">Тип сервера.</param>
        /// <param name="name">Название параметра.</param>
        /// <param name="value">Значение параметра. Если null, то значение будет удалено.</param>
        public static void SetParameter<T>(ServerType serverType, [NotNullNotEmpty] string name, T value)
        {
            SetParameter(GetConfigRootPath(serverType), GetConfigSubPath(serverType), name, value);
        }

        private static void SetParameter<T>(string rootPath, string subPath, string name, T value)
        {
            ConfigController.SetValue(value, rootPath, subPath, name);
        }
        #endregion

        #region WriteToLog
        /// <summary>
        /// Записывает сообщение в журнал событий сервера, без заголовка, включающего имя модуля, обрезая сообщение до c_EventLog_Max_Event_Message_Chars символов.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <param name="type">Тип сообщения.</param>
        protected void WriteToLogWithoutModuleHeader([NotNullNotEmpty, Localized] string message, ushort eventId, EventLogEntryType type = EventLogEntryType.Error)
        {
            Exec.Try(() => Console.WriteLine($"{type} | {eventId} | {message.TrimLength(c_EventLog_Max_Event_Message_Chars)}"));
        }

        /// <summary>
        /// Записывает сообщение в журнал событий сервера, обрезая сообщение до c_EventLog_Max_Event_Message_Chars символов.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <param name="eventId">Идентификатор события.</param>
        /// <param name="type">Тип события.</param>
        protected void WriteToLog([NotNullNotEmpty, Localized] string message, ushort eventId, EventLogEntryType type = EventLogEntryType.Error)
        {
            WriteToLogWithoutModuleHeader(CallerModule?.GetDisplayName() ?? String.Empty + Environment.NewLine + Environment.NewLine + message, eventId, type);
        }


        /// <summary>
        /// Записывает сообщение в журнал событий сервера. Если сообщение получится длиной более c_EventLog_Max_Event_Message_Chars, то сообщение делится на части, и в заголовке каждого соощнения указывается номер части и полное количество частей.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <param name="eventId">Идентификатор события.</param>
        /// <param name="type">Тип события.</param>
        protected void WriteToLogMultiple([NotNullNotEmpty, Localized] string message, ushort eventId, EventLogEntryType type = EventLogEntryType.Error)
        {
            var prefix = CallerModule?.GetDisplayName() ?? String.Empty + Environment.NewLine;

            Exec.Try(() =>
            {
                var messageToStore = prefix + Environment.NewLine + message;
                if (messageToStore.Length <= c_EventLog_Max_Event_Message_Chars)
                {
                    WriteToLogWithoutModuleHeader(messageToStore, eventId, type);
                    return;
                }

                var messages = message.SplitByLength(c_EventLog_Max_Event_Message_Chars).ToList();
                var messagePartPrefixLen = ("ModuleLogMessageBig" + Environment.NewLine).Length;
                messages = message.SplitByLength(c_EventLog_Max_Event_Message_Chars - (prefix.Length + messagePartPrefixLen + Environment.NewLine.Length + 10)).ToList();
                messages = messages.Select((m, i) => prefix + "ModuleLogMessageBig" + Environment.NewLine + m).ToList();
                foreach (var msg in messages)
                    WriteToLogWithoutModuleHeader(msg, eventId, type);
            });
        }

        /// <summary>
        /// Записывает информацию об исключении в журнал событий сервера.
        /// </summary>
        /// <param name="exc">Исключение.</param>
        /// <param name="eventId">Идентификатор события.</param>
        /// <param name="includeStackTrace">Признак того, что в текст исключения необходимо включить трассировку стэка исключения.</param>
        protected void WriteToLog([NotNull] Exception exc, ushort eventId = LogEventId.UnhandledException, bool includeStackTrace = true)
        {
            WriteToLog(exc.GetFullMessage(false, includeStackTrace), exc is ServerActionException ? (exc as ServerActionException).LogEventId : eventId);
        }

        /// <summary>
        /// Записывает информацию об исключении в журнал событий сервера.
        /// </summary>
        /// <param name="exc">Исключение.</param>
        /// <param name="title">Поясняющая информация об исключении.</param>
        /// <param name="eventId">Идентификатор события.</param>
        /// <param name="includeStackTrace">Признак того, что в текст исключения необходимо включить трассировку стэка исключения.</param>
        protected void WriteToLog([NotNull] Exception exc, string title, ushort eventId = LogEventId.UnhandledException, bool includeStackTrace = true)
        {
            WriteToLog(title + Environment.NewLine + Environment.NewLine + exc.GetFullMessage(false, includeStackTrace), exc is ServerActionException ? (exc as ServerActionException).LogEventId : eventId);
        }

        #endregion

        #region IBaseServer
        T IBaseServer.GetConfiguration<T>()
        {
            return GetModuleConfig(CallerModule.GetType(), ServerType) as T;
        }
        T IBaseServer.GetParameter<T>([NotNullNotEmpty] string name)
        {
            return GetModuleParameter<T>(CallerModule.GetType(), ServerType, name);
        }
        T IBaseServer.GetService<T>()
        {
            if (IsServiceSupported<T>())
                foreach (var m in Modules)
                    if (!ModuleComparer.Instance.Equals(m, CallerModule))
                        if (m is T)
                            return _proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(m as T, new ServiceInterceptor(CallerModule)) as T;
                        else
                          if (m is IServiceProvider)
                        {
                            var service = (m as IServiceProvider).GetService(typeof(T));

                            if (service != null)
                                return _proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(service as T, new ServiceImplicitInterceptor(CallerModule, m)) as T;
                        }

            return null;
        }
        T IBaseServer.GetServerInterface<T>()
        {
            return this as T;
        }
        T IBaseServer.GetService<T>(ProviderContext context)
        {
            if (IsServiceSupported<T>())
                foreach (var m in Modules)
                    if (!ModuleComparer.Instance.Equals(m, CallerModule) && m is IContextServiceProvider)
                    {
                        var service = (m as IContextServiceProvider).GetService<T>(context);

                        if (service != null)
                            return _proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(service as T, new ServiceImplicitInterceptor(CallerModule, m)) as T;
                    }

            return null;
        }
        T[] IBaseServer.GetServices<T>()
        {
            var services = new List<T>();

            if (IsServiceSupported<T>())
                foreach (var m in Modules)
                    if (!ModuleComparer.Instance.Equals(m, CallerModule))
                    {
                        if (m is T)
                            services.Add(_proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(m as T, new ServiceInterceptor(CallerModule)) as T);

                        if (m is IServiceProvider)
                        {
                            var service = (m as IServiceProvider).GetService(typeof(T));

                            if (service != null)
                                services.Add(_proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(service as T, new ServiceImplicitInterceptor(CallerModule, m)) as T);
                        }
                    }

            return services.ToArray();
        }
        T[] IBaseServer.GetServices<T>(ProviderContext context)
        {
            var services = new List<T>();

            if (IsServiceSupported<T>())
                foreach (var m in Modules)
                    if (!ModuleComparer.Instance.Equals(m, CallerModule) && m is IContextServiceProvider)
                    {
                        var service = (m as IContextServiceProvider).GetService<T>(context);

                        if (service != null)
                            services.Add(_proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(service as T, new ServiceImplicitInterceptor(CallerModule, m)) as T);
                    }

            return services.ToArray();
        }
        void IBaseServer.SetConfiguration(ModuleConfig config)
        {
            SetModuleConfig(CallerModule.GetType(), ServerType, config);
        }
        void IBaseServer.SetParameter<T>([NotNullNotEmpty] string name, [NotNull] T value)
        {
            SetModuleParameter<T>(CallerModule.GetType(), ServerType, name, value);
        }
        void IBaseServer.WriteToLog(string message, ServerLogEntryType type)
        {
            WriteToLog(message, (ushort)(LogEventId.ModuleLogMessage + (ushort)type), (EventLogEntryType)type);
        }
        void IBaseServer.WriteToLogMultiple(string message, ServerLogEntryType type)
        {
            WriteToLogMultiple(message, (ushort)(LogEventId.ModuleLogMessage + (ushort)type), (EventLogEntryType)type);
        }

        void IBaseServer.WriteToLog(Exception exc, bool includeStackTrace)
        {
            WriteToLog(exc, (ushort)(LogEventId.ModuleLogMessage + (ushort)ServerLogEntryType.Error), includeStackTrace);
        }

        void IBaseServer.WriteToLog(Exception exc, string title, bool includeStackTrace)
        {
            WriteToLog(exc, title, (ushort)(LogEventId.ModuleLogMessage + (ushort)ServerLogEntryType.Error), includeStackTrace);
        }

        void IBaseServer.WriteToLogWithoutModuleHeader(string message, ServerLogEntryType type)
        {
            WriteToLogWithoutModuleHeader(message, (ushort)(LogEventId.ModuleLogMessage + (ushort)type), (EventLogEntryType)type);
        }
        void IBaseServer.SendUDPSignal(string signal) => throw new NotImplementedException();
        void IBaseServer.SendUDPLog(ServerLogEntryType type, string message) => throw new NotImplementedException();
        #endregion


        internal static class Constants
        {
            public const string SHELL_REGISTRY_HIVE = @"HKEY_CURRENT_USER";
            public const string SHELL_REGISTRY_ROOT_PATH = SHELL_REGISTRY_HIVE + @"\Software\SpaceTeam\SpaceTeamLab";
            public const string SHELL_REGISTRY_SUB_PATH = ConfigController.CONFIG_SHELLSERVER_SUBPATH;
            public const string SHELL_MODULES_REGISTRY_PATH = SHELL_REGISTRY_SUB_PATH + @"\" + ConfigController.CONFIG_MODULES_SUBPATH;

            public const string SERVER_REGISTRY_HIVE = @"HKEY_LOCAL_MACHINE";
            public const string SERVER_REGISTRY_ROOT_PATH = SERVER_REGISTRY_HIVE + @"\Software\SpaceTeam\SpaceTeamLab";
            public const string SERVER_REGISTRY_SUB_PATH = ConfigController.CONFIG_APPSERVER_SUBPATH;
            public const string SERVER_MODULES_REGISTRY_PATH = SERVER_REGISTRY_SUB_PATH + @"\" + ConfigController.CONFIG_MODULES_SUBPATH;
        }

        #region OnUnhandledDomainException
        private void OnUnhandledDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception)
                OnUnhandledException(e.ExceptionObject as Exception);
        }
        #endregion

        #region OnUnobservedTaskException
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();

            OnUnhandledException(e.Exception);
        }
        #endregion

        //#region OnThreadException
        //private void OnThreadException(object sender, ThreadExceptionEventArgs e)
        //{
        //    OnUnhandledUIException(e.Exception);
        //}
        //#endregion

        #region OnUnhandledException
        /// <summary>
        /// Вызывается при возникновении необработанного исключения.
        /// </summary>
        /// <param name="exc">Исключение.</param>
        protected virtual void OnUnhandledException(Exception exc)
        {
            exc = exc.GetRealException();

            if (!(exc is ThreadAbortException))
                WriteToLog(exc);
        }
        #endregion

        private static void SetInstance(BaseServer instance)
        {
            lock (_instanceLock)
            {
                ClientContext.IsActive = ServerContext.IsActive = false;

                if (_instance != null)
                {
                    AppDomain.CurrentDomain.UnhandledException -= _instance.OnUnhandledDomainException;
                    TaskScheduler.UnobservedTaskException -= _instance.OnUnobservedTaskException;
                    //Application.ThreadException -= _instance.OnThreadException;
                }

                _instance = instance;

                if (_instance != null)
                {
                    if (_instance.ServerType == ServerType.Shell)
                        ClientContext.IsActive = true;
                    else
                      if (_instance.ServerType == ServerType.ApplicationServer)
                        ServerContext.IsActive = true;

                    AppDomain.CurrentDomain.UnhandledException += _instance.OnUnhandledDomainException;
                    TaskScheduler.UnobservedTaskException += _instance.OnUnobservedTaskException;
                    //Application.ThreadException += _instance.OnThreadException;
                }
            }
        }

        /// <summary>
        /// Класс, позволяющий сравнивать экземпляры модулей.
        /// </summary>
        protected sealed class ModuleComparer : IEqualityComparer<BaseModule>
        {
            #region .Static Field
            /// <summary>
            /// Singleton-экземпляр.
            /// </summary>
            public static readonly ModuleComparer Instance = new ModuleComparer();
            #endregion

            #region .Ctor
            private ModuleComparer()
            {
            }
            #endregion

            #region Equals
            public bool Equals(BaseModule x, BaseModule y)
            {
                return ReferenceEquals(x, y);
            }
            #endregion

            #region GetHashCode
            public int GetHashCode(BaseModule obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
            #endregion
        }

        /// <summary>
        /// Идентификаторы событий для журнала событий (0 - 999).
        /// </summary>
        protected internal static class LogEventId
        {
            #region .Constants
            /// <summary>
            /// Идентификатор по умолчанию.
            /// </summary>
            public const ushort Default = 0;

            /// <summary>
            /// Необработанное исключение.
            /// </summary>
            public const ushort UnhandledException = 100;

            /// <summary>
            /// Ошибка при запуске сервера.
            /// </summary>
            public const ushort ServerStartingError = 200;

            /// <summary>
            /// Ошибка при остановке сервера.
            /// </summary>
            public const ushort ServerStoppingError = 300;

            /// <summary>
            /// Ошибка при загрузке модуля.
            /// </summary>
            public const ushort ModuleLoadError = 400;

            /// <summary>
            /// Ошибка при выгрузке модуля.
            /// </summary>
            public const ushort ModuleUnloadError = 500;

            /// <summary>
            /// Сообщение модуля (800 - 899).
            /// </summary>
            public const ushort ModuleLogMessage = 600;
            #endregion
        }
    }

    public enum ServerType
    {
        #region .Static Fields
        /// <summary>
        /// Неизвестный тип.
        /// </summary>
        Unknown,

        /// <summary>
        /// Сервер приложений.
        /// </summary>
        ApplicationServer,

        /// <summary>
        /// Оболочка.
        /// </summary>
        Shell
        #endregion
    }

}
