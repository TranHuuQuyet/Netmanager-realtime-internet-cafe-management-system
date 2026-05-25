namespace ClientApp.Forms;

internal static class ClientUiTheme
{
    public static readonly Color Window = Color.FromArgb(224, 224, 224);
    public static readonly Color Header = Color.FromArgb(192, 192, 192);
    public static readonly Color Surface = Color.White;
    public static readonly Color Line = Color.FromArgb(160, 160, 160);
    public static readonly Color Shadow = Color.FromArgb(128, 128, 128);
    public static readonly Color Ink = Color.Black;
    public static readonly Color MutedInk = Color.FromArgb(64, 64, 64);
    public static readonly Color Danger = Color.FromArgb(160, 0, 0);
    public static readonly Color Warning = Color.FromArgb(128, 80, 0);
    public static readonly Color Success = Color.FromArgb(0, 96, 0);
    public static readonly Color LockShade = Color.FromArgb(40, 40, 40);

    public static readonly Font BodyFont = new("Tahoma", 7.5F, FontStyle.Regular);
    public static readonly Font BoldFont = new("Tahoma", 7.5F, FontStyle.Bold);
    public static readonly Font TitleFont = new("Tahoma", 9F, FontStyle.Bold);
    public static readonly Font SmallFont = new("Tahoma", 7F, FontStyle.Regular);
    public static readonly Font ButtonFont = new("Tahoma", 6.75F, FontStyle.Regular);

    public static void ApplyClassicWindow(Form form, string title, Size clientSize)
    {
        form.Text = title;
        form.FormBorderStyle = FormBorderStyle.FixedSingle;
        form.MaximizeBox = false;
        form.MinimizeBox = true;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.ClientSize = clientSize;
        form.MinimumSize = Size.Empty;
        form.BackColor = Window;
        form.Font = BodyFont;
    }

    public static Label HeaderLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            BackColor = Header,
            BorderStyle = BorderStyle.FixedSingle,
            Font = TitleFont,
            ForeColor = Ink,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    public static TextBox Input(string text = "", bool password = false)
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Text = text,
            UseSystemPasswordChar = password,
            Font = BodyFont,
            BorderStyle = BorderStyle.Fixed3D,
            Margin = new Padding(0, 1, 0, 1)
        };
    }

    public static NumericUpDown PortInput(decimal value)
    {
        return new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 65535,
            Value = value,
            Font = BodyFont,
            BorderStyle = BorderStyle.Fixed3D,
            Margin = new Padding(0, 1, 0, 1)
        };
    }

    public static Label FieldLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = BoldFont,
            ForeColor = Ink,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    public static Label ValueBox(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            BackColor = Surface,
            BorderStyle = BorderStyle.Fixed3D,
            Font = BodyFont,
            ForeColor = Ink,
            Padding = new Padding(4, 0, 4, 0),
            TextAlign = ContentAlignment.MiddleRight
        };
    }

    public static Panel ClassicPanel(int padding)
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(padding),
            Margin = new Padding(0)
        };
    }

    public static Button ActionButton(string text, Action<Graphics, Rectangle> iconPainter, EventHandler clickHandler)
    {
        var button = new Button
        {
            Dock = DockStyle.Fill,
            Text = text,
            TextAlign = ContentAlignment.BottomCenter,
            ImageAlign = ContentAlignment.TopCenter,
            TextImageRelation = TextImageRelation.ImageAboveText,
            BackColor = Surface,
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont,
            ForeColor = Ink,
            Margin = new Padding(2),
            Padding = new Padding(0, 2, 0, 1),
            Cursor = Cursors.Hand,
            Image = CreateIcon(iconPainter)
        };
        button.FlatAppearance.BorderColor = Line;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(210, 210, 210);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 245);
        button.Click += clickHandler;
        return button;
    }

    public static Button CommandButton(string text, EventHandler clickHandler)
    {
        var button = new Button
        {
            Dock = DockStyle.Fill,
            Text = text,
            BackColor = Surface,
            FlatStyle = FlatStyle.Flat,
            Font = BoldFont,
            ForeColor = Ink,
            Margin = new Padding(2),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Line;
        button.Click += clickHandler;
        return button;
    }

    public static Bitmap CreateIcon(Action<Graphics, Rectangle> painter)
    {
        Bitmap bitmap = new(32, 28);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        painter(graphics, new Rectangle(2, 1, 28, 25));
        return bitmap;
    }
}
