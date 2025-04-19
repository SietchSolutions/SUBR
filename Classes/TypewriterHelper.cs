using System.Threading.Tasks;
using System.Windows.Forms;

public static class TypewriterHelper
{
    public static async Task TypeTextAsync(RichTextBox box, string text, int delay = 15)
    {
        foreach (char c in text)
        {
            box.AppendText(c.ToString());
            await Task.Delay(delay);
        }
        box.AppendText("\n");
        box.ScrollToCaret();
    }
}
