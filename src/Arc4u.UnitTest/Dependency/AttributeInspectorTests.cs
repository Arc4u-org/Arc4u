using Arc4u.Dependency.Attribute;

namespace Arc4u.UnitTest.Dependency;
#pragma warning disable CS0618

// [Trait("Category", "CI")]
// public class AttributeInspectorTests
// {
//     [Fact]
//     public void Registering_A_Instance_With_Export_And_Shared_Attributes_Registers_With_Singleton_Lifetime()
//     {
//         var services = new ServiceCollection();
//         services.AddSingleton<SingletonObject>();
//
//         var container = services.BuildServiceProvider();
//
//         var instance = container.GetRequiredKeyedService<ISingletonObject>();
//         var instance2 = container.getre<ISingletonObject>();
//
//         instance.Should().BeSameAs(instance2);
//     }
//
//     [Fact]
//     public void Registering_A_Instance_With_Export_And_Scoped_Attributes_Registers_With_Scoped_Lifetime()
//     {
//         var container = new ComponentModelContainer();
//         var inspector = new AttributeInspector(container);
//         inspector.Register(typeof(ScopedObject));
//         container.CreateContainer();
//
//         // With a scope, the instances are different.
//         var scope1 = container.CreateScope();
//         var scope2 = container.CreateScope();
//         var instance1 = scope1.Resolve<IScopedObject>();
//         var instance2 = scope2.Resolve<IScopedObject>();
//         instance1.Should().NotBeNull();
//         instance1.Should().NotBeSameAs(instance2);
//
//         // Without any scope, the instance is the same.
//         var instance3 = container.Resolve<IScopedObject>();
//         var instance4 = container.Resolve<IScopedObject>();
//         instance3.Should().BeSameAs(instance4);
//     }
//
//     [Fact]
//     public void Registering_A_Instance_With_Only_Export_Registers_With_Transient_Lifetime()
//     {
//         var container = new ComponentModelContainer();
//         var inspector = new AttributeInspector(container);
//         inspector.Register(typeof(TransientObject));
//         container.CreateContainer();
//
//         var instance1 = container.Resolve<ITransientObject>();
//         var instance2 = container.Resolve<ITransientObject>();
//
//         instance1.Should().NotBeNull();
//         instance1.Should().NotBeSameAs(instance2);
//     }
// }

[Export(typeof(ITransientObject))]
public class TransientObject : ITransientObject
{
}

public interface ITransientObject
{
}

[Export(typeof(IScopedObject))]
[Scoped]
public class ScopedObject : IScopedObject
{
}

public interface IScopedObject
{
}

[Export(typeof(ISingletonObject))]
[Shared]
public class SingletonObject : ISingletonObject
{
}

public interface ISingletonObject
{
}

[Export(typeof(ITuple<int, string>))]
public class ImpTuple : ITuple<int, string>
{
}

public interface ITuple<T1, T2>
{
}

// Exported several times, to check that GenerateRegisteredTypes registers every Export.
[Export(typeof(ISingletonObject))]
[Export("Multi", typeof(ITuple<int, string>))]
[Shared]
public class MultiExport : ISingletonObject, ITuple<int, string>
{
}
