namespace GmailAutoSender.App.Behaviors;

public class PressAnimationBehavior : Behavior<Button>
{
    private Button? _button;

    protected override void OnAttachedTo(Button bindable)
    {
        base.OnAttachedTo(bindable);

        _button = bindable;

        _button.Pressed += OnPressed;
        _button.Released += OnReleased;
    }

    protected override void OnDetachingFrom(Button bindable)
    {
        bindable.Pressed -= OnPressed;
        bindable.Released -= OnReleased;

        _button = null;

        base.OnDetachingFrom(bindable);
    }

    private async void OnPressed(object? sender, EventArgs e)
    {
        if (_button is null || _button.IsEnabled is false)
            return;

        await _button.ScaleTo(0.96, 70, Easing.CubicOut);
    }

    private async void OnReleased(object? sender, EventArgs e)
    {
        if (_button is null)
            return;

        await _button.ScaleTo(1.0, 100, Easing.CubicOut);
    }
}
