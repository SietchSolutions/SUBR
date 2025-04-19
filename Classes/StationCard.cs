using System;
using System.Drawing;
using System.Windows.Forms;

public partial class StationCard : UserControl
{
    public string StationName { get; private set; }
    public string StationType { get; private set; }
    public string Owner { get; private set; }
    public int Required { get; private set; }
    public int Delivered { get; private set; }

    private Label lblTitle;
    private Label lblType;
    private Label lblOwner;
    private Label lblCompletion;
    private Label lblStatus;

    public event Action<StationCard> OnStationCardClicked;

    public StationCard(string stationName, string stationType, string owner, int required, int delivered)
    {

        this.StationName = stationName;
        this.StationType = stationType;
        this.Owner = owner;
        this.Required = required;
        this.Delivered = delivered;

        this.DoubleBuffered = true;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.Padding = new Padding(5);
        this.Margin = new Padding(10);
        this.Width = 350;
        this.Height = 150;
        this.Cursor = Cursors.Hand;

        BuildCard();

        this.Click += (s, e) => OnStationCardClicked?.Invoke(this);
        foreach (Control ctrl in this.Controls)
            ctrl.Click += (s, e) => OnStationCardClicked?.Invoke(this);
    }

    private void BuildCard()
    {
        this.Controls.Clear();

        lblTitle = new Label
        {
            Text = StationName,
            Font = new Font("Consolas", 14, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Orange
        };

        lblType = new Label
        {
            Text = $"Type: {StationType}",
            Font = new Font("Consolas", 10, FontStyle.Regular),
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter
        };

        lblOwner = new Label
        {
            Text = $"Owner: {Owner}",
            Font = new Font("Consolas", 10, FontStyle.Regular),
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter
        };

        int percent = (Required == 0) ? 0 : (int)(((double)Delivered / Required) * 100);

        lblCompletion = new Label
        {
            Text = $"Completion: {percent}%",
            Font = new Font("Consolas", 10, FontStyle.Regular),
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = percent >= 100 ? Color.Lime : Color.White
        };

        lblStatus = new Label
        {
            Text = percent >= 100 ? "✅ Completed" : "🛠️ In Progress",
            Font = new Font("Consolas", 10, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = percent >= 100 ? Color.Lime : Color.Cyan
        };

        this.Controls.Add(lblStatus);
        this.Controls.Add(lblCompletion);
        this.Controls.Add(lblOwner);
        this.Controls.Add(lblType);
        this.Controls.Add(lblTitle);
    }

}
