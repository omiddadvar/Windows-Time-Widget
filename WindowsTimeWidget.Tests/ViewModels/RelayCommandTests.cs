
using FluentAssertions;
using WindowsTimeWidget.ViewModels;

namespace WindowsTimeWidget.Tests.ViewModels;

[Trait("Module", "ViewModels.RelayCommand")]
public class RelayCommandTests
{
    [Fact]
    public void Execute_InvokesAction()
    {
        // Arrange
        var invoked = 0;
        var cmd = new RelayCommand(() => invoked++);

        // Act
        cmd.Execute(null);

        // Assert
        invoked.Should().Be(1);
    }

    [Fact]
    public void CanExecute_WithoutPredicate_ReturnsTrue()
    {
        // Arrange
        var cmd = new RelayCommand(() => { });

        // Act
        var can = cmd.CanExecute(null);

        // Assert
        can.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithPredicate_DelegatesToIt()
    {
        // Arrange
        var allowed = false;
        var cmd = new RelayCommand(() => { }, () => allowed);

        // Act / Assert
        cmd.CanExecute(null).Should().BeFalse();
        allowed = true;
        cmd.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Execute_WithParameter_PassesParameterToAction()
    {
        // Arrange
        object? received = null;
        var cmd = new RelayCommand(p => received = p);

        // Act
        cmd.Execute("hello");

        // Assert
        received.Should().Be("hello");
    }

    [Fact]
    public void CanExecute_WithParameter_PassesParameterToPredicate()
    {
        // Arrange
        object? seen = null;
        var cmd = new RelayCommand(_ => { }, p => { seen = p; return true; });

        // Act
        cmd.CanExecute(123);

        // Assert
        seen.Should().Be(123);
    }

    [Fact]
    public void Ctor_NullExecute_Throws()
    {
        // Arrange / Act
        Action act = () => new RelayCommand((Action<object?>)(null!));

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("execute");
    }


    [Fact]
    public void CanExecuteChanged_SubscribesAndUnsubscribes_WithoutThrowing()
    {
        // Arrange
        var cmd = new RelayCommand(() => { });
        EventHandler handler = (_, _) => { };

        // Act
        cmd.CanExecuteChanged += handler;
        cmd.CanExecuteChanged -= handler;

        // Assert
        cmd.Should().NotBeNull();
    }
}