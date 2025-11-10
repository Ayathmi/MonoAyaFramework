using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;

namespace MonoAya
{
    #region Framework

    public interface IFramework
    {
        void RegisterRules<T>(T rules) where T : IRules;
        void RegisterModel<T>(T model) where T : IModel;
        void RegisterTools<T>(T tools) where T : ITools;
        
        T GetModel<T>() where T : class, IModel;
        T GetTools<T>() where T : class, ITools;
        T GetRules<T>() where T : class, IRules;
        
        UniTask SendCommandAsync<T>(T command) where T : IAsyncCommand;
        UniTask<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command);
        TResult SendQuery<TResult>(IQuery<TResult> query);

        void SendEvent<T>(T eventData);
        IDisposable RegisterEvent<T>(Action<T> onEvent);
        
        void Shutdown();
    }

    public abstract class Framework<T> : IFramework, IDisposable where T : Framework<T>, new()
    {
        protected static T s_Instance;
        private readonly IoCContainer m_Container = new IoCContainer();
        private readonly CompositeDisposable m_Disposables = new CompositeDisposable();
        private bool m_IsInitialized;

        public static IFramework Entry => s_Instance ??= new T();

        protected Framework()
        {
            Initialize();
            m_IsInitialized = true;

            foreach (var model in m_Container.GetInstanceByType<IModel>())
            {
                if (!model.IsInitialized)
                {
                    model.Initialize();
                }
            }

            foreach (var rules in m_Container.GetInstanceByType<IRules>())
            {
                if (!rules.IsInitialized)
                {
                    rules.Initialize();
                }
            }
        }
        
        protected abstract void Initialize();

        public void RegisterRules<T1>(T1 rules) where T1 : IRules
        {
            rules.SetFramework(this);
            m_Container.Register(rules);

            if (m_IsInitialized)
            {
                rules.Initialize();
            }
        }

        public void RegisterModel<T1>(T1 model) where T1 : IModel
        {
            model.SetFramework(this);
            m_Container.Register(model);

            if (m_IsInitialized)
            {
                model.Initialize();
            }
        }

        public IDisposable RegisterEvent<T1>(Action<T1> onEvent) => UniEventSystem.Register(onEvent);

        public void RegisterTools<T1>(T1 tools) where T1 : ITools => m_Container.Register(tools);
        
        public T2 GetRules<T2>() where T2 : class, IRules => m_Container.Get<T2>();
        
        public T2 GetModel<T2>() where T2 : class, IModel => m_Container.Get<T2>();
        
        public T2 GetTools<T2>() where T2 : class, ITools => m_Container.Get<T2>();
        
        public async UniTask SendCommandAsync<T3>(T3 command) where T3 : IAsyncCommand
        {
            if (command == null) throw new ArgumentException(nameof(command));
            command.SetFramework(this);
            await command.ExecuteAsync();
        }

        public async UniTask<T3> SendCommandAsync<T3>(IAsyncCommand<T3> command)
        {
            command.SetFramework(this);
            return await command.ExecuteAsync();
        }

        public T3 SendQuery<T3>(IQuery<T3> query)
        {
            query.SetFramework(this);
            return query.Execute();
        }

        public void SendEvent<T3>(T3 eventData) => UniEventSystem.Send(eventData);

        public void Shutdown()
        {
            m_Disposables?.Dispose();
            m_Container?.Clear();
            s_Instance = null;
        }

        public void Dispose() => Shutdown();
    }

    #endregion

    #region Rules

    public interface IRules : IConstraintByFramework, ISetFramework, IGetModel, IGetRules, IGetTools, IRegisterEvent,
        ISendEvent, ISendQuery, IInit { }

    public abstract class RulesBase : IRules
    {
        public bool IsInitialized { get; set; }

        private IFramework m_Framework;
        public IFramework GetFramework() => m_Framework;
        public void SetFramework(IFramework framework) => m_Framework = framework;

        public void Initialize()
        {
            OnInitialize();
            IsInitialized = true;
        }

        public void Shutdown() => OnShutdown();
        
        protected abstract void OnInitialize();

        protected virtual void OnShutdown() { }
    }

    #endregion

    #region Model

    public interface IModel : IConstraintByFramework, ISetFramework, IGetTools, ISendEvent, IInit { }

    public abstract class ModelBase : IModel
    {
        public bool IsInitialized { get; set; }

        private IFramework m_Framework;
        public IFramework GetFramework() => m_Framework;
        public void SetFramework(IFramework framework) => m_Framework = framework;

        public void Initialize()
        {
            OnInitialization();
            IsInitialized = true;
        }
        
        public void Shutdown() => OnShutdown();

        protected abstract void OnInitialization();

        protected virtual void OnShutdown() { }
    }

    #endregion

    #region Tools

    public interface ITools { }

    #endregion

    #region Query

    public interface IQuery<TResult> : IConstraintByFramework, ISetFramework, IGetModel, IGetTools, IGetRules, ISendQuery
    {
        TResult Execute();
    }

    public abstract class QueryBase<T> : IQuery<T>
    {
        private IFramework m_Framework;
        public IFramework GetFramework() => m_Framework;
        public void SetFramework(IFramework framework) => m_Framework = framework;
        
        public T Execute() => OnExecute();
        protected abstract T OnExecute();
    }

    #endregion

    #region Command

    public interface IAsyncCommand : IConstraintByFramework, ISetFramework, IGetRules, IGetModel, IGetTools, ISendEvent,
        ISendCommand, ISendQuery
    {
        UniTask ExecuteAsync();
    }

    public interface IAsyncCommand<TResult> : IConstraintByFramework, ISetFramework, IGetModel, IGetTools, ISendEvent,
        ISendCommand, ISendQuery
    {
        UniTask<TResult> ExecuteAsync();
    }

    public abstract class AsyncCommandBase : IAsyncCommand
    {
        private IFramework m_Framework;
        public IFramework GetFramework() => m_Framework;
        public void SetFramework(IFramework framework) => m_Framework = framework;

        public async UniTask ExecuteAsync() => await OnExecuteAsync();
        protected abstract UniTask OnExecuteAsync();
    }

    public abstract class AsyncCommandBase<TResult> : IAsyncCommand<TResult>
    {
        private IFramework m_Framework;
        public IFramework GetFramework() => m_Framework;
        public void SetFramework(IFramework framework) => m_Framework = framework;
        
        public async UniTask<TResult> ExecuteAsync() => await OnExecuteAsync();
        protected abstract UniTask<TResult> OnExecuteAsync();
    }

    #endregion

    #region Controller

    public interface IController : IConstraintByFramework, ISendCommand, IGetRules, IGetModel, IGetTools,
        IRegisterEvent, ISendQuery { }

    #endregion

    #region Constraint

    public interface IConstraintByFramework
    {
        IFramework GetFramework();
    }

    public interface ISetFramework
    {
        void SetFramework(IFramework framework);
        
    }
    
    public interface IGetModel : IConstraintByFramework { }
    
    public interface IGetRules : IConstraintByFramework { }

    public interface IGetTools : IConstraintByFramework { }

    public interface IRegisterEvent : IConstraintByFramework { }
    
    public interface ISendEvent : IConstraintByFramework { }
    
    public interface ISendQuery : IConstraintByFramework { }
    
    public interface ISendCommand : IConstraintByFramework { }

    public interface IInit
    {
        bool IsInitialized { get; set; }
        void Initialize();
        void Shutdown();
    }

    public static class FrameworkExtensions
    {
        public static T GetModel<T>(this IGetModel self) where T : class, IModel => self.GetFramework().GetModel<T>();
        
        public static T GetRules<T>(this IGetRules self) where T : class, IRules => self.GetFramework().GetRules<T>();
        
        public static T GetTools<T>(this IGetTools self) where T : class, ITools => self.GetFramework().GetTools<T>();
        
        public static IDisposable RegisterEvent<T>(this IRegisterEvent self, Action<T> onEvent) => 
            self.GetFramework().RegisterEvent(onEvent);

        public static async UniTask SendCommand<T>(this ISendCommand self, T command)
            where T : IAsyncCommand => await self.GetFramework().SendCommandAsync(command);

        public static void SendEvent<T>(this ISendEvent self, T eventData) =>
            self.GetFramework().SendEvent(eventData);

        public static TResult SendQuery<TResult>(this ISendQuery self, IQuery<TResult> query) =>
            self.GetFramework().SendQuery(query);
    }
    
    #endregion

    #region IoC

    public enum LifetimeScope
    {
        Singleton,
        Transient
    }
    public class IoCContainer : IDisposable
    {
        private readonly ConcurrentDictionary<Type, object> m_Instances = new ConcurrentDictionary<Type, object>();
        private bool m_IsDisposed;

        public void Register<T>(T instance)
        {
            if (m_IsDisposed) return;
            var key = typeof(T);
            m_Instances[key] = instance;
        }

        public void Register<T>(Func<T> factory, LifetimeScope scope)
        {
            if (m_IsDisposed) return;
            var key = typeof(T);
        }

        public T Get<T>() where T : class
        {
            if (m_IsDisposed) return null;
            var key = typeof(T);

            if (m_Instances.TryGetValue(key, out var instance))
            {
                return instance as T;
            }

            return null;
        }

        public IEnumerable<T> GetInstanceByType<T>()
        {
            if (m_IsDisposed) return Enumerable.Empty<T>();
            
            var type = typeof(T);
            return m_Instances.Values.Where(instance => type.IsInstanceOfType(instance)).Cast<T>();
        }

        public void Clear()
        {
            foreach (var instance in m_Instances.Values)
            {
                if (instance is IInit init)
                {
                    init.Shutdown();
                }

                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            m_Instances.Clear();
        }

        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                Clear();
                m_IsDisposed = true;
            }
        }
    }

    #endregion
    
    #region EventSystem
    
    public interface IUnRegister : IDisposable
    {
        void UnRegisterOperation();
    }

    public interface IUniEvent
    {
        IDisposable RegisterOperation(Action onEvent);
    }

    public struct UnRegister : IUnRegister
    {
        private Action m_UnRegister;
        
        public UnRegister(Action unRegister) => m_UnRegister = unRegister;

        public void UnRegisterOperation()
        {
            m_UnRegister?.Invoke();
            m_UnRegister = null;
        }

        public void Dispose() => UnRegisterOperation();
    }

    public class UniEvent : IUniEvent, IDisposable
    {
        private readonly Subject<Unit> m_Subject = new Subject<Unit>();

        public IDisposable RegisterOperation(Action onEvent)
        {
            return m_Subject.Subscribe(_ => onEvent());
        }

        public IDisposable RegisterOperationCallable(Action onEvent)
        {
            onEvent.Invoke();
            return RegisterOperation(onEvent);
        }
        
        public void Trigger() => m_Subject.OnNext(Unit.Default);
        
        public void Dispose() => m_Subject.Dispose();
    }

    public class UniEvent<T> : IUniEvent, IDisposable
    {
        private readonly Subject<T> m_Subject = new Subject<T>();

        public IDisposable RegisterOperation(Action<T> onEvent)
        {
            return m_Subject.Subscribe(onEvent);
        }

        public IDisposable RegisterOperation(Action onEvent)
        {
            return m_Subject.Subscribe(_ => onEvent());
        }

        public IDisposable RegisterOperationInitialized(T initialValue ,Action<T> onEvent)
        {
            onEvent?.Invoke(initialValue);
            return onEvent != null ? m_Subject.Subscribe(onEvent) :  Disposable.Empty;
        }

        public void Dispose() => m_Subject.Dispose();
    }

    public interface IAsyncUniEvent<T>
    {
        UniTask<T> GetAsync();
        void Complete(T value);
    }

    public class AsyncUniEvent<T> : IAsyncUniEvent<T>, IDisposable
    {
        private readonly Subject<T> m_Subject = new Subject<T>();

        public UniTask<T> GetAsync()
        {
            return m_Subject.FirstAsync().AsUniTask();
        }

        public void Complete(T value)
        {
            m_Subject.OnNext(value);
            m_Subject.OnCompleted();
        }

        public void Dispose() => m_Subject.Dispose();
    }

    public static class UniEventSystem
    {
        private static readonly ConcurrentDictionary<Type, object> s_Subject = new ConcurrentDictionary<Type, object>();

        private static Subject<T> GetOrAddSubject<T>()
        {
            return (Subject<T>)s_Subject.GetOrAdd(typeof(T), _ => new Subject<T>());
        }

        public static void Send<T>(T e)
        {
            var subject = GetOrAddSubject<T>();
            subject.OnNext(e);
        }

        public static IDisposable Register<T>(Action<T> onEvent)
        {
            var subject = GetOrAddSubject<T>();
            return subject.Subscribe(onEvent);
        }

        public static void Clear()
        {
            foreach (var subject in s_Subject.Values.OfType<IDisposable>())
            {
                subject?.Dispose();
            }
            s_Subject.Clear();
        }
    }
    
    #endregion

    #region DataBinding

    public interface IReadonlyBindableProperty<out T>
    {
        T Value { get; }
        IDisposable Subscribe(Action<T> observer);
    }

    public class BindableProperty<T> : IReadonlyBindableProperty<T>
    {
        private readonly BehaviorSubject<T> m_Subject;
        private readonly object m_lock = new object();

        public T Value
        {
            get => m_Subject.Value;
            set
            {
                lock (m_lock)
                {
                    if (!EqualityComparer<T>.Default.Equals(m_Subject.Value, value))
                    {
                        m_Subject.OnNext(value);
                    }
                }
            }
        }

        public BindableProperty(T defaultValue = default)
        {
            m_Subject = new BehaviorSubject<T>(defaultValue);
        }

        public IDisposable Subscribe(Action<T> onValueChanged)
        {
            return m_Subject.Subscribe(onValueChanged);
        }

        public override string ToString() => Value.ToString();
    }

    #endregion

    #region EventExtend

    public class OrEvent : IDisposable
    {
        private readonly List<IDisposable> m_Disposables = new List<IDisposable>();
        private readonly UniEvent m_Event = new UniEvent();

        public OrEvent OrOperation(IUniEvent uniEvent)
        {
            var disposable = uniEvent.RegisterOperation(() => m_Event.Trigger());
            m_Disposables.Add(disposable);
            return this;
        }

        public IDisposable RegisterOperation(Action onEvent) => m_Event.RegisterOperation(onEvent);

        public void Dispose()
        {
            foreach (var disposable in m_Disposables)
            {
                disposable?.Dispose();
            }
            
            m_Disposables.Clear();
            m_Event?.Dispose();
        }
    }

    public static class OrEventExtension
    {
        public static OrEvent OrOperation(this IUniEvent self, IUniEvent other) =>
            new OrEvent().OrOperation(self).OrOperation(other);
    }

    #endregion
}