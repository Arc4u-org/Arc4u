using Arc4u.Blazor;
using AwesomeAssertions;
using Blazored.LocalStorage;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Blazor;

[Trait("Category", "CI")]
public class WindowInteropTests
{
    private static Mock<ILocalStorageService> LocalStorage(params string?[] fetchingValues)
    {
        var localStorage = new Mock<ILocalStorageService>();
        localStorage.Setup(s => s.ContainKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<bool>(false));

        var fetching = localStorage.SetupSequence(s => s.GetItemAsync<string>("fetching", It.IsAny<CancellationToken>()));
        foreach (var value in fetchingValues)
        {
            fetching = fetching.Returns(new ValueTask<string?>(value));
        }

        return localStorage;
    }

    [Fact]
    public async Task OpenWindowAsync_When_The_Popup_Script_Is_Missing_Should_Throw_An_Actionable_Exception()
    {
        var jsError = new JSException("Could not find 'Arc4u.CreatePopupWindow' ('Arc4u' was undefined).");
        var jsRuntime = new Mock<IJSRuntime>();
#pragma warning disable BL0016 // A Moq setup expression, not a JS interop call.
        jsRuntime.Setup(r => r.InvokeAsync<IJSVoidResult>("Arc4u.CreatePopupWindow", It.IsAny<object?[]?>())).Throws(jsError);
#pragma warning restore BL0016

        var act = () => new WindowInterop().OpenWindowAsync(jsRuntime.Object, LocalStorage().Object, "https://login.example.com");

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*CreatePopupWindow.js*")
            .WithInnerException<JSException>();
    }

    [Fact]
    public async Task OpenWindowAsync_When_The_Login_Is_Never_Completed_Should_Time_Out()
    {
        // "fetching" is never written, as when the user closes the popup.
        var sut = new WindowInterop { LoginTimeout = TimeSpan.FromMilliseconds(300) };

        var act = () => sut.OpenWindowAsync(new Mock<IJSRuntime>().Object, LocalStorage().Object, "https://login.example.com");

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task OpenWindowAsync_When_The_Login_Completes_Should_Return_And_Remove_The_Fetching_Entry()
    {
        var localStorage = LocalStorage(null, "true");

        await new WindowInterop().OpenWindowAsync(new Mock<IJSRuntime>().Object, localStorage.Object, "https://login.example.com");

        localStorage.Verify(s => s.RemoveItemAsync("fetching", It.IsAny<CancellationToken>()), Times.Once);
    }
}
