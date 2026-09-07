using Win32.SimpleGui;

namespace Win32.SimpleGui.Example;

public class Example
{
    [STAThread]
    public static void Main()
    {
        using var window = Application.CreateWindow("Example Window", 800, 600);

        var layout = new VerticalLayout();
        window.AddElement(layout);

        layout.AddElement(new Label("Hello, World!"));
        layout.AddElement(new TextBox());
        var button = layout.AddElement(new Button("Click Me"));
        button.OnClick += _ => Application.MessageBox("Button Clicked", "You clicked the button!");
        var label = new Label("Select an item!");
        var list = layout.AddElement(new ListBox());
        list.Items.Add("Item 1");
        list.Items.Add("Item 2");
        list.Items.Add("Item 3");
        list.OnSelectedIndexChanged += (_, i) => label.Text = $"Selected: {list.Items[i]}";
        layout.AddElement(label);

        layout.Arrange();

        window.RunEventLoop();
    }
}