

using FluentAssertions;
using WindowsTimeWidget.ViewModels;

namespace WindowsTimeWidget.Tests.ViewModels;

[Trait("Module", "ViewModels.ViewModelBase")]
public class ViewModelBaseTests
{
    private class Probe : ViewModelBase
    {
        private int _value;
        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public void Raise(string name) => OnPropertyChanged(name);
    }

    [Fact]
    public void SetProperty_WhenValueChanges_RaisesPropertyChanged()
    {
        // Arrange
        var vm = new Probe();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // Act
        vm.Value = 42;

        // Assert
        raised.Should().ContainSingle().Which.Should().Be(nameof(Probe.Value));
        vm.Value.Should().Be(42);
    }

    [Fact]
    public void SetProperty_WhenValueUnchanged_DoesNotRaise()
    {
        // Arrange
        var vm = new Probe { Value = 7 };
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // Act
        vm.Value = 7;

        // Assert
        raised.Should().BeEmpty();
    }

    [Fact]
    public void SetProperty_ReturnsTrue_WhenValueChanged()
    {
        // Arrange
        var vm = new Probe();

        // Act
        var changed = InvokeSet(vm, 99);

        // Assert
        changed.Should().BeTrue();
    }

    [Fact]
    public void SetProperty_ReturnsFalse_WhenValueUnchanged()
    {
        // Arrange
        var vm = new Probe();
        InvokeSet(vm, 5);

        // Act
        var changed = InvokeSet(vm, 5);

        // Assert
        changed.Should().BeFalse();
    }

    [Fact]
    public void OnPropertyChanged_WithExplicitName_UsesThatName()
    {
        // Arrange
        var vm = new Probe();
        string? captured = null;
        vm.PropertyChanged += (_, e) => captured = e.PropertyName;

        // Act
        vm.Raise("Custom");

        // Assert
        captured.Should().Be("Custom");
    }

    // Helper
    private static bool InvokeSet(Probe vm, int value)
    {
        var before = vm.Value;
        vm.Value = value;
        return before != vm.Value;
    }
}