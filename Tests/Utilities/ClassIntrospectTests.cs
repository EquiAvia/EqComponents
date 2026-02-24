using equiavia.components.Utilities;
using Xunit;

namespace equiavia.components.Tests.Utilities;

public class ClassIntrospectTests
{
    private class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public Address? Address { get; set; }
    }

    private class Address
    {
        public string? City { get; set; }
    }

    // ── GetPropValue ──────────────────────────────────────────────────────────

    [Fact]
    public void GetPropValue_ReturnsStringValue()
    {
        var person = new Person { Name = "Alice" };
        Assert.Equal("Alice", person.GetPropValue("Name"));
    }

    [Fact]
    public void GetPropValue_ReturnsIntValue()
    {
        var person = new Person { Age = 30 };
        Assert.Equal(30, person.GetPropValue("Age"));
    }

    [Fact]
    public void GetPropValue_ReturnsNull_WhenPropertyDoesNotExist()
    {
        var person = new Person();
        Assert.Null(person.GetPropValue("NonExistent"));
    }

    [Fact]
    public void GetPropValue_ReturnsDottedPathValue_ForNestedProperty()
    {
        var person = new Person { Address = new Address { City = "Wellington" } };
        Assert.Equal("Wellington", person.GetPropValue("Address.City"));
    }

    [Fact]
    public void GetPropValue_ReturnsNull_WhenIntermediatePropertyIsNull()
    {
        var person = new Person { Address = null };
        Assert.Null(person.GetPropValue("Address.City"));
    }

    [Fact]
    public void GetPropValue_Generic_ReturnsTypedValue()
    {
        var person = new Person { Name = "Bob" };
        Assert.Equal("Bob", person.GetPropValue<string>("Name"));
    }

    [Fact]
    public void GetPropValue_Generic_ReturnsDefault_WhenPropertyIsNull()
    {
        var person = new Person { Name = null };
        Assert.Null(person.GetPropValue<string>("Name"));
    }

    // ── HasProperty (extension on object) ────────────────────────────────────

    [Fact]
    public void HasProperty_ReturnsTrue_WhenPropertyExists()
    {
        var person = new Person();
        Assert.True(person.HasProperty("Name"));
        Assert.True(person.HasProperty("Age"));
    }

    [Fact]
    public void HasProperty_ReturnsFalse_WhenPropertyDoesNotExist()
    {
        var person = new Person();
        Assert.False(person.HasProperty("Missing"));
    }

    // ── HasProperty (static overload taking Type) ─────────────────────────────

    [Fact]
    public void HasProperty_ByType_ReturnsTrue_WhenPropertyExists()
    {
        Assert.True(ClassIntrospect.HasProperty(typeof(Person), "Name"));
    }

    [Fact]
    public void HasProperty_ByType_ReturnsFalse_WhenPropertyDoesNotExist()
    {
        Assert.False(ClassIntrospect.HasProperty(typeof(Person), "Missing"));
    }

    // ── SetPropValue ──────────────────────────────────────────────────────────

    [Fact]
    public void SetPropValue_SetsStringProperty()
    {
        var person = new Person();
        person.SetPropValue("Name", "Charlie");
        Assert.Equal("Charlie", person.Name);
    }

    [Fact]
    public void SetPropValue_SetsIntProperty()
    {
        var person = new Person();
        person.SetPropValue("Age", 42);
        Assert.Equal(42, person.Age);
    }

    [Fact]
    public void SetPropValue_DoesNotThrow_WhenPropertyDoesNotExist()
    {
        var person = new Person();
        // Should write to console but not throw
        var ex = Record.Exception(() => person.SetPropValue("NonExistent", "value"));
        Assert.Null(ex);
    }
}
