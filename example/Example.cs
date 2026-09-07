using System.Drawing;

namespace Win32.SimpleGui.Example;

public class Example
{
    [STAThread]
    public static void Main()
    {
        // All optional, but make it look better
        Application.EnableHiDPISupportForCurrentProcess();
        Application.EnableVisualStylesForCurrentThread();

        // Load an Icon - expects valid .ico data, no error handling
        using var icon = new Icon(LoadEmbeddedIcon());

        // Create font instances
        using var fontUI = new Font();
        using var fontMono = new Font("Consolas", 11f);

        // Spawn a Window with the given size and default font
        var window = new Window("Example Window", 280, 380, fontUI, icon);
        window.CanMaximize = false;

        // Window has a ManualLayout as the root, we add a vertical stack panel
        var layout = new VerticalLayout { Width = BaseLayout.Fill, Height = BaseLayout.Fill, Margin = new(left: 8, right: 8, top: 8, bottom: 6) };
        window.Children.Add(layout);

        // Basic label
        layout.Children.Add(new Label("Hello, World!") { Foreground = Color.CornflowerBlue });

        // TextBox, Button, nested layouts and MessageBox helper - we can even put our Icon on the MessageBox
        var box = layout.Children.Add(new TextBox());
        var nestedHorizontal = new HorizontalLayout { Width = BaseLayout.Fill, Height = 32, Margin = new(left: 8, right: 8, top: 4, bottom: 0), Spacing = 8 };
        var button = nestedHorizontal.Children.Add(new Button("Click Me") { Width = BaseLayout.Fill });
        var buttonDisabled = nestedHorizontal.Children.Add(new Button("Disabled") { Disabled = true, Width = BaseLayout.Fill });
        button.OnClick += _ => Application.MessageBox("Button Clicked", $"You clicked the button! Input: {box.Text}", icon, MessageBoxIcon.Information, canCancel: false);
        layout.Children.Add(nestedHorizontal);

        // A ListBox with some selection logic
        var label = new Label("Select an item!");
        var list = layout.Children.Add(new ListBox() { Height = BaseLayout.Fill, Font = fontMono });
        list.Items.Add("Item 1");
        list.Items.Add("Item 2");
        list.Items.Add("Item 3");
        list.OnSelectedIndexChanged += (_, i) => label.Text = i >= 0 ? $"Selected: {list.Items[i]}" : "Select an item!";
        layout.Children.Add(label);

        // A Slider from 0-100 (default) that drives a label
        var slider = layout.Children.Add(new Slider());
        var sliderLabel = new Label("Slider: 0") { Background = Color.LightSalmon };
        slider.OnValueChanged += (_, v) => sliderLabel.Text = $"Slider: {v}";
        layout.Children.Add(sliderLabel);

        // Timer example, callback runs on UI thread
        var timerLabel = layout.Children.Add(new Label("Timer: 0") { Background = Color.LightGreen });
        var counterA = 0; var counterB = 0;
        Application.ScheduleTimer(() => timerLabel.Text = $"Timer: {++counterA} {counterB}", 1000);
        Application.ScheduleTimer(() => { timerLabel.Text = $"Timer: {counterA} {++counterB}"; return counterB < 100; }, 250);

        // Make all of them fill the available width of the VerticalLayout, which itself is set to Fill the root layout (Window) + Margin
        foreach (var child in layout.Children)
            child.Width = BaseLayout.Fill;

        // Layouting is not automatic (except via Window.AutoLayoutOnResize), so call Arrange() manually
        // This will recurse down into other layouts, if required
        window.Arrange();

        // Blocking call to start our event loop
        // You may call `window.Dispose` from any other thread (or in a callback) to close it and continue from here
        Application.RunEventLoop();
    }

    private static byte[] LoadEmbeddedIcon()
    {
        using var stream = typeof(Example).Assembly.GetManifestResourceStream("Win32.SimpleGui.Example.icon.ico");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}