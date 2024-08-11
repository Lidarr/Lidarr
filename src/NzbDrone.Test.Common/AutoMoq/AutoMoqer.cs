using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using DryIoc;
using Moq;
using NzbDrone.Common.Composition;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Test.Common.AutoMoq
{
    [DebuggerStepThrough]
    public class AutoMoqer
    {
        public readonly MockBehavior DefaultBehavior = MockBehavior.Default;
        private readonly IContainer _container;
        private readonly IDictionary<Type, object> _registeredMocks = new Dictionary<Type, object>();

        public AutoMoqer()
        {
            _container = CreateTestContainer(new Container(rules => rules.WithMicrosoftDependencyInjectionRules().WithDefaultReuse(Reuse.Singleton)));

            LoadPlatformLibrary();

            AssemblyLoader.RegisterSQLiteResolver();
        }

        public IContainer Container => _container;

        public virtual T Resolve<T>()
        {
            var result = _container.Resolve<T>();
            SetConstant(result);
            return result;
        }

        public virtual T Resolve<T>(object serviceKey)
        {
            var result = _container.Resolve<T>(serviceKey: serviceKey);
            SetConstant(result);
            return result;
        }

        public virtual Mock<T> GetMock<T>()
            where T : class
        {
            return GetMock<T>(DefaultBehavior);
        }

        public virtual Mock<T> GetMock<T>(MockBehavior behavior)
            where T : class
        {
            var type = typeof(T);
            if (GetMockHasNotBeenCalledForThisType(type))
            {
                CreateANewMockAndRegisterIt<T>(type, behavior);
            }

            var mock = TheRegisteredMockForThisType<T>(type);

            if (behavior != MockBehavior.Default && mock.Behavior == MockBehavior.Default)
            {
                throw new InvalidOperationException("Unable to change be behaviour of an existing mock.");
            }

            return mock;
        }

        public virtual void SetMock(Type type, Mock mock)
        {
            if (GetMockHasNotBeenCalledForThisType(type))
            {
                _registeredMocks.Add(type, mock);
            }

            if (mock != null)
            {
                _container.RegisterInstance(type, mock.Object, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
            }
        }

        public virtual void SetConstant<T>(T instance)
        {
            _container.RegisterInstance(instance, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
            SetMock(instance.GetType(), null);
        }

        private IContainer CreateTestContainer(IContainer container)
        {
            var c = container.CreateChild(IfAlreadyRegistered.Replace,
                container.Rules
                    .WithDynamicRegistration((serviceType, serviceKey) =>
                    {
                        // ignore services with non-default key
                        if (serviceKey != null)
                        {
                            return null;
                        }

                        if (serviceType == typeof(object))
                        {
                            return null;
                        }

                        if (serviceType.IsGenericType && serviceType.IsOpenGeneric())
                        {
                            return null;
                        }

                        if (serviceType == typeof(System.Text.Json.Serialization.JsonConverter))
                        {
                            return null;
                        }

                        // get the Mock object for the abstract class or interface
                        if (serviceType.IsInterface || serviceType.IsAbstract)
                        {
                            return new[] { new DynamicRegistration(GetMockFactory(serviceType), IfAlreadyRegistered.Keep) };
                        }

                        // concrete types
                        var concreteTypeFactory = serviceType.ToFactory(Reuse.Singleton, FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic);

                        return new[] { new DynamicRegistration(concreteTypeFactory) };
                    },
                    DynamicRegistrationFlags.Service | DynamicRegistrationFlags.AsFallback));

            c.Register(typeof(Mock<>), Reuse.Singleton, FactoryMethod.DefaultConstructor());

            return c;
        }

        private Mock<T> TheRegisteredMockForThisType<T>(Type type)
            where T : class
        {
            return (Mock<T>)_registeredMocks.First(x => x.Key == type).Value;
        }

        private void CreateANewMockAndRegisterIt<T>(Type type, MockBehavior behavior)
            where T : class
        {
            var mock = new Mock<T>(behavior);
            _container.RegisterInstance(mock.Object);
            SetMock(type, mock);
        }

        private bool GetMockHasNotBeenCalledForThisType(Type type)
        {
            return !_registeredMocks.ContainsKey(type);
        }

        private DelegateFactory GetMockFactory(Type serviceType)
        {
            var mockType = typeof(Mock<>).MakeGenericType(serviceType);
            return DelegateFactory.Of(r =>
            {
                var mock = (Mock)r.Resolve(mockType);
                SetMock(serviceType, mock);
                return mock.Object;
            }, Reuse.Singleton);
        }

        private void LoadPlatformLibrary()
        {
            var assemblyName = "Lidarr.Windows";

            if (OsInfo.IsNotWindows)
            {
                assemblyName = "Lidarr.Mono";
            }

            var types = Assembly.Load(assemblyName).GetTypes();
            var diskProvider = types.SingleOrDefault(x => x.Name == "DiskProvider");

            // The standard dynamic mock registrations, explicit so DryIoC doesn't get confused when we add alternatives
            _container.Register(typeof(IFileSystem), GetMockFactory(typeof(IFileSystem)));
            _container.Register(typeof(IDiskProvider), GetMockFactory(typeof(IDiskProvider)));

            // A concrete registration from the platform library using a mock filesystem
            _container.RegisterInstance<IFileSystem>(new MockFileSystem(), serviceKey: FileSystemType.Mock);
            _container.Register(typeof(IDiskProvider),
                diskProvider,
                made: Parameters.Of.Type<IFileSystem>(serviceKey: FileSystemType.Mock),
                serviceKey: FileSystemType.Mock);

            // A concrete registration from the platform library using the actual filesystem
            _container.Register<IFileSystem, FileSystem>(serviceKey: FileSystemType.Actual);
            _container.Register(typeof(IDiskProvider),
                diskProvider,
                made: Parameters.Of.Type<IFileSystem>(serviceKey: FileSystemType.Actual),
                serviceKey: FileSystemType.Actual);
        }
    }
}
